using Microsoft.EntityFrameworkCore;
using TaskManagementAPI.Data;
using TaskManagementAPI.Models;
using TaskManagementAPI.Models.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace TaskManagementAPI.Services
{
    
    public class AuthService
    {
        private readonly TaskDbContext _dbContext;
        private readonly IConfiguration _configuration;

        public AuthService(TaskDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }
        public async Task<User> RegisterAsync(RegisterRequest request) 
        {
            // Email check
            var existingUser = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (existingUser != null) 
            {
                throw new Exception("Email already exists");
            }

            // Password hash
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Make user
            var user = new User
            {
                Email = request.Email,
                Name = request.Name,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow,
            };

            // Save
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            return user;
        }
        public async Task<AuthResponse?> LoginAsync(LoginRequest request) 
        {
            // user search
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null) 
            {
                return null;
            }

            // Password check
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

            if (!isPasswordValid)
            {
                return null;
            }

            // JWT token general
            var token = GenerateJwtToken(user);

            // AuthResponse back
            return new AuthResponse
            {
                Token = token,
                Email = user.Email,
                Name = user.Name,
            };
        }
        private string GenerateJwtToken(User user) 
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(365),
                signingCredentials : credentials
                );

            
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
