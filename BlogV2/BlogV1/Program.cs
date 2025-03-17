using System.Text;
using Microsoft.EntityFrameworkCore;
//tokens namespace
using Microsoft.IdentityModel.Tokens;
//jwt namespace need to be installed manually
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi;
//required for password hashing
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

//services for swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        Description = "Enter your JWT token in the format **Bearer {your token here}**"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("allowAll", policy =>
    {
        policy.AllowAnyHeader()
        .AllowAnyMethod()
        .AllowAnyOrigin();
    });
});

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

//add to check
SeedAdminUser(app);

app.UseStaticFiles();

//enable swagger only in development
if(app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("allowAll");

//we enable authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

//app.MapGet("/", () => "Hello World!");

app.MapGet("/", (context) =>
{
    //context.Response.Redirect("/index.html");
    context.Response.Redirect("/index.html");
    return Task.CompletedTask;
});

void SeedAdminUser(WebApplication app)
{
    using(var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        //check if database is available before seeding
        if(context.Database.CanConnect() && !context.Users.Any(u => u.UserRole == UserRole.Admin))
        {
            var passwordHasher = new PasswordHasher<User>();
            var adminUser = new User
            {
                UserRole = UserRole.Admin,
                UserName = "admin",
                Email = "admin@email.com",
                Password = passwordHasher.HashPassword(null, "1234") //secure hashed password
            };
            context.Users.Add(adminUser);
            context.SaveChanges();
            Console.WriteLine("admin user created successfully"); //log the success
        }
    }
}

app.Run();
