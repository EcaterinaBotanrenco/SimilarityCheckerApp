using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimilarityChecker.Api.Data;
using SimilarityChecker.Api.Data.Entities;
using SimilarityChecker.Api.Services;
using SimilarityChecker.Shared.Dto;
using SimilarityChecker.Shared.Dtos;
using System;
using System.Security.Cryptography;
using System.Text;

namespace SimilarityChecker.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly SimilarityCheckerDbContext _db;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;
        private readonly IAuthService _authService;

        public AuthController(
            SimilarityCheckerDbContext db,
            IConfiguration configuration,
            ILogger<AuthController> logger,
            IAuthService authService)
        {
            _db = db;
            _configuration = configuration;
            _logger = logger;
            _authService = authService;
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                await _authService.ForgotPasswordAsync(request);

                return Ok(new
                {
                    message = "Dacă există un cont asociat acestui email, linkul de resetare a fost generat."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
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