using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using StarWarsTimelines.Application.Dtos;
using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Api.Tests;

public sealed class AuthEndpointsTests : ApiTestBase
{
    public AuthEndpointsTests(StarWarsTimelinesApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenAndUser()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new LoginRequest("padme", "padme123"));

        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();

        Assert.NotNull(auth);
        Assert.False(string.IsNullOrEmpty(auth.AccessToken));
        Assert.NotNull(auth.User);
        Assert.Equal("padme", auth.User.Username);
        Assert.Equal("Padmé Amidala", auth.User.DisplayName);
        Assert.Equal(UserRole.Standard, auth.User.Role);
    }

    [Fact]
    public async Task Login_WithAdminCredentials_ReturnsAdminUser()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin", "admin123"));

        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();

        Assert.NotNull(auth);
        Assert.Equal(UserRole.Admin, auth.User.Role);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new LoginRequest("padme", "wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownUser_ReturnsUnauthorized()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new LoginRequest("nobody", "anything"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithValidPayload_ReturnsAccountAndSendsVerificationEmail()
    {
        var result = await RegisterUserAsync(
            new RegisterRequest("obiwan", "Obi-Wan Kenobi", "Obi.Wan@Example.com", "kenobi123"));

        Assert.False(Guid.Empty == result.UserId);
        Assert.Equal("obiwan", result.Username);
        Assert.Equal("Obi-Wan Kenobi", result.DisplayName);
        Assert.Equal("obi.wan@example.com", result.Email);

        var email = Factory.EmailSender.Sent.Single(x => x.To == "obi.wan@example.com");
        Assert.Equal("Verify your Star Wars Timelines account", email.Subject);
        Assert.Contains("verify-email?token=", email.HtmlBody);
        Assert.NotEmpty(ExtractVerificationToken("obi.wan@example.com"));
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_CaseInsensitive_ReturnsBadRequest()
    {
        await RegisterUserAsync(new RegisterRequest("first", null, "Shared@Example.com", "password123"));

        var response = await Client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("second", null, "shared@example.com", "password123"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("already registered", await ReadProblemDetailAsync(response));
    }

    [Fact]
    public async Task Register_WithDuplicateUsername_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest("padme", null, "new@example.com", "password123"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("already exists", await ReadProblemDetailAsync(response));
    }

    [Fact]
    public async Task VerifyEmail_WithTokenFromRegistration_AllowsLogin()
    {
        await RegisterUserAsync(
            new RegisterRequest("ahsoka", "Ahsoka Tano", "ahsoka@example.com", "tano12345"));
        var token = ExtractVerificationToken("ahsoka@example.com");

        var verifyResponse = await Client.PostAsJsonAsync("/api/auth/verify-email", new VerifyEmailRequest(token));
        verifyResponse.EnsureSuccessStatusCode();

        var loginResponse = await Client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("ahsoka", "tano12345"));
        loginResponse.EnsureSuccessStatusCode();
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.Equal("ahsoka", auth.User.Username);
    }

    [Fact]
    public async Task VerifyEmail_WithInvalidToken_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/verify-email", new VerifyEmailRequest("not-a-real-token"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("invalid or has expired", await ReadProblemDetailAsync(response));
    }

    [Fact]
    public async Task Login_WithUnverifiedAccount_ReturnsUnauthorizedWithVerificationDetail()
    {
        await RegisterUserAsync(
            new RegisterRequest("grogu", null, "grogu@example.com", "yesplease1"));

        var response = await Client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("grogu", "yesplease1"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("verify your email", await ReadProblemDetailAsync(response));
    }

    [Fact]
    public async Task Login_AfterVerification_DoesNotSendAnotherEmail()
    {
        await RegisterUserAsync(new RegisterRequest("boba", null, "boba@example.com", "fettpassword"));
        var token = ExtractVerificationToken("boba@example.com");
        await Client.PostAsJsonAsync("/api/auth/verify-email", new VerifyEmailRequest(token));

        var before = Factory.EmailSender.Sent.Count;
        var response = await Client.PostAsJsonAsync("/api/auth/login", new LoginRequest("boba", "fettpassword"));
        response.EnsureSuccessStatusCode();

        Assert.Equal(before, Factory.EmailSender.Sent.Count);
    }

    [Fact]
    public async Task ResendVerificationEmail_WithUnverifiedUsername_ReturnsOkAndSendsFreshToken()
    {
        await RegisterUserAsync(new RegisterRequest("sabine", null, "sabine@example.com", "wrenpassword"));
        var originalToken = ExtractVerificationToken("sabine@example.com");
        var before = Factory.EmailSender.Sent.Count;

        var response = await Client.PostAsJsonAsync(
            "/api/auth/resend-verification-email",
            new ResendVerificationEmailRequest("sabine"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(before + 1, Factory.EmailSender.Sent.Count);
        Assert.NotEqual(originalToken, ExtractVerificationToken("sabine@example.com"));

        var newToken = ExtractVerificationToken("sabine@example.com");
        var verifyResponse = await Client.PostAsJsonAsync("/api/auth/verify-email", new VerifyEmailRequest(newToken));
        verifyResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ResendVerificationEmail_WithEmailIdentifier_ReturnsOkAndSendsEmail()
    {
        await RegisterUserAsync(new RegisterRequest("hondo", null, "hondo@example.com", "ohnakahara"));
        var before = Factory.EmailSender.Sent.Count;

        var response = await Client.PostAsJsonAsync(
            "/api/auth/resend-verification-email",
            new ResendVerificationEmailRequest("HONDO@example.com"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(before + 1, Factory.EmailSender.Sent.Count);
    }

    [Fact]
    public async Task ResendVerificationEmail_WithUnknownAccount_ReturnsOkWithoutSending()
    {
        var before = Factory.EmailSender.Sent.Count;

        var response = await Client.PostAsJsonAsync(
            "/api/auth/resend-verification-email",
            new ResendVerificationEmailRequest("no-such-user"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(before, Factory.EmailSender.Sent.Count);
    }

    [Fact]
    public async Task ResendVerificationEmail_WhenAlreadyVerified_ReturnsOkWithoutSending()
    {
        await RegisterUserAsync(new RegisterRequest("cad", null, "cad@example.com", "banebuster1"));
        var token = ExtractVerificationToken("cad@example.com");
        await Client.PostAsJsonAsync("/api/auth/verify-email", new VerifyEmailRequest(token));
        var before = Factory.EmailSender.Sent.Count;

        var response = await Client.PostAsJsonAsync(
            "/api/auth/resend-verification-email",
            new ResendVerificationEmailRequest("cad"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(before, Factory.EmailSender.Sent.Count);
    }

    private static async Task<string> ReadProblemDetailAsync(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.TryGetProperty("detail", out var detail) ? detail.GetString() ?? string.Empty : string.Empty;
    }
}
