using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using StarWarsTimelines.Application.Dtos;
using StarWarsTimelines.Persistence;

namespace StarWarsTimelines.Api.Tests;

public sealed class UserEndpointsTests : ApiTestBase
{
    public UserEndpointsTests(StarWarsTimelinesApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Get_AsOwner_ReturnsAccountWithEmail()
    {
        var client = await CreateStandardClientAsync();

        var response = await client.GetAsync($"/api/users/{SeedData.PadmeUserId}");

        response.EnsureSuccessStatusCode();
        var account = await response.Content.ReadFromJsonAsync<UserAccountResponse>();
        Assert.NotNull(account);
        Assert.Equal(SeedData.PadmeUserId, account.Id);
        Assert.Equal("padme", account.Username);
        Assert.Equal("Padmé Amidala", account.DisplayName);
        Assert.Equal("padme@example.com", account.Email);
        Assert.True(account.EmailVerified);
    }

    [Fact]
    public async Task Get_AsAdmin_ReturnsAnyAccount()
    {
        var client = await CreateAdminClientAsync();

        var response = await client.GetAsync($"/api/users/{SeedData.LukeUserId}");

        response.EnsureSuccessStatusCode();
        var account = await response.Content.ReadFromJsonAsync<UserAccountResponse>();
        Assert.NotNull(account);
        Assert.Equal("luke", account.Username);
    }

    [Fact]
    public async Task Get_AsOtherStandardUser_ReturnsForbidden()
    {
        var luke = await CreateClientAsAsync("luke", "luke123");

        var response = await luke.GetAsync($"/api/users/{SeedData.PadmeUserId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync($"/api/users/{SeedData.PadmeUserId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_AsAdmin_WithUnknownUser_ReturnsNotFound()
    {
        var client = await CreateAdminClientAsync();

        var response = await client.GetAsync($"/api/users/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateDisplayName_AsOwner_UpdatesName()
    {
        var (userId, client) = await RegisterVerifiedUserAsync("korkie", "korkie123");

        var response = await client.PutAsJsonAsync(
            $"/api/users/{userId}/display-name",
            new UpdateDisplayNameRequest("  Korkie Kryze  "));

        response.EnsureSuccessStatusCode();
        var account = await response.Content.ReadFromJsonAsync<UserAccountResponse>();
        Assert.NotNull(account);
        Assert.Equal("Korkie Kryze", account.DisplayName);

        var login = await Client.PostAsJsonAsync("/api/auth/login", new LoginRequest("korkie", "korkie123"));
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.Equal("Korkie Kryze", auth.User.DisplayName);
    }

    [Fact]
    public async Task UpdateDisplayName_WithBlankName_ReturnsBadRequest()
    {
        var (userId, client) = await RegisterVerifiedUserAsync("bo_katan", "bokatan123");

        var response = await client.PutAsJsonAsync(
            $"/api/users/{userId}/display-name",
            new UpdateDisplayNameRequest("   "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("display name is required", await ReadProblemDetailAsync(response));
    }

    [Fact]
    public async Task UpdateDisplayName_AsOtherUser_ReturnsForbidden()
    {
        var luke = await CreateClientAsAsync("luke", "luke123");

        var response = await luke.PutAsJsonAsync(
            $"/api/users/{SeedData.PadmeUserId}/display-name",
            new UpdateDisplayNameRequest("Queen Amidala"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEmail_AsOwner_ChangesEmailAndSendsVerificationLink()
    {
        var (userId, client) = await RegisterVerifiedUserAsync("satine", "satine123");
        var before = Factory.EmailSender.Sent.Count;

        var response = await client.PutAsJsonAsync(
            $"/api/users/{userId}/email",
            new UpdateEmailRequest("  SATINE.KRYZE@example.com "));

        response.EnsureSuccessStatusCode();
        var account = await response.Content.ReadFromJsonAsync<UserAccountResponse>();
        Assert.NotNull(account);
        Assert.Equal("satine.kryze@example.com", account.Email);
        Assert.False(account.EmailVerified);

        Assert.Equal(before + 1, Factory.EmailSender.Sent.Count);
        Assert.Contains("verify-email?token=", Factory.EmailSender.Sent.Last().HtmlBody);
        Assert.Contains("satine.kryze@example.com", Factory.EmailSender.Sent.Last().HtmlBody);
    }

    [Fact]
    public async Task UpdateEmail_WithDuplicateEmail_ReturnsConflict()
    {
        var (_, first) = await RegisterVerifiedUserAsync("duchess", "duchess12");
        var (secondId, second) = await RegisterVerifiedUserAsync("pre_vizsla", "pre1234567");

        var response = await second.PutAsJsonAsync(
            $"/api/users/{secondId}/email",
            new UpdateEmailRequest("duchess@example.com"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("already registered", await ReadProblemDetailAsync(response));
    }

    [Fact]
    public async Task UpdateEmail_NewAddressCanBeVerifiedAndLoginStillWorks()
    {
        var (userId, client) = await RegisterVerifiedUserAsync("ahsoka_t", "tano12345");

        await client.PutAsJsonAsync(
            $"/api/users/{userId}/email",
            new UpdateEmailRequest("ahsoka.tano@example.com"));

        var token = ExtractVerificationToken("ahsoka.tano@example.com");
        var verifyResponse = await Client.PostAsJsonAsync("/api/auth/verify-email", new VerifyEmailRequest(token));
        verifyResponse.EnsureSuccessStatusCode();

        var loginResponse = await Client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("ahsoka_t", "tano12345"));
        loginResponse.EnsureSuccessStatusCode();
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.Equal("ahsoka_t", auth.User.Username);
    }

    [Fact]
    public async Task UpdatePassword_WithCorrectCurrentPassword_AllowsLoginWithNewPassword()
    {
        var (userId, client) = await RegisterVerifiedUserAsync("sabine_w", "wren12345");

        var response = await client.PutAsJsonAsync(
            $"/api/users/{userId}/password",
            new UpdatePasswordRequest("wren12345", "paintgall1"));

        response.EnsureSuccessStatusCode();

        var oldLogin = await Client.PostAsJsonAsync("/api/auth/login", new LoginRequest("sabine_w", "wren12345"));
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);

        var newLogin = await Client.PostAsJsonAsync("/api/auth/login", new LoginRequest("sabine_w", "paintgall1"));
        newLogin.EnsureSuccessStatusCode();
        var auth = await newLogin.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.Equal("sabine_w", auth.User.Username);
    }

    [Fact]
    public async Task UpdatePassword_WithWrongCurrentPassword_ReturnsBadRequest()
    {
        var (userId, client) = await RegisterVerifiedUserAsync("hera", "syndulla1");

        var response = await client.PutAsJsonAsync(
            $"/api/users/{userId}/password",
            new UpdatePasswordRequest("wrong-password", "spectreone"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("current password is incorrect", await ReadProblemDetailAsync(response));
    }

    [Fact]
    public async Task UpdatePassword_AsOtherUser_ReturnsForbidden()
    {
        var luke = await CreateClientAsAsync("luke", "luke123");

        var response = await luke.PutAsJsonAsync(
            $"/api/users/{SeedData.PadmeUserId}/password",
            new UpdatePasswordRequest("padme123", "noblequeen1"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<(Guid UserId, HttpClient Client)> RegisterVerifiedUserAsync(string username, string password)
    {
        var result = await RegisterUserAsync(new RegisterRequest(username, null, $"{username}@example.com", password));
        var token = ExtractVerificationToken(result.Email);
        var verifyResponse = await Client.PostAsJsonAsync("/api/auth/verify-email", new VerifyEmailRequest(token));
        verifyResponse.EnsureSuccessStatusCode();

        var client = await CreateClientAsAsync(username, password);
        return (result.UserId, client);
    }

    private static async Task<string> ReadProblemDetailAsync(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.TryGetProperty("detail", out var detail) ? detail.GetString() ?? string.Empty : string.Empty;
    }
}
