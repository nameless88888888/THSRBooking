using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using THSRBooking.Api.Data;
using THSRBooking.Api.Models;
using THSRBooking.Api.Services;

namespace THSRBooking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        // POST: /api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest("Email、密碼與姓名不可為空");
            }

            bool exists = await _db.Users.AnyAsync(u => u.Email == request.Email);
            if (exists)
            {
                return Conflict("Email 已被使用");
            }

            var user = new User
            {
                Email = request.Email,
                PasswordHash = PasswordHasher.Hash(request.Password),
                Name = request.Name,
                Phone = request.Phone,
                CreatedAt = DateTime.Now
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return Ok("註冊成功");
        }


        // POST: /api/auth/login
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("請輸入 Email 與密碼");
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                return Unauthorized("Email 或密碼錯誤");
            }

            if (!PasswordHasher.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized("Email 或密碼錯誤");
            }

            // 🔐 讀取 Jwt 設定（跟 Program.cs 用同一組）
            var jwtSection = _config.GetSection("Jwt");
            var keyBytes = Encoding.UTF8.GetBytes(jwtSection["Key"]!);
            var securityKey = new SymmetricSecurityKey(keyBytes);
            var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var expires = DateTime.UtcNow.AddMinutes(
                int.Parse(jwtSection["ExpireMinutes"] ?? "60")
            );

            var claims = new[]
            {
        new Claim("uid", user.UserId.ToString()),
        new Claim("email", user.Email),
        new Claim("name", user.Name)
    };

            var token = new JwtSecurityToken(
                issuer: jwtSection["Issuer"],      // "THSRBooking"
                audience: jwtSection["Audience"],  // "THSRBookingClient"
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            var response = new LoginResponse
            {
                Token = tokenString,
                ExpireAt = expires,
                UserId = user.UserId,
                Name = user.Name,
                Email = user.Email
            };

            return Ok(response);
        }


    }
}
