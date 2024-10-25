using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JwtService.Controllers;
[Authorize]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity.Name != null)
        {
            return Ok($"Hello,  {User.Identity.Name}");
        }
        return BadRequest("Token is not validated");
    }
}