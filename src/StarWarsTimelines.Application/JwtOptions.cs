namespace StarWarsTimelines.Application;

/// <summary>
/// Configuration options for issuing and validating JWT bearer tokens.
/// </summary>
/// <param name="Issuer">The issuer that issued and signed the token.</param>
/// <param name="Audience">The audience the token is intended for.</param>
/// <param name="SecretKey">The symmetric signing key used to sign and validate tokens.</param>
/// <param name="ExpiryMinutes">The number of minutes a token remains valid after being issued.</param>
/// <param name="RefreshTokenExpiryDays">The number of days a refresh token remains valid after being issued.</param>
public sealed record JwtOptions(string Issuer, string Audience, string SecretKey, int ExpiryMinutes, int RefreshTokenExpiryDays);
