using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidIssuer = builder.Configuration["JwtBearer:Issuer"],
            ValidateIssuer = true,
            ValidAudience = builder.Configuration["JwtBearer:Audience"],
            ValidateAudience = true,
            IssuerSigningKey =
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtBearer:SecretKey"]!)),
            ValidateIssuerSigningKey = true
        };
    });
builder.Services.AddControllersWithViews();
var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();