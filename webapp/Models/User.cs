using System.ComponentModel.DataAnnotations;

namespace webapp.Models;

public class User
{
    public Guid Id { get; set; }
    [Required]
    public string UserName { get; set; }
    [Required]
    public string Password { get; set; }

    public List<UserRole> UserRoles { get; set; } = new();
}