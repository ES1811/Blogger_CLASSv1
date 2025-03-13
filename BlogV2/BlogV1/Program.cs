using System.Text;
using Microsoft.EntityFrameworkCore;
//tokens namespace
using Microsoft.IdentityModel.Tokens;
//jwt namespace need to be installed manually
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

//register db context

builder.Services.AddDbContext<ApplicationDbContext>(options => 
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

//stop the json cycling
builder.Services.AddControllers()
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.WriteIndented = true; //pretty Json output
});

//we configure JWT after adding JWT in appsettings.json
var jwtSettings = builder.Configuration.GetSection("Jwt");
//then we get the key
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? "");
//we add authentication service, this is policy signing
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
    };
});
//enable Authorize attribute
builder.Services.AddAuthorization();

var app = builder.Build();

//we enable authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => "Hello World!");

app.Run();
