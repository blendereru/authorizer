using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace webapp.Controllers;
[Authorize]
public class HomeController : Controller
{
    [HttpGet]
    public void Index()
    {
        
    }
}