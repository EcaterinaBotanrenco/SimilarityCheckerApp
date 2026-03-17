using Microsoft.EntityFrameworkCore;
using SimilarityChecker.Api.Data;
using SimilarityChecker.Api.Data.Entities;
using SimilarityChecker.Api.Services.Auth;
using SimilarityChecker.Shared.Dtos;
using System.Security.Cryptography;
using SimilarityChecker.Api.Services.Email;
using System.Text;

namespace SimilarityChecker.Api.Services
{
    public sealed class AuthService : IAuthService
    {
        private readonly SimilarityCheckerDbContext _db;
        private readonly JwtTokenService _jwtTokenService;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;

        public AuthService(
            SimilarityCheckerDbContext db,
            JwtTokenService jwtTokenService,
            IConfiguration configuration,
            IEmailSender emailSender)
        {
            _db = db;
            _jwtTokenService = jwtTokenService;
            _configuration = configuration;
            _emailSender = emailSender;
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

            var resetUrl = $"https://localhost:5001/reset-password?email={Uri.EscapeDataString(user.Email)}&token={Uri.EscapeDataString(rawToken)}";

            var subject = "Resetare parolă - SimilarityChecker";

            var htmlBody = $@"
                <!DOCTYPE html>
                <html lang='ro'>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Resetare parolă</title>
                </head>
                <body style='margin:0; padding:0; background-color:#f3f6fb; font-family:Arial, Helvetica, sans-serif; color:#1f2937;'>
                    <table role='presentation' width='100%' cellspacing='0' cellpadding='0' style='background-color:#f3f6fb; padding:32px 16px;'>
                        <tr>
                            <td align='center'>
                                <table role='presentation' width='100%' cellspacing='0' cellpadding='0' style='max-width:600px; background:#ffffff; border-radius:18px; overflow:hidden; border:1px solid #e5e7eb;'>
                                    <tr>
                                        <td style='background:linear-gradient(135deg, #1d4ed8, #2563eb); padding:28px 32px; color:#ffffff;'>
                                            <div style='font-size:14px; opacity:0.9; margin-bottom:8px;'>SimilarityChecker</div>
                                            <h1 style='margin:0; font-size:28px; line-height:1.2; font-weight:700;'>Resetare parolă</h1>
                                        </td>
                                    </tr>

                                    <tr>
                                        <td style='padding:32px;'>
                                            <p style='margin:0 0 16px 0; font-size:16px; line-height:1.7; color:#374151;'>
                                                Ai solicitat resetarea parolei pentru contul tău din platforma <strong>SimilarityChecker</strong>.
                                            </p>

                                            <p style='margin:0 0 24px 0; font-size:16px; line-height:1.7; color:#374151;'>
                                                Pentru a seta o parolă nouă, apasă pe butonul de mai jos:
                                            </p>

                                            <table role='presentation' cellspacing='0' cellpadding='0' style='margin:0 0 28px 0;'>
                                                <tr>
                                                    <td>
                                                        <a href='{resetUrl}'
                                                           style='display:inline-block; background:#2563eb; color:#ffffff; text-decoration:none; padding:14px 24px; border-radius:10px; font-size:15px; font-weight:700;'>
                                                            Resetează parola
                                                        </a>
                                                    </td>
                                                </tr>
                                            </table>

                                            <div style='margin:0 0 24px 0; padding:16px 18px; background:#f9fafb; border:1px solid #e5e7eb; border-radius:12px;'>
                                                <p style='margin:0; font-size:14px; line-height:1.7; color:#4b5563;'>
                                                    Dacă butonul nu funcționează, poți copia și deschide manual linkul de mai jos:
                                                </p>
                                                <p style='margin:12px 0 0 0; word-break:break-all;'>
                                                    <a href='{resetUrl}' style='color:#2563eb; text-decoration:none; font-size:14px;'>
                                                        {resetUrl}
                                                    </a>
                                                </p>
                                            </div>

                                            <p style='margin:0 0 12px 0; font-size:15px; line-height:1.7; color:#374151;'>
                                                Dacă nu ai făcut tu această solicitare, poți ignora acest mesaj în siguranță.
                                            </p>

                                            <p style='margin:0; font-size:15px; line-height:1.7; color:#374151;'>
                                                Linkul de resetare este valabil timp de <strong>1 oră</strong>.
                                            </p>
                                        </td>
                                    </tr>

                                    <tr>
                                        <td style='padding:20px 32px; background:#f9fafb; border-top:1px solid #e5e7eb;'>
                                            <p style='margin:0; font-size:13px; line-height:1.6; color:#6b7280; text-align:center;'>
                                                Acest email a fost trimis automat de sistemul SimilarityChecker.
                                            </p>
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                    </table>
                </body>
                </html>";
            await _emailSender.SendAsync(user.Email, subject, htmlBody);
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

        public async Task ResetPasswordAsync(ResetPasswordRequestDto request)
        {
            var email = request.Email.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(request.Token))
                throw new InvalidOperationException("Link invalid sau incomplet.");

            var tokenHash = ComputeSha256(request.Token);

            var resetToken = await _db.PasswordResetTokens
                .Include(x => x.User)
                .FirstOrDefaultAsync(x =>
                    x.TokenHash == tokenHash &&
                    !x.IsUsed &&
                    x.ExpiresAtUtc > DateTime.UtcNow &&
                    x.User != null &&
                    x.User.Email == email);

            if (resetToken is null || resetToken.User is null)
                throw new InvalidOperationException("Linkul de resetare este invalid sau a expirat.");

            resetToken.User.PasswordHash = PasswordHasher.Hash(request.NewPassword);
            resetToken.IsUsed = true;

            var otherTokens = await _db.PasswordResetTokens
                .Where(x => x.UserId == resetToken.UserId && !x.IsUsed && x.Id != resetToken.Id)
                .ToListAsync();

            foreach (var token in otherTokens)
            {
                token.IsUsed = true;
            }

            await _db.SaveChangesAsync();
        }
    }
}