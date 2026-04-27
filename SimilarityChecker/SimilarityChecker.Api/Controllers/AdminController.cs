//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using SimilarityChecker.Api.Data;
//using SimilarityChecker.Api.Data.Entities;

//namespace SimilarityChecker.Api.Controllers
//{
//    [Route("api/admin")]
//    [ApiController]
//    [Authorize(Roles = "Admin")]  // Permite doar Adminilor să acceseze aceste rute
//    public class AdminController : Controller
//    {
//        private readonly SimilarityCheckerDbContext _db;

//        public AdminController(SimilarityCheckerDbContext db)
//        {
//            _db = db;
//        }

//        [HttpPost("upload-document")]
//        [Authorize(Roles = "Admin")]
//        public async Task<IActionResult> UploadDocument(IFormFile file)
//        {
//            if (file == null || file.Length == 0)
//                return BadRequest("Fără fișier încărcat.");

//            // Logica pentru încărcarea fișierului în baza de date
//            // Poți salva fișierul în folderul corespunzător pe server sau în baza de date

//            return Ok("Fișier încărcat cu succes.");
//        }

//        // 1. Atribuirea rolului Admin unui utilizator
//        [HttpPost("assign-admin/{userId}")]
//        public async Task<IActionResult> AssignAdminRole(Guid userId)
//        {
//            // Găsește utilizatorul
//            var user = await _db.AppUsers.FindAsync(userId);
//            if (user == null)
//                return NotFound("Utilizatorul nu a fost găsit.");

//            // Găsește rolul Admin
//            var adminRole = await _db.Set<RoleEntity>().FirstOrDefaultAsync(r => r.Name == "Admin");

//            if (adminRole == null)
//                return BadRequest("Rolul Admin nu există.");

//            // Verifică dacă rolul Admin este deja atribuit
//            var userRole = await _db.Set<AppUserRoleEntity>()
//                .FirstOrDefaultAsync(ur => ur.UserId == user.Id && ur.RoleId == adminRole.Id);

//            if (userRole == null)
//            {
//                // Dacă nu există relație, atribuie rolul Admin
//                _db.Set<AppUserRoleEntity>().Add(new AppUserRoleEntity
//                {
//                    UserId = user.Id,
//                    RoleId = adminRole.Id
//                });
//                await _db.SaveChangesAsync();
//            }

//            return Ok("Rolul de Admin a fost atribuit utilizatorului.");
//        }

//        // 2. Obține lista de utilizatori
//        [HttpGet("users")]
//        public async Task<IActionResult> GetUsers()
//        {
//            var users = await _db.AppUsers
//                .Select(u => new { u.Id, u.Email, UserName = $"{u.FirstName} {u.LastName}" })
//                .ToListAsync();

//            return Ok(users);
//        }
//    }
//}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SimilarityChecker.Api.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new { message = "Acces admin permis." });
        }
    }
}