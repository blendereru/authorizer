using System.Text;
using FingerprintPro.ServerSdk.Api;
using FingerprintPro.ServerSdk.Client;
using JwtService.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JwtService:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["JwtService:Audience"],
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.Default.GetBytes(builder.Configuration["JwtService:SecurityKey"]!)),
            ValidateIssuerSigningKey = true,
        };
    });
builder.Services.AddDbContext<ApplicationContext>(opts =>
{
    opts.UseSqlServer(builder.Configuration.GetConnectionString("UserService"));
});
builder.Services.AddSingleton<IFingerprintApi>(new FingerprintApi(
    new Configuration(builder.Configuration["FingerprintApiKey"]!)));
builder.Services.AddControllersWithViews();
var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();