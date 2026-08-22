using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using Microsoft.OpenApi;
using StarWarsTimelines.Api;
using StarWarsTimelines.Api.Endpoints;
using StarWarsTimelines.Api.OpenApi;
using StarWarsTimelines.Application;
using StarWarsTimelines.Application.Abstractions;
using StarWarsTimelines.Persistence;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .Enrich.WithProperty("Application", "StarWarsTimelinesApi"),
        preserveStaticLogger: true);

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.OperationFilter<ExampleOperationFilter>();
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT token"
        });
        options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
    });
    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<ApiExceptionHandler>();

    var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
    if (corsOrigins is { Length: > 0 })
    {
        builder.Services.AddCors(options =>
            options.AddDefaultPolicy(policy =>
                policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));
    }

    var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
        ?? throw new InvalidOperationException("JWT settings are not configured.");
    builder.Services.AddSingleton(jwtOptions);

    var emailOptions = builder.Configuration.GetSection("Email").Get<EmailOptions>()
        ?? throw new InvalidOperationException("Email settings are not configured.");
    builder.Services.AddSingleton(emailOptions);
    builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
    builder.Services.AddSingleton<CatalogEventBroadcaster>();

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
            };
        });
    builder.Services.AddAuthorizationBuilder()
        .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));

    builder.Services.AddScoped<ISourceMaterialService, SourceMaterialService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IAccountService, AccountService>();
    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddScoped<ILibraryService, LibraryService>();
    builder.Services.AddScoped<ICharacterService, CharacterService>();
    builder.Services.AddScoped<ISpeciesService, SpeciesService>();
    builder.Services.AddScoped<ILocationService, LocationService>();
    builder.Services.AddScoped<IVehicleService, VehicleService>();
    builder.Services.AddScoped<ISourceMaterialEventService, SourceMaterialEventService>();
    builder.Services.AddScoped<ISourceMaterialUnitService, SourceMaterialUnitService>();

    var connectionString = builder.Configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

    builder.Services.AddPersistence(connectionString);

    var app = builder.Build();

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.000} ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestProtocol", httpContext.Request.Protocol);
            diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);

            var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is not null)
            {
                diagnosticContext.Set("UserId", userId);
            }
        };
        options.GetLevel = static (httpContext, _, exception) =>
            exception is not null || httpContext.Response.StatusCode >= 500
                ? LogEventLevel.Error
                : httpContext.Response.StatusCode >= 400
                    ? LogEventLevel.Warning
                    : LogEventLevel.Information;
    });

    app.UseExceptionHandler();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    if (corsOrigins is { Length: > 0 })
    {
        app.UseCors();
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapSourceMaterialEndpoints();
    app.MapAuthEndpoints();
    app.MapUserEndpoints();
    app.MapLibraryEndpoints();
    app.MapCharacterEndpoints();
    app.MapSpeciesEndpoints();
    app.MapLocationEndpoints();
    app.MapVehicleEndpoints();
    app.MapSourceMaterialEventEndpoints();
    app.MapSourceMaterialUnitEndpoints();
    app.MapCatalogEventEndpoints();

    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.Migrate();
        SeedData.Seed(dbContext);
    }

    app.Run();
    Log.Information("StarWarsTimelinesApi stopped cleanly.");
    return 0;
}
catch (Exception ex)
{
    // Startup or shutdown failure. The host disposes and flushes its Serilog providers on a graceful shutdown,
    // so no explicit CloseAndFlush is required here; the static Serilog logger is intentionally left alone
    // (see the preserveStaticLogger flag above) so the host can be rebuilt within the same process during tests.
    Log.Fatal(ex, "StarWarsTimelinesApi terminated unexpectedly.");
    return 1;
}

/// <summary>
/// The web application entry point. This partial declaration exposes the generated <c>Program</c> type so the
/// integration test factory can reference it via <c>WebApplicationFactory&lt;Program&gt;</c>.
/// </summary>
public partial class Program;
