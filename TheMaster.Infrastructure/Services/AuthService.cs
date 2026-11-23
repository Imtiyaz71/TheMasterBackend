using System;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TheMaster.Application.DTOs;
using TheMaster.Application.Interfaces;
using TheMaster.Domain.Entities;
using TheMaster.Infrastructure.Data;

namespace TheMaster.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IConfiguration _config;

        public AuthService(IDbConnectionFactory connectionFactory, IConfiguration config)
        {
            _connectionFactory = connectionFactory;
            _config = config;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            using var db = _connectionFactory.CreateConnection();
            var normalizedEmail = dto.Email.Trim().ToLowerInvariant();

            var exists = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Users WHERE LOWER(Email) = @Email", 
                new { Email = normalizedEmail }) > 0;

            if (exists)
            {
                throw new InvalidOperationException("An account with this email already exists.");
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var sql = @"
                INSERT INTO Users (FullName, Email, PasswordHash, PhoneNumber, Role, IsActive, CreatedAt)
                VALUES (@FullName, @Email, @PasswordHash, @PhoneNumber, 'Student', 1, SYSUTCDATETIME());
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var userId = await db.ExecuteScalarAsync<int>(sql, new
            {
                FullName = dto.FullName.Trim(),
                Email = normalizedEmail,
                PasswordHash = passwordHash,
                PhoneNumber = dto.PhoneNumber?.Trim()
            });

            var user = new User
            {
                Id = userId,
                FullName = dto.FullName.Trim(),
                Email = normalizedEmail,
                Role = "Student"
            };

            var token = GenerateJwtToken(user);

            return new AuthResponseDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                Token = token
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            using var db = _connectionFactory.CreateConnection();
            var normalizedEmail = dto.Email.Trim().ToLowerInvariant();

            // 1. Check AdminLogin table first (for Admin Panel login, e.g. abc@bd.com / 123)
            var admin = await db.QueryFirstOrDefaultAsync<AdminLogin>(
                "SELECT * FROM AdminLogin WHERE LOWER(Email) = @Email",
                new { Email = normalizedEmail });

            if (admin != null)
            {
                bool isPasswordValid = admin.Password == dto.Password
                    || (!string.IsNullOrEmpty(admin.PasswordHash) && BCrypt.Net.BCrypt.Verify(dto.Password, admin.PasswordHash))
                    || BCrypt.Net.BCrypt.Verify(dto.Password, admin.Password);

                if (!isPasswordValid)
                {
                    throw new UnauthorizedAccessException("Invalid email or password.");
                }

                if (!admin.IsActive)
                {
                    throw new UnauthorizedAccessException("Your admin account is deactivated. Please contact support.");
                }

                // Ensure corresponding record in Users table exists so foreign keys (Orders, etc.) never fail
                var correspondingUser = await db.QueryFirstOrDefaultAsync<User>(
                    "SELECT * FROM Users WHERE LOWER(Email) = @Email", new { Email = normalizedEmail });

                int effectiveUserId;
                if (correspondingUser == null)
                {
                    var insertUserSql = @"
                        INSERT INTO Users (FullName, Email, PasswordHash, PhoneNumber, Role, IsActive, CreatedAt)
                        VALUES (@FullName, @Email, @PasswordHash, '01700000000', @Role, 1, SYSUTCDATETIME());
                        SELECT CAST(SCOPE_IDENTITY() as int);";
                    effectiveUserId = await db.ExecuteScalarAsync<int>(insertUserSql, new
                    {
                        FullName = admin.FullName,
                        Email = normalizedEmail,
                        PasswordHash = !string.IsNullOrEmpty(admin.PasswordHash) ? admin.PasswordHash : BCrypt.Net.BCrypt.HashPassword(dto.Password),
                        Role = string.IsNullOrWhiteSpace(admin.Role) ? "Admin" : admin.Role
                    });
                }
                else
                {
                    effectiveUserId = correspondingUser.Id;
                }

                var adminUser = new User
                {
                    Id = effectiveUserId,
                    FullName = admin.FullName,
                    Email = admin.Email,
                    Role = string.IsNullOrWhiteSpace(admin.Role) ? "Admin" : admin.Role
                };

                var adminToken = GenerateJwtToken(adminUser);

                return new AuthResponseDto
                {
                    UserId = adminUser.Id,
                    FullName = adminUser.FullName,
                    Email = adminUser.Email,
                    Role = adminUser.Role,
                    Token = adminToken
                };
            }

            // 2. Check Users table (for students and general users)
            var user = await db.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM Users WHERE LOWER(Email) = @Email", 
                new { Email = normalizedEmail });

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException("Your account is deactivated. Please contact support.");
            }

            var token = GenerateJwtToken(user);

            return new AuthResponseDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                Token = token
            };
        }

        public async Task<AuthResponseDto> GetProfileAsync(int userId, string? userEmail = null)
        {
            using var db = _connectionFactory.CreateConnection();

            // 1. Try finding in Users table first (so students and users always get their own profile)
            User? user = null;
            if (!string.IsNullOrWhiteSpace(userEmail))
            {
                user = await db.QueryFirstOrDefaultAsync<User>(
                    "SELECT Id, FullName, Email, PhoneNumber, Role, IsActive, CreatedAt FROM Users WHERE LOWER(Email) = @Email",
                    new { Email = userEmail.Trim().ToLowerInvariant() });
            }

            if (user == null && userId > 0)
            {
                user = await db.QueryFirstOrDefaultAsync<User>(
                    "SELECT Id, FullName, Email, PhoneNumber, Role, IsActive, CreatedAt FROM Users WHERE Id = @Id", 
                    new { Id = userId });
            }

            if (user != null)
            {
                return new AuthResponseDto
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role,
                    Token = string.Empty
                };
            }

            // 2. Fallback to AdminLogin table
            AdminLogin? admin = null;
            if (!string.IsNullOrWhiteSpace(userEmail))
            {
                admin = await db.QueryFirstOrDefaultAsync<AdminLogin>(
                    "SELECT Id, FullName, Email, Role, IsActive, CreatedAt FROM AdminLogin WHERE LOWER(Email) = @Email",
                    new { Email = userEmail.Trim().ToLowerInvariant() });
            }

            if (admin == null && userId > 0)
            {
                admin = await db.QueryFirstOrDefaultAsync<AdminLogin>(
                    "SELECT Id, FullName, Email, Role, IsActive, CreatedAt FROM AdminLogin WHERE Id = @Id", 
                    new { Id = userId });
            }

            if (admin != null)
            {
                return new AuthResponseDto
                {
                    UserId = admin.Id,
                    FullName = admin.FullName,
                    Email = admin.Email,
                    Role = admin.Role,
                    Token = string.Empty
                };
            }

            throw new KeyNotFoundException("User not found.");
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _config["JwtSettings:Secret"] ?? "TheMasterEnterpriseUltraSecureSuperSecretKey20261121!#@$";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var expiryMinutes = int.TryParse(_config["JwtSettings:ExpiryMinutes"], out var exp) ? exp : 10080;

            var token = new JwtSecurityToken(
                issuer: _config["JwtSettings:Issuer"] ?? "TheMasterAPI",
                audience: _config["JwtSettings:Audience"] ?? "TheMasterClients",
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
