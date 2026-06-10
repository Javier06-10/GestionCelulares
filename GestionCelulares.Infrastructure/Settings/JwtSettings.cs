namespace GestionCelulares.Infrastructure.Settings;

public class JwtSettings
{
    public string Key { get; set; } = null!;
    public string Issuer { get; set; } = "GestionCelulares";
    public string Audience { get; set; } = "GestionCelulares";
    public int AccessTokenMinutes { get; set; } = 60;
    public int RefreshTokenDays { get; set; } = 7;
}
