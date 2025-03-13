//namespaces
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

//identity model
using Microsoft.IdentityModel.Tokens;
//jwt token
using System.IdentityModel.Tokens.Jwt;
//security claims
using System.Security.Claims;
using System.Text;

//route name
[Route("auth")]
[ApiController]

public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    //Iconfiguration explanation
    private readonly IConfiguration _config;
    public AuthController(ApplicationDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }
    //common endpoints
    //we want to get a request from the login request
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.Password == request.Password);
        //this line checks if a user exists in the database with the provided email and password
        //now we check the users
        if (user == null)
        {
            return Unauthorized(new { Message = "Invalid email or password", Error = 401 });
        }
        //if user exists, we create a token
        //create an instance of GenerateJWTToken
        //this then calls the GenerateJWTToken(user) method to generate a JWT token for the authenticated user
        var token = GenerateJwtToken(user);
        return Ok(new { Token = token });
    }
    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _config.GetSection("Jwt");
        //check if key is empty or not
        var keyString = jwtSettings["Key"];
        if (string.IsNullOrEmpty(keyString))
        {
            throw new Exception("JWT token is missing from configuration");
        }
        var key = Encoding.UTF8.GetBytes(keyString);
        //claims are key-value pairs that store the user's identity details inside the JWT token
        var claims = new List<Claim>
        {
            //ClaimTypes.NameIdentifies stores the user's ID
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            //this one store's the user's username
            new Claim(ClaimTypes.Name, user.UserName),
            //next, stores the user's role(Admin, Writer or Reader) to manage access control
            new Claim(ClaimTypes.Role, user.UserRole.ToString()) //store the role in token
        };
        //defines how the token should be structured
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            // Subject = new ClaimsIdentity(claims): Assigns user identity details (claims) to the token
            Subject = new ClaimsIdentity(claims),
            //Expires: sets the expiration time for the token
            Expires = DateTime.UtcNow.AddMinutes(Convert.ToInt32(jwtSettings["ExpirationInMinutes"])),
            // SigningCredentials: Signs the token using HMAC SHA-256 encryption for security
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256),
            //Issuer defines the entity that issues the token(from appsettings.json)
            Issuer = jwtSettings["Issuer"],
            //Audience defines the expected recipient of the token(from appsettings.json)
            Audience = jwtSettings["Audience"]
        };
        //JwtSecurityHandler
        var tokenHandler = new JwtSecurityTokenHandler();
        //CreateToken(tokenDecriptor) generates a token based on the defined properties
        var token = tokenHandler.CreateToken(tokenDescriptor);
        //WriteToken(token) converts the token into a string format that can be sent to the client
        return tokenHandler.WriteToken(token);
    }
}

//define the public Login Request model
public class LoginRequest
{
    public string? Email { get; set; }
    public string? Password { get; set; }
}