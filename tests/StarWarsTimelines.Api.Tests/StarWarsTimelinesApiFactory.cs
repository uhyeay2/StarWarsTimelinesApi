using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using StarWarsTimelines.Application.Abstractions;

namespace StarWarsTimelines.Api.Tests;

public sealed class StarWarsTimelinesApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath;
    private readonly FakeEmailSender _emailSender = new();

    public StarWarsTimelinesApiFactory()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"starwarstimelines-test-{Guid.NewGuid():N}.db");
    }

    public string DatabasePath => _dbPath;

    /// <summary>
    /// Gets the in-memory email sender that captures messages instead of delivering them via SMTP.
    /// </summary>
    public FakeEmailSender EmailSender => _emailSender;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_dbPath};Pooling=False");
        builder.ConfigureServices(services => services.AddSingleton<IEmailSender>(_emailSender));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
}
