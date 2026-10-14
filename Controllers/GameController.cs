using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AncientServer.Data;
using AncientServer.Models;
using AncientServer.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AncientServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameController : ControllerBase
    {
        private readonly AncientGameContext _context;
        private static readonly Random _rand = new Random();

        private const float CENTER_RADIUS = 0.15f;
        private const float MIN_DISTANCE = 0.1f;

        public GameController(AncientGameContext context)
        {
            _context = context;
        }

        // ========== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ==========
        private string? GetPlayerIdFromToken()
        {
            return User.FindFirst("playerId")?.Value;
        }

        private static bool IsTooCloseToCenter(float x, float z, float minDistance)
        {
            float dx = x - 0.5f;
            float dz = z - 0.5f;
            return Math.Sqrt(dx * dx + dz * dz) < minDistance;
        }

        private static bool IsOverlappingWithMines(float x, float z, List<Mine> mines, float minDist)
        {
            foreach (var m in mines)
                if (Math.Abs(m.LocalX - x) < minDist && Math.Abs(m.LocalZ - z) < minDist)
                    return true;
            return false;
        }

        private static bool IsOverlappingWithMobs(float x, float z, List<MobCamp> mobs, float minDist)
        {
            foreach (var m in mobs)
                if (Math.Abs(m.LocalX - x) < minDist && Math.Abs(m.LocalZ - z) < minDist)
                    return true;
            return false;
        }

        private static bool IsOverlappingWithDungeons(float x, float z, List<Dungeon> dungeons, float minDist)
        {
            foreach (var d in dungeons)
                if (Math.Abs(d.LocalX - x) < minDist && Math.Abs(d.LocalZ - z) < minDist)
                    return true;
            return false;
        }

        private static bool IsOverlappingWithSettlements(float x, float z, List<PlayerSettlement> settlements, float minDist)
        {
            foreach (var s in settlements)
                if (Math.Abs(s.LocalX - x) < minDist && Math.Abs(s.LocalZ - z) < minDist)
                    return true;
            return false;
        }

        private static bool IsOverlappingWithAll(float x, float z, City city, float minDist)
        {
            return IsOverlappingWithMines(x, z, city.Mines, minDist) ||
                   IsOverlappingWithMobs(x, z, city.MobCamps, minDist) ||
                   IsOverlappingWithDungeons(x, z, city.Dungeons, minDist) ||
                   IsOverlappingWithSettlements(x, z, city.Settlements, minDist);
        }

        private static void RespawnMine(City city, Mine oldMine)
        {
            city.Mines.Remove(oldMine);
            float newX, newZ;
            int attempts = 0;
            do
            {
                newX = (float)_rand.NextDouble();
                newZ = (float)_rand.NextDouble();
                attempts++;
                if (attempts > 500) break;
            } while (IsTooCloseToCenter(newX, newZ, CENTER_RADIUS) ||
                     IsOverlappingWithAll(newX, newZ, city, MIN_DISTANCE));

            city.Mines.Add(new Mine
            {
                Id = Guid.NewGuid().ToString(),
                Type = oldMine.Type,
                LocalX = newX,
                LocalZ = newZ,
                Amount = oldMine.MaxAmount,
                MaxAmount = oldMine.MaxAmount
            });
        }

        // ========== СИСТЕМНЫЕ МЕТОДЫ ==========
        [HttpGet("ping")]
        public IActionResult Ping() => Ok(new { message = "pong", serverTime = DateTime.UtcNow });

        [HttpGet("test")]
        public async Task<IActionResult> Test()
        {
            var playersCount = await _context.Players.CountAsync();
            return Ok(new { Message = "Сервер работает", Status = "online", Time = DateTime.UtcNow, PlayersCount = playersCount });
        }

        // ========== КОНТИНЕНТЫ ==========
        [HttpGet("continents")]
        public async Task<IActionResult> GetContinents([FromQuery] string? playerId = null)
        {
            Player? player = null;
            if (!string.IsNullOrEmpty(playerId))
                player = await _context.Players.FindAsync(playerId);

            var continents = await _context.Continents.ToListAsync();

            var result = continents.Select(c => new
            {
                c.Id,
                c.Name,
                c.Element,
                c.Description,
                Players = $"{c.CurrentPlayers}/{c.MaxPlayers}",
                c.LevelRequirement,
                CanSpawn = c.IsStarter || (player != null && player.Level >= c.LevelRequirement),
                IsLocked = !(c.IsStarter || (player != null && player.Level >= c.LevelRequirement))
            });
            return Ok(result);
        }

        // ========== ИГРОКИ ==========
        [HttpGet("players")]
        public async Task<IActionResult> GetPlayers()
        {
            var players = await _context.Players.ToListAsync();
            var continents = await _context.Continents.ToListAsync();

            var list = players.Select(p =>
            {
                var continent = p.ContinentId.HasValue ? continents.FirstOrDefault(c => c.Id == p.ContinentId) : null;
                return new
                {
                    p.Id,
                    p.Username,
                    p.Level,
                    p.Platinum,
                    p.SpiritEnergy,
                    Continent = continent?.Name ?? "Не выбран",
                    SpawnPosition = p.ContinentId.HasValue ? new { p.SpawnX, p.SpawnZ } : null,
                    p.CreatedAt
                };
            });
            return Ok(list);
        }

        [HttpGet("player/{id}")]
        public async Task<IActionResult> GetPlayer(string id)
        {
            var player = await _context.Players.FindAsync(id);
            if (player == null) return NotFound(new { error = "Игрок не найден" });

            Continent? continent = null;
            if (player.ContinentId.HasValue)
                continent = await _context.Continents.FindAsync(player.ContinentId.Value);

            return Ok(new
            {
                player.Id,
                player.Username,
                player.Level,
                player.Experience,
                player.Platinum,
                player.SpiritEnergy,
                Continent = continent?.Name ?? "Не выбран",
                Element = continent?.Element,
                SpawnPosition = player.ContinentId.HasValue ? new { player.SpawnX, player.SpawnZ } : null,
                player.CreatedAt,
                player.LastLogin,
                CityId = player.CityId != -1 ? player.CityId : (int?)null,
                SettlementLocal = player.CityId != -1 ? new { player.SettlementLocalX, player.SettlementLocalZ } : null
            });
        }

        // Старая регистрация — оставляем для совместимости с Unity-клиентом
        [HttpPost("register")]
        public async Task<IActionResult> RegisterPlayer([FromBody] RegisterRequest? request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || request.Username.Length < 3 || request.Username.Length > 20)
                return BadRequest(new { error = "Имя должно быть 3-20 символов" });

            var existing = await _context.Players.FirstOrDefaultAsync(p => p.Username.ToLower() == request.Username.ToLower());
            if (existing != null)
                return BadRequest(new { error = "Имя уже занято" });

            var player = new Player
            {
                Username = request.Username.Trim(),
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

            return Ok(new { success = true, playerId = player.Id, username = player.Username, message = "Зарегистрирован" });
        }

        // ========== СПАВН (ЗАЩИЩЁН JWT) ==========
        [Authorize]
        [HttpPost("spawn")]
        public async Task<IActionResult> SpawnOnContinent([FromBody] SpawnRequest? request)
        {
            if (request == null)
                return BadRequest(new { error = "Неверный запрос" });

            var playerId = GetPlayerIdFromToken();
            if (string.IsNullOrEmpty(playerId))
                return Unauthorized(new { error = "Токен не содержит playerId" });

            var player = await _context.Players.FindAsync(playerId);
            if (player == null) return NotFound(new { error = "Игрок не найден" });
            if (player.CityId != -1) return BadRequest(new { error = "Игрок уже имеет поселение" });

            var continent = await _context.Continents.FindAsync(request.ContinentId);
            if (continent == null) return NotFound(new { error = "Континент не найден" });
            if (!continent.IsStarter && player.Level < continent.LevelRequirement)
                return BadRequest(new { error = $"Требуется уровень {continent.LevelRequirement}" });
            if (continent.CurrentPlayers >= continent.MaxPlayers)
                return BadRequest(new { error = "Континент переполнен" });

            if (request.CityId <= 0) return BadRequest(new { error = "CityId обязателен" });

            var city = await _context.Cities.FindAsync(request.CityId);
            if (city == null) return BadRequest(new { error = "Город не найден" });
            if (city.ContinentId != continent.Id) return BadRequest(new { error = "Город не принадлежит выбранному континенту" });
            if (city.Level != 1) return BadRequest(new { error = "Вы можете заселяться только в город первого уровня (внешний круг)" });
            if (city.Settlements.Count >= 40) return BadRequest(new { error = "В этом городе нет свободных мест" });

            float localX, localZ;
            int attempts = 0;
            bool found = false;
            do
            {
                localX = (float)_rand.NextDouble();
                localZ = (float)_rand.NextDouble();
                attempts++;
                if (attempts > 1000) break;
                if (IsTooCloseToCenter(localX, localZ, CENTER_RADIUS)) continue;
                if (IsOverlappingWithAll(localX, localZ, city, MIN_DISTANCE)) continue;
                found = true;
                break;
            } while (true);

            if (!found) return BadRequest(new { error = "Не удалось найти свободное место в городе" });

            var settlement = new PlayerSettlement
            {
                PlayerId = player.Id,
                LocalX = localX,
                LocalZ = localZ,
                Level = 1
            };
            city.Settlements.Add(settlement);

            float worldX = city.CenterX + (localX - 0.5f) * 20f;
            float worldZ = city.CenterZ + (localZ - 0.5f) * 20f;

            int? oldContinentId = player.ContinentId;
            player.CityId = city.Id;
            player.SettlementLocalX = localX;
            player.SettlementLocalZ = localZ;
            player.ContinentId = continent.Id;
            player.SpawnX = worldX;
            player.SpawnZ = worldZ;
            player.CurrentX = worldX;
            player.CurrentZ = worldZ;
            player.LastLogin = DateTime.UtcNow;

            if (oldContinentId.HasValue && oldContinentId.Value != continent.Id)
            {
                var oldContinent = await _context.Continents.FindAsync(oldContinentId.Value);
                if (oldContinent != null && oldContinent.CurrentPlayers > 0) oldContinent.CurrentPlayers--;
            }
            continent.CurrentPlayers++;

            _context.Entry(city).Property(c => c.Settlements).IsModified = true;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = $"Добро пожаловать в город {city.Id} на {continent.Name}!",
                player = new
                {
                    player.Id,
                    player.Username,
                    player.Level,
                    player.Platinum,
                    player.SpiritEnergy,
                    Continent = continent.Name,
                    CityId = city.Id,
                    CityLevel = city.Level,
                    SettlementLocal = new { localX, localZ },
                    SpawnPosition = new { worldX, worldZ }
                }
            });
        }

        // ========== СБОР РЕСУРСОВ (ЗАЩИЩЁН JWT) ==========
        [Authorize]
        [HttpPost("mines/{mineId}/gather")]
        public async Task<IActionResult> GatherMine(string mineId, [FromBody] GatherMineRequest request)
        {
            var playerId = GetPlayerIdFromToken();
            if (string.IsNullOrEmpty(playerId))
                return Unauthorized(new { error = "Токен не содержит playerId" });

            var player = await _context.Players.FindAsync(playerId);
            if (player == null) return NotFound(new { error = "Игрок не найден" });

            var cities = await _context.Cities.ToListAsync();
            var city = cities.FirstOrDefault(c => c.Mines.Any(m => m.Id == mineId));
            if (city == null) return NotFound(new { error = "Шахта не найдена" });
            if (player.CityId != city.Id) return BadRequest(new { error = "Вы находитесь не в том городе" });

            var mine = city.Mines.First(m => m.Id == mineId);
            if (mine.Amount <= 0) return BadRequest(new { error = "Шахта истощена" });

            int gathered = Math.Min(request?.Amount ?? 100, mine.Amount);

            var continent = await _context.Continents.FindAsync(player.ContinentId);
            if (continent != null)
            {
                if (continent.BonusType == "defense" && mine.Type == "platinum")
                    gathered = (int)(gathered * 1.1f);
                else if (continent.BonusType == "speed" && mine.Type == "spirit")
                    gathered = (int)(gathered * 1.1f);
            }

            mine.Amount -= gathered;

            if (mine.Type == "platinum")
                player.Platinum += gathered;
            else
                player.SpiritEnergy += gathered;

            if (mine.Amount <= 0)
            {
                RespawnMine(city, mine);
            }

            _context.Entry(city).Property(c => c.Mines).IsModified = true;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                gathered = gathered,
                resourceType = mine.Type,
                remaining = mine.Amount,
                playerResources = new { player.Platinum, player.SpiritEnergy }
            });
        }

        // ========== ГОРОДА ==========
        [HttpGet("cities")]
        public async Task<IActionResult> GetCities(int continentId, float? minX = null, float? maxX = null, float? minZ = null, float? maxZ = null)
        {
            var query = _context.Cities.Where(c => c.ContinentId == continentId);

            if (minX.HasValue && maxX.HasValue && minZ.HasValue && maxZ.HasValue)
                query = query.Where(c => c.CenterX >= minX && c.CenterX <= maxX && c.CenterZ >= minZ && c.CenterZ <= maxZ);

            var cities = await query.ToListAsync();

            var result = cities.Select(c => new
            {
                c.Id,
                c.Level,
                c.CenterX,
                c.CenterZ,
                SettlementsCount = c.Settlements.Count,
                c.CastleOwnerGuildId
            });
            return Ok(result);
        }

        [HttpGet("cities/{cityId}")]
        public async Task<IActionResult> GetCity(int cityId)
        {
            var city = await _context.Cities.FindAsync(cityId);
            if (city == null) return NotFound("City not found");

            bool changed = false;
            foreach (var mob in city.MobCamps)
            {
                if (!mob.IsAlive && mob.RespawnTime.HasValue && DateTime.UtcNow >= mob.RespawnTime.Value)
                {
                    mob.IsAlive = true;
                    mob.Health = mob.MaxHealth;
                    mob.RespawnTime = null;
                    changed = true;
                }
            }

            if (changed)
            {
                _context.Entry(city).Property(c => c.MobCamps).IsModified = true;
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                city.Id,
                city.Level,
                city.Name,
                city.Buff,
                city.CenterX,
                city.CenterZ,
                city.CastleOwnerGuildId,
                Settlements = city.Settlements.Select(s => new { s.PlayerId, s.LocalX, s.LocalZ, s.Level }),
                Mines = city.Mines.Select(m => new { m.Id, m.Type, m.LocalX, m.LocalZ, m.Amount, m.MaxAmount }),
                MobCamps = city.MobCamps.Select(m => new { m.Id, m.MobType, m.Level, m.LocalX, m.LocalZ, m.IsAlive, m.Health, m.MaxHealth, m.RespawnTime }),
                Dungeons = city.Dungeons.Select(d => new { d.Id, d.DungeonType, d.LocalX, d.LocalZ, d.IsAvailable })
            });
        }

        // ========== СТАТИСТИКА ==========
        [HttpGet("world-stats")]
        public async Task<IActionResult> GetWorldStats()
        {
            await UpdateContinentCounters();

            var totalPlayers = await _context.Players.CountAsync();
            var players = await _context.Players.ToListAsync();
            var continents = await _context.Continents.ToListAsync();

            var playersOnCentral = players.Count(p =>
                p.ContinentId.HasValue &&
                continents.Any(c => c.Id == p.ContinentId.Value && c.IsCentral));

            return Ok(new
            {
                totalPlayers,
                playersOnCentral,
                continents = continents.Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Element,
                    players = c.CurrentPlayers,
                    maxPlayers = c.MaxPlayers,
                    status = c.CurrentPlayers >= c.MaxPlayers ? "full" : "available"
                })
            });
        }

        [HttpGet("world-map")]
        public async Task<IActionResult> GetWorldMap()
        {
            var continents = await _context.Continents.ToListAsync();

            var result = continents.Select(c => new
            {
                c.Id,
                c.Name,
                c.Element,
                Position = new { c.CenterX, c.CenterZ },
                Radius = c.IsCentral ? 150 : 100,
                Color = c.Element switch
                {
                    "earth" => "#8B4513",
                    "wind" => "#87CEEB",
                    "fire" => "#FF4500",
                    "water" => "#1E90FF",
                    "void" => "#9370DB",
                    _ => "#808080"
                },
                c.IsStarter,
                c.IsCentral,
                PlayerCount = c.CurrentPlayers
            });

            var connections = continents.Where(c => !c.IsCentral).Select(c => new
            {
                From = c.Id,
                To = 5,
                Distance = Math.Sqrt(Math.Pow(c.CenterX, 2) + Math.Pow(c.CenterZ, 2)),
                Type = "elemental_to_central"
            });

            return Ok(new { continents = result, connections, centerContinentId = 5, timestamp = DateTime.UtcNow });
        }

        // ========== ОПЫТ (ЗАЩИЩЁН JWT) ==========
        [Authorize]
        [HttpPost("gain-experience")]
        public async Task<IActionResult> GainExperience([FromBody] ExperienceRequest? request)
        {
            var playerId = GetPlayerIdFromToken();
            if (string.IsNullOrEmpty(playerId))
                return Unauthorized(new { error = "Токен не содержит playerId" });

            var player = await _context.Players.FindAsync(playerId);
            if (player == null) return NotFound(new { error = "Игрок не найден" });
            if (request == null || request.Amount <= 0) return BadRequest(new { error = "Опыт должен быть положительным" });

            player.Experience += request.Amount;
            int levels = 0;
            int expToNext = 100 + (player.Level - 1) * 50;
            while (player.Experience >= expToNext)
            {
                player.Experience -= expToNext;
                player.Level++;
                levels++;
                expToNext = 100 + (player.Level - 1) * 50;
            }
            player.LastLogin = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                playerId = player.Id,
                username = player.Username,
                newLevel = player.Level,
                levelsGained = levels,
                currentExperience = player.Experience,
                experienceToNextLevel = expToNext,
                canEnterAtlantis = player.Level >= 10
            });
        }

        [HttpGet("player/{id}/available-continents")]
        public async Task<IActionResult> GetAvailableContinents(string id)
        {
            var player = await _context.Players.FindAsync(id);
            if (player == null) return NotFound(new { error = "Игрок не найден" });

            var continents = await _context.Continents.ToListAsync();

            var available = continents
                .Where(c => c.IsStarter || player.Level >= c.LevelRequirement)
                .Select(c => new { c.Id, c.Name, c.Element, Players = $"{c.CurrentPlayers}/{c.MaxPlayers}", CanSpawn = true });

            var locked = continents
                .Where(c => !c.IsStarter && player.Level < c.LevelRequirement)
                .Select(c => new { c.Id, c.Name, RequiredLevel = c.LevelRequirement, CurrentLevel = player.Level });

            return Ok(new
            {
                playerId = player.Id,
                playerLevel = player.Level,
                available,
                locked,
                canAccessAtlantis = player.Level >= 10
            });
        }

        // ========== АТАКА МОБА (ЗАЩИЩЁН JWT) ==========
        [Authorize]
        [HttpPost("mobs/{mobId}/attack")]
        public async Task<IActionResult> AttackMob(string mobId, [FromBody] AttackMobRequest request)
        {
            var playerId = GetPlayerIdFromToken();
            if (string.IsNullOrEmpty(playerId))
                return Unauthorized(new { error = "Токен не содержит playerId" });

            var player = await _context.Players.FindAsync(playerId);
            if (player == null) return NotFound(new { error = "Игрок не найден" });

            var cities = await _context.Cities.ToListAsync();
            var city = cities.FirstOrDefault(c => c.MobCamps.Any(m => m.Id == mobId));
            if (city == null) return NotFound(new { error = "Моб не найден" });
            if (player.CityId != city.Id) return BadRequest(new { error = "Вы находитесь не в том городе" });

            var mob = city.MobCamps.First(m => m.Id == mobId);
            if (!mob.IsAlive) return BadRequest(new { error = "Моб уже повержен. Ожидайте респавна." });

            int damage = player.Level * 10;
            mob.Health -= damage;

            bool isKilled = mob.Health <= 0;

            if (isKilled)
            {
                int expReward = mob.RewardExp;
                player.Experience += expReward;

                int levelsGained = 0;
                int expToNext = 100 + (player.Level - 1) * 50;
                while (player.Experience >= expToNext)
                {
                    player.Experience -= expToNext;
                    player.Level++;
                    levelsGained++;
                    expToNext = 100 + (player.Level - 1) * 50;
                }

                int platinumReward = _rand.Next(mob.RewardMinPlatinum, mob.RewardMaxPlatinum + 1);
                player.Platinum += platinumReward;

                mob.IsAlive = false;
                mob.Health = 0;
                mob.RespawnTime = DateTime.UtcNow.AddMinutes(1);

                _context.Entry(city).Property(c => c.MobCamps).IsModified = true;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Моб побеждён!",
                    damage = damage,
                    isKilled = true,
                    reward = new
                    {
                        exp = expReward,
                        platinum = platinumReward,
                        newLevel = player.Level,
                        levelsGained = levelsGained
                    },
                    respawnAt = mob.RespawnTime
                });
            }
            else
            {
                _context.Entry(city).Property(c => c.MobCamps).IsModified = true;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Моб получил {damage} урона. Осталось здоровья: {mob.Health}",
                    damage = damage,
                    isKilled = false,
                    remainingHealth = mob.Health
                });
            }
        }

        // ========== ВСПОМОГАТЕЛЬНЫЙ МЕТОД ==========
        private async Task UpdateContinentCounters()
        {
            var continents = await _context.Continents.ToListAsync();
            foreach (var c in continents)
                c.CurrentPlayers = 0;

            var players = await _context.Players.ToListAsync();
            foreach (var p in players)
            {
                if (p.ContinentId.HasValue)
                {
                    var cont = continents.FirstOrDefault(c => c.Id == p.ContinentId.Value);
                    if (cont != null) cont.CurrentPlayers++;
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}