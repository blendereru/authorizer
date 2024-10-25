namespace JwtService.Models;

public class RefreshSession
{
    public Guid Id { get; set; }
    public User User { get; set; }
    public string RefreshToken { get; set; }
    public string UA { get; set; } // User-Agent
    public string Fingerprint { get; set; }
    public string Ip { get; set; }
    public long ExpiresIn { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}