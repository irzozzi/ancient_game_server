using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AncientServer.Data;
using AncientServer.Models;
using AncientServer.Services;

namespace AncientServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AncientGameContext _context;
        private readonly JwtService _jwtService;

        public AuthController(AncientGameContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        public class AuthRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] AuthRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { error = "Логин и пароль обязательны" });

            if (request.Username.Length < 3 || request.Username.Length > 20)
                return BadRequest(new { error = "Имя должно быть 3–20 символов" });

            if (request.Password.Length < 6)
                return BadRequest(new { error = "Пароль должен быть минимум 6 символов" });

            var existing = await _context.Players
                .FirstOrDefaultAsync(p => p.Username.ToLower() == request.Username.ToLower());
            if (existing != null)
                return BadRequest(new { error = "Имя уже занято" });

            var player = new Player
            {
                Username = request.Username.Trim(),
                PasswordHash = PasswordService.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                IsActive = true,
                Level = 1,
                Experience = 0,
                Platinum = 0,
                SpiritEnergy = 0
            };

            _context.Players.Add(player);
            await _context.SaveChangesAsync();

            var token = _jwtService.GenerateToken(player);

            return Ok(new
            {
                success = true,
                playerId = player.Id,
                username = player.Username,
                token = token,
                message = "Игрок зарегистрирован"
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AuthRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { error = "Логин и пароль обязательны" });

            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.Username.ToLower() == request.Username.ToLower());

            if (player == null)
                return Unauthorized(new { error = "Неверный логин или пароль" });

            if (!PasswordService.VerifyPassword(request.Password, player.PasswordHash))
                return Unauthorized(new { error = "Неверный логин или пароль" });

            player.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var token = _jwtService.GenerateToken(player);

            return Ok(new
            {
                success = true,
                playerId = player.Id,
                username = player.Username,
                token = token,
                message = "Успешный вход"
            });
        }
    }
}
