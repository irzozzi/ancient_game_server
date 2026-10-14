using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AncientServer.Data;
using AncientServer.Models;
using AncientServer.Models.DTOs;

namespace AncientServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly AncientGameContext _context;

        public ProfileController(AncientGameContext context)
        {
            _context = context;
        }

        private string? GetPlayerIdFromToken()
        {
            return User.FindFirst("playerId")?.Value;
        }

        // GET: api/profile/me — свой профиль (только с токеном)
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var playerId = GetPlayerIdFromToken();
            if (string.IsNullOrEmpty(playerId))
                return Unauthorized(new { error = "Токен не содержит playerId" });

            var player = await _context.Players.FindAsync(playerId);
            if (player == null) return NotFound(new { error = "Игрок не найден" });

            return Ok(new
            {
                player.Id,
                player.Username,
                player.Level,
                player.Experience,
                player.Platinum,
                player.SpiritEnergy,
                player.AvatarUrl,
                player.Description,
                player.Language,
                player.NotificationsEnabled,
                player.CreatedAt,
                player.LastLogin,
                player.CityId,
                player.ContinentId
            });
        }

        // GET: api/profile/{playerId}/public — публичный профиль другого игрока
        [HttpGet("{playerId}/public")]
        public async Task<IActionResult> GetPublicProfile(string playerId)
        {
            var player = await _context.Players.FindAsync(playerId);
            if (player == null) return NotFound(new { error = "Игрок не найден" });

            var continent = player.ContinentId.HasValue
                ? await _context.Continents.FindAsync(player.ContinentId.Value)
                : null;

            return Ok(new
            {
                player.Id,
                player.Username,
                player.Level,
                player.AvatarUrl,
                player.Description,
                Continent = continent?.Name ?? "Не выбран",
                player.CreatedAt
            });
        }

        // PUT: api/profile — обновить свой профиль (только с токеном)
        [Authorize]
        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            if (request == null)
                return BadRequest(new { error = "Неверный запрос" });

            var playerId = GetPlayerIdFromToken();
            if (string.IsNullOrEmpty(playerId))
                return Unauthorized(new { error = "Токен не содержит playerId" });

            var player = await _context.Players.FindAsync(playerId);
            if (player == null) return NotFound(new { error = "Игрок не найден" });

            // Валидация
            if (request.AvatarUrl != null)
            {
                if (request.AvatarUrl.Length > 500)
                    return BadRequest(new { error = "Ссылка на аватар слишком длинная" });
                player.AvatarUrl = request.AvatarUrl.Trim();
            }

            if (request.Description != null)
            {
                if (request.Description.Length > 1000)
                    return BadRequest(new { error = "Описание не должно превышать 1000 символов" });
                player.Description = request.Description.Trim();
            }

            if (request.Language != null)
            {
                var allowed = new[] { "ru", "en" };
                if (!allowed.Contains(request.Language))
                    return BadRequest(new { error = "Недопустимый язык. Доступны: ru, en" });
                player.Language = request.Language;
            }

            if (request.NotificationsEnabled.HasValue)
            {
                player.NotificationsEnabled = request.NotificationsEnabled.Value;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Профиль обновлён",
                profile = new
                {
                    player.AvatarUrl,
                    player.Description,
                    player.Language,
                    player.NotificationsEnabled
                }
            });
        }
    }
}