using Microsoft.AspNetCore.Mvc;
using THSR.Api.Data;
using THSR.Api.Helpers;
using THSR.Api.Models;

namespace THSR.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AuthController(AppDbContext db)
        {
            _db = db;
        }

        // POST /api/auth/register
        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email) ||
                string.IsNullOrWhiteSpace(req.Password) ||
                string.IsNullOrWhiteSpace(req.Name))
            {
                return BadRequest("Email、密碼與姓名為必填。");
            }

            // 檢查 Email 是否已存在
            var exists = _db.Users.Any(u => u.Email == req.Email);
            if (exists)
            {
                return BadRequest("此 Email 已被註冊。");
            }

            var user = new User
            {
                Email = req.Email,
                PasswordHash = PasswordHelper.HashPassword(req.Password),
                Name = req.Name,
                PhoneNumber = req.PhoneNumber,
                CreatedAt = DateTime.Now
            };

            _db.Users.Add(user);
            _db.SaveChanges();

            return Ok(new
            {
                userId = user.UserID,
                user.Name,
                user.Email
            });
        }

        // POST /api/auth/login
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Email) ||
                string.IsNullOrWhiteSpace(req.Password))
            {
                return BadRequest("Email 與密碼為必填。");
            }

            var user = _db.Users.FirstOrDefault(u => u.Email == req.Email);
            if (user == null)
            {
                // 不要暴露是哪個錯，統一回傳登入失敗
                return Unauthorized("帳號或密碼錯誤。");
            }

            var ok = PasswordHelper.VerifyPassword(req.Password, user.PasswordHash);
            if (!ok)
            {
                return Unauthorized("帳號或密碼錯誤。");
            }

            var resp = new LoginResponse
            {
                UserId = user.UserID,
                Name = user.Name,
                Email = user.Email
            };

            return Ok(resp);
        }
    }
}
