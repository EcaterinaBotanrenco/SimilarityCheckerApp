using Microsoft.EntityFrameworkCore;
using SimilarityChecker.Api.Data;
using SimilarityChecker.Api.Data.Entities;
using SimilarityChecker.Api.Services.Auth;
using SimilarityChecker.Shared.Dtos;
using System.Security.Cryptography;
using System.Text;

namespace SimilarityChecker.Api.Services
{
    public sealed class AuthService : IAuthService
    {
        private readonly SimilarityCheckerDbContext _db;
        private readonly JwtTokenService _jwtTokenService;

        public AuthService(SimilarityCheckerDbContext db, JwtTokenService jwtTokenService)
        {
            _db = db;
            _jwtTokenService = jwtTokenService;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
        {
            var email = request.Email.Trim().ToLower();

            if (request.Role != "Student" && request.Role != "Teacher")
            {
                return new AuthResponseDto
                {
                    Success = false,
                    ErrorMessage = "Rol invalid."
                };
            }

            var exists = await _db.AppUsers.AnyAsync(x => x.Email == email);
            if (exists)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    ErrorMessage = "Există deja un cont cu acest email."
                };
            }

            var user = new AppUserEntity
            {
                Id = Guid.NewGuid(),
                Email = email,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                PasswordHash = PasswordHasher.Hash(request.Password),
                RolesCsv = request.Role
            };

            _db.AppUsers.Add(user);
            await _db.SaveChangesAsync();

            var (token, expiresAtUtc) = _jwtTokenService.CreateToken(user);

            return new AuthResponseDto
            {
                Success = true,
                Token = token,
                ExpiresAtUtc = expiresAtUtc,
                User = new CurrentUserDto
                {
                    Id = user.Id.ToString(),
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    DisplayName = $"{user.FirstName} {user.LastName}".Trim(),
                    Roles = new[] { request.Role }
                }
            };
        }

        public async Task ForgotPasswordAsync(ForgotPasswordRequestDto request)
        {
            Console.WriteLine("AM INTRAT IN ForgotPasswordAsync");

            var email = request.Email.Trim().ToLower();

            var user = await _db.AppUsers.FirstOrDefaultAsync(x => x.Email == email);

            // Nu divulgăm dacă utilizatorul există sau nu
            if (user is null)
                return;

            var rawToken = GenerateSecureToken();
            var tokenHash = ComputeSha256(rawToken);

            var oldTokens = await _db.PasswordResetTokens
                .Where(x => x.UserId == user.Id && !x.IsUsed && x.ExpiresAtUtc > DateTime.UtcNow)
                .ToListAsync();

            foreach (var oldToken in oldTokens)
            {
                oldToken.IsUsed = true;
            }

            var resetToken = new PasswordResetTokenEntity
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = tokenHash,
                ExpiresAtUtc = DateTime.UtcNow.AddHours(1),
                IsUsed = false,
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.PasswordResetTokens.Add(resetToken);
            await _db.SaveChangesAsync();

            var resetUrl = $"https://localhost:44375/reset-password?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(rawToken)}";

            Console.WriteLine("===== RESET PASSWORD LINK =====");
            Console.WriteLine(resetUrl);
            Console.WriteLine("================================");
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
        {
            var email = request.Email.Trim().ToLower();

            var user = await _db.AppUsers.FirstOrDefaultAsync(x => x.Email == email);
            if (user is null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    ErrorMessage = "Utilizator inexistent."
                };
            }

            var validPassword = PasswordHasher.Verify(request.Password, user.PasswordHash);
            if (!validPassword)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    ErrorMessage = "Parolă incorectă."
                };
            }

            var (token, expiresAtUtc) = _jwtTokenService.CreateToken(user);

            return new AuthResponseDto
            {
                Success = true,
                Token = token,
                ExpiresAtUtc = expiresAtUtc,
                User = new CurrentUserDto
                {
                    Id = user.Id.ToString(),
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    DisplayName = $"{user.FirstName} {user.LastName}".Trim(),
                    Roles = user.RolesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                }
            };
        }
        private static string GenerateSecureToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        private static string ComputeSha256(string input)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes);
        }
    }
}