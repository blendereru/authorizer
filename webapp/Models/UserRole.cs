namespace webapp.Models;

public class UserRole
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public User User { get; set; }
}