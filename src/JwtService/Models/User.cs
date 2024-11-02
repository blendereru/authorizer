namespace JwtService.Models;

public class User
{
    public Guid Id { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public string Fingerprint { get; set; }
    public DateTime RegistrationDate { get; set; }
    public IList<RefreshSession> RefreshSessions = new List<RefreshSession>();
}