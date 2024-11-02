using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using FingerprintPro.ServerSdk.Api;
using JwtService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace JwtService.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationContext _db;
    private readonly IFingerprintApi _fingerprintApi;
    public AccountController(ApplicationContext db,IFingerprintApi fingerprintApi)
    {
        _db = db;
        _fingerprintApi = fingerprintApi;
    }
    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    public async Task<IActionResult> Login([FromForm] LoginModel model)
    {
        if (ModelState.IsValid)
        {
            var fingerprintEvent = await _fingerprintApi.GetEventAsync(model.RequestId);
            var identification = fingerprintEvent.Products.Identification.Data;
            var confidence = identification.Confidence.Score;
            if (identification.VisitorId != model.VisitorId)
            {
                ModelState.AddModelError(string.Empty, "Forged Visitor ID.");
                return View(model);
            }
            var identifiedAt = DateTimeOffset.FromUnixTimeMilliseconds(identification.Timestamp ??
                                                                       throw new FormatException(
                                                                           "Missing identification timestamp"));
            if (DateTimeOffset.UtcNow - identifiedAt > TimeSpan.FromMinutes(2))
            {
                ModelState.AddModelError(string.Empty, "Expired identification timestamp.");
                return View(model);
            }
            if (confidence < 0.9f)
            {
                ModelState.AddModelError(string.Empty, "Low confidence identification score.");
                return View(model);
            }

            var startDate = DateTime.UtcNow.AddDays(-7);
            if (_db.Users.Count(x => x.Fingerprint == model.VisitorId &&
                                     x.RegistrationDate >= startDate) >= 5)
            {
                ModelState.AddModelError(string.Empty, "You cannot register another account using this browser.");
                return View(model);
            }

            var applicationUser = new User()
            {
                UserName = model.Email,
                Password = model.Password,
                Fingerprint = model.VisitorId
            };
            applicationUser.RegistrationDate = DateTime.UtcNow;
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserName == applicationUser.UserName &&
                                                                u.Password == applicationUser.Password);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt");
                return View(model);
            }

            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };
            var jwt = new JwtSecurityToken(
                issuer: AuthOptions.ISSUER,
                audience: AuthOptions.AUDIENCE,
                claims: claims,
                expires: DateTime.UtcNow.Add(TimeSpan.FromMinutes(30)),
                signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(),
                    SecurityAlgorithms.HmacSha256));
            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                var refreshToken = Convert.ToBase64String(randomNumber);
                Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions()
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    MaxAge = TimeSpan.FromDays(7)
                });
                var session = new RefreshSession()
                {
                    User = user,
                    RefreshToken = refreshToken,
                    UA = Request.Headers["User-Agent"].ToString(),
                    Ip = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Fingerprint = model.VisitorId,
                    ExpiresIn = DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeMilliseconds()
                };
                user.RefreshSessions.Add(session);
                _db.Update(user);
                await _db.SaveChangesAsync();
                var result = new
                {
                    access_token = accessToken,
                    refresh_token = refreshToken
                };
                return Ok(result);
            }
        }
        return View(model);
    }
    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    public async Task<IActionResult> Register([FromForm] RegisterModel model)
    {
        if (ModelState.IsValid)
        {
            var fingerprintEvent = await _fingerprintApi.GetEventAsync(model.RequestId);
            var identification = fingerprintEvent.Products.Identification.Data;
            if (identification.VisitorId != model.VisitorId)
            {
                ModelState.AddModelError(string.Empty, "Forged Visitor ID.");
                return View(model);
            }
            var identifiedAt = DateTimeOffset.FromUnixTimeMilliseconds(identification.Timestamp ?? 
                                                                       throw new FormatException("Missing identification timestamp"));
            if (DateTimeOffset.UtcNow - identifiedAt > TimeSpan.FromMinutes(2))
            {
                ModelState.AddModelError(string.Empty, "Expired identification timestamp.");
                return View(model);
            }

            var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.UserName == model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "User already exists.");
                return View(model);
            }

            var user = new User()
            {
                UserName = model.Email,
                Password = model.Password,
                Fingerprint = model.VisitorId,
                RegistrationDate = DateTime.UtcNow
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            var refreshToken = Convert.ToBase64String(randomNumber);
            Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions()
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                MaxAge = TimeSpan.FromDays(7)
            });
            var session = new RefreshSession()
            {
                User = user,
                RefreshToken = refreshToken,
                UA = Request.Headers["User-Agent"].ToString(),
                Ip = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Fingerprint = model.VisitorId,
                ExpiresIn = DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeMilliseconds()
            };
            user.RefreshSessions.Add(session);
            _db.Update(user);
            await _db.SaveChangesAsync();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };
            var jwt = new JwtSecurityToken(
                issuer: AuthOptions.ISSUER,
                audience: AuthOptions.AUDIENCE,
                claims: claims,
                expires: DateTime.UtcNow.Add(TimeSpan.FromMinutes(30)),
                signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(),
                    SecurityAlgorithms.HmacSha256));
            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);
        
            var result = new
            {
                access_token = accessToken,
                refresh_token = refreshToken
            };
        
            return Ok(result);
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Refresh([FromBody] string fingerprint)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(fingerprint);
        if (Request.Cookies.TryGetValue("refreshToken", out var refreshToken))
        {
            var session = await _db.RefreshSessions
                .Include(u => u.User)
                .FirstOrDefaultAsync(r => r.RefreshToken == refreshToken);
            if (session != null)
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                if (session.ExpiresIn < now)
                {
                    return Unauthorized("Refresh token expired.");
                }
                if (session.Fingerprint != fingerprint)
                {
                    return BadRequest("Forged visitor id");
                }

                var user = session.User;
                var claims = new List<Claim>()
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.UserName)
                };
                var jwt = new JwtSecurityToken(
                    issuer: AuthOptions.ISSUER,
                    audience: AuthOptions.AUDIENCE,
                    claims: claims,
                    expires: DateTime.UtcNow.Add(TimeSpan.FromMinutes(30)),
                    signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(),
                        SecurityAlgorithms.HmacSha256));
                var newAccessToken = new JwtSecurityTokenHandler().WriteToken(jwt);
                var randomNumber = new byte[32];
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(randomNumber);
                var newRefreshToken = Convert.ToBase64String(randomNumber);
                var newExpiresIn = DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeMilliseconds();
                _db.RefreshSessions.Remove(session);
                var newSession = new RefreshSession()
                {
                    User = user,
                    Ip = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UA = Request.Headers["User-Agent"].ToString(),
                    RefreshToken = newRefreshToken,
                    ExpiresIn = newExpiresIn,
                    Fingerprint = fingerprint
                };
                _db.RefreshSessions.Add(newSession);
                await _db.SaveChangesAsync();
                Response.Cookies.Append("refreshToken", newRefreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,   // Use HTTPS only.
                    SameSite = SameSiteMode.Strict, 
                    Expires = DateTimeOffset.FromUnixTimeMilliseconds(newExpiresIn)
                });
                return Ok(new { access_token = newAccessToken });
            }
            return Unauthorized("Invalid refresh token.");
        }
        return Unauthorized("Refresh token not found.");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        if (Request.Cookies.TryGetValue("refreshToken", out var refreshToken))
        {
            var session = await _db.RefreshSessions.FirstOrDefaultAsync(r => r.RefreshToken == refreshToken);
            if (session != null)
            {
                _db.RefreshSessions.Remove(session);
                await _db.SaveChangesAsync();
            }
            else
            {
                return BadRequest(new { message = "Session not found." }); // Handle case where session does not exist
            }
        }
        Response.Cookies.Delete("refreshToken");
        return Ok(new { message = "Successfully logged out." });
    }
    
    [HttpGet]
    public IActionResult Index() => View();
}