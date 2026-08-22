using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StarWarsTimelines.Application.Dtos;
using StarWarsTimelines.Persistence;

namespace StarWarsTimelines.Api.Tests;

public abstract class ApiTestBase : IClassFixture<StarWarsTimelinesApiFactory>
{
    protected const string AdminUsername = "admin";
    protected const string AdminPassword = "admin123";
    protected const string StandardUsername = "padme";
    protected const string StandardPassword = "padme123";

    protected readonly StarWarsTimelinesApiFactory Factory;
    protected readonly HttpClient Client;

    protected ApiTestBase(StarWarsTimelinesApiFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    protected async Task<HttpClient> CreateClientAsAsync(string username, string password)
    {
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new LoginRequest(username, password));
        loginResponse.EnsureSuccessStatusCode();

        var auth = (await loginResponse.Content.ReadFromJsonAsync<AuthResponse>())!;

        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    protected async Task<HttpClient> CreateAdminClientAsync() => await CreateClientAsAsync(AdminUsername, AdminPassword);

    protected async Task<HttpClient> CreateStandardClientAsync() => await CreateClientAsAsync(StandardUsername, StandardPassword);

    /// <summary>
    /// Registers a new user and returns the API response.
    /// </summary>
    /// <param name="request">The registration payload.</param>
    /// <returns>The registration response.</returns>
    protected async Task<RegisterResponse> RegisterUserAsync(RegisterRequest request)
    {
        var response = await Client.PostAsJsonAsync("/api/auth/register", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RegisterResponse>())!;
    }

    /// <summary>
    /// Extracts the verification token from the most recent captured email sent to an address.
    /// </summary>
    /// <param name="toAddress">The recipient whose verification email is read.</param>
    /// <returns>The raw verification token.</returns>
    protected string ExtractVerificationToken(string toAddress)
    {
        var email = Factory.EmailSender.Sent.Last(x => x.To == toAddress);
        const string marker = "?token=";
        var tokenStart = email.HtmlBody.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        Assert.True(tokenStart > marker.Length - 1, "No verification link was found in the email.");
        var tokenEnd = email.HtmlBody.IndexOf('"', tokenStart);
        if (tokenEnd < 0)
        {
            tokenEnd = email.HtmlBody.Length;
        }

        return email.HtmlBody[tokenStart..tokenEnd];
    }

    protected void ClearSourceMaterials()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.ExecuteSqlRaw("DELETE FROM UserSourceMaterials");
        db.Database.ExecuteSqlRaw("DELETE FROM SourceMaterials");
    }

    protected void ClearUserSourceMaterials()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.ExecuteSqlRaw("DELETE FROM UserSourceMaterials");
    }

    protected void ClearCharacters()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.ExecuteSqlRaw("DELETE FROM Characters");
    }

    protected void ClearLocations()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.ExecuteSqlRaw("DELETE FROM Locations");
    }

    protected void ClearVehicles()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.ExecuteSqlRaw("DELETE FROM Vehicles");
    }

    protected void ClearSpecies()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.ExecuteSqlRaw("DELETE FROM Species");
    }
}
