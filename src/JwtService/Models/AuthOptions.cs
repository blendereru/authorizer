using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace JwtService.Models;

public static class AuthOptions
{
    public const string ISSUER = "Sanzhar";
    public const string AUDIENCE = "Sanzhar's Audience";
    const string KEY = "mysupersecret_secretsecretsecretkey!123";
    public static SymmetricSecurityKey GetSymmetricSecurityKey() =>
        new SymmetricSecurityKey(Encoding.Default.GetBytes(KEY));
}