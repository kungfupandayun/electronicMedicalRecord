using ElectronicMedicalRecord.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ElectronicMedicalRecord.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IConfiguration configuration, ILogger<AuthController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest("Username and password are required");
                }

                // Simple authentication - in production, validate against database
                if (IsValidUser(request.Username, request.Password))
                {
                    var token = GenerateJwtToken(request.Username);
                    var expiration = DateTime.UtcNow.AddHours(24);

                    var response = new LoginResponse
                    {
                        Token = token,
                        Expiration = expiration,
                        Username = request.Username
                    };

                    _logger.LogInformation("User {Username} logged in successfully", request.Username);
                    return Ok(response);
                }

                _logger.LogWarning("Invalid login attempt for user {Username}", request.Username);
                return Unauthorized("Invalid username or password");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login attempt for user {Username}", request?.Username);
                return StatusCode(500, "Internal server error");
            }
        }

        private string GenerateJwtToken(string username)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured")));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, "MedicalStaff")
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private bool IsValidUser(string username, string password)
        {
            // Simple hardcoded validation for demo purposes
            // In production, validate against database with hashed passwords
            var validUsers = new Dictionary<string, string>
            {
                { "admin", "admin123" },
                { "doctor", "doctor123" },
                { "nurse", "nurse123" }
            };

            return validUsers.ContainsKey(username) && validUsers[username] == password;
        }
    }
}