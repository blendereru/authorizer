using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webapp.Models;

namespace webapp.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationContext _db;
    public AccountController(ApplicationContext db)
    {
        _db = db;
    }
    [HttpGet]
    public IActionResult Login() => View();
    [HttpPost]
    public async Task<IActionResult> Login([FromForm] User model)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserName == model.UserName);
        if (user != null)
        {
            //generate jwt token
        }

        return Ok();
    }
}