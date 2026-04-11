using Microsoft.AspNetCore.Mvc;
using AncientServer.Models;
using AncientServer.Models.DTOs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace AncientServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameController : ControllerBase
    {
        private static readonly ConcurrentDictionary<string, Player> _players = new();
        private static readonly List<Continent> _continents = new()
        {
            new Continent { Id = 1, Name = "Макошь", Element = "earth", Description = "Континент земли", CenterX = 200, CenterZ = 200, MaxPlayers = 1000, CurrentPlayers = 0, IsAvailable = true, IsStarter = true, LevelRequirement = 1, BonusType = "defense", IsCentral = false },
            new Continent { Id = 2, Name = "Стрибог", Element = "wind", Description = "Континент ветра", CenterX = -200, CenterZ = 200, MaxPlayers = 1000, CurrentPlayers = 0, IsAvailable = true, IsStarter = true, LevelRequirement = 1, BonusType = "speed", IsCentral = false },
            new Continent { Id = 3, Name = "Семаргл", Element = "fire", Description = "Континент огня", CenterX = -200, CenterZ = -200, MaxPlayers = 1000, CurrentPlayers = 0, IsAvailable = true, IsStarter = true, LevelRequirement = 1, BonusType = "attack", IsCentral = false },
            new Continent { Id = 4, Name = "Давана", Element = "water", Description = "Континент воды", CenterX = 200, CenterZ = -200, MaxPlayers = 1000, CurrentPlayers = 0, IsAvailable = true, IsStarter = true, LevelRequirement = 1, BonusType = "wisdom", IsCentral = false },
            new Continent { Id = 5, Name = "Атлантида", Element = "void", Description = "Центральный континент", CenterX = 0, CenterZ = 0, MaxPlayers = 4000, CurrentPlayers = 0, IsAvailable = true, IsStarter = false, LevelRequirement = 15, BonusType = "all", IsCentral = true }
        };

        private static readonly Dictionary<int, City> _cities = new();
        private static int _nextCityId = 1;
        private static readonly Random _rand = new Random();

        // Константы для проверки наложений
        private const float CENTER_RADIUS = 0.15f;
        private const float MIN_DISTANCE = 0.1f;

        static GameController()
        {
            GenerateCitiesForContinent(1);
            GenerateCitiesForContinent(2);
            GenerateCitiesForContinent(3);
            GenerateCitiesForContinent(4);
        }

        private static void GenerateCitiesForContinent(int continentId)
        {
            var rings = new[]
            {
                new { Count = 36, Radius = 100f, Level = 1 },
                new { Count = 30, Radius = 80f, Level = 2 },
                new { Count = 24, Radius = 60f, Level = 3 },
                new { Count = 18, Radius = 40f, Level = 4 },
                new { Count = 12, Radius = 20f, Level = 5 }
            };

            foreach (var ring in rings)
            {
                float angleStep = 2 * MathF.PI / ring.Count;
                for (int i = 0; i < ring.Count; i++)
                {
                    float angle = i * angleStep;
                    float x = ring.Radius * MathF.Cos(angle);
                    float z = ring.Radius * MathF.Sin(angle);

                    var city = new City
                    {
                        Id = _nextCityId++,
                        ContinentId = continentId,
                        Level = ring.Level,
                        CenterX = x,
                        CenterZ = z,
                        Settlements = new List<PlayerSettlement>(), // пустой список – поместья будут добавляться при спавне
                        Mines = new List<Mine>(),
                        MobCamps = new List<MobCamp>(),
                        Dungeons = new List<Dungeon>()
                    };

                    // 10 шахт (с проверкой наложения только между собой, так как поместий ещё нет)
                    for (int m = 0; m < 10; m++)
                    {
                        float localX, localZ;
                        int attempts = 0;
                        do
                        {
                            localX = (float)_rand.NextDouble();
                            localZ = (float)_rand.NextDouble();
                            attempts++;
                            if (attempts > 500) break;
                        } while (IsTooCloseToCenter(localX, localZ, CENTER_RADIUS) ||
                                 IsOverlappingWithMines(localX, localZ, city.Mines, MIN_DISTANCE));
                        
                        city.Mines.Add(new Mine
                        {
                            Type = _rand.Next(2) == 0 ? "platinum" : "spirit",
                            LocalX = localX,
                            LocalZ = localZ,
                            Amount = _rand.Next(800, 1200),
                            MaxAmount = 1200
                        });
                    }

                    // 20 мобов (с проверкой наложения на шахты)
                    for (int m = 0; m < 20; m++)
                    {
                        float localX, localZ;
                        int attempts = 0;
                        do
                        {
                            localX = (float)_rand.NextDouble();
                            localZ = (float)_rand.NextDouble();
                            attempts++;
                            if (attempts > 500) break;
                        } while (IsTooCloseToCenter(localX, localZ, CENTER_RADIUS) ||
                                 IsOverlappingWithMines(localX, localZ, city.Mines, MIN_DISTANCE) ||
                                 IsOverlappingWithMobs(localX, localZ, city.MobCamps, MIN_DISTANCE));
                        
                        city.MobCamps.Add(new MobCamp
                        {
                            MobType = _rand.Next(3) switch { 0 => "goblin", 1 => "wolf", _ => "skeleton" },
                            Level = _rand.Next(1, city.Level + 2),
                            LocalX = localX,
                            LocalZ = localZ,
                            IsAlive = true
                        });
                    }

                    // 3 подземелья (с проверкой наложения на шахты и мобов)
                    for (int m = 0; m < 3; m++)
                    {
                        float localX, localZ;
                        int attempts = 0;
                        do
                        {
                            localX = (float)_rand.NextDouble();
                            localZ = (float)_rand.NextDouble();
                            attempts++;
                            if (attempts > 500) break;
                        } while (IsTooCloseToCenter(localX, localZ, CENTER_RADIUS) ||
                                 IsOverlappingWithMines(localX, localZ, city.Mines, MIN_DISTANCE) ||
                                 IsOverlappingWithMobs(localX, localZ, city.MobCamps, MIN_DISTANCE) ||
                                 IsOverlappingWithDungeons(localX, localZ, city.Dungeons, MIN_DISTANCE));
                        
                        city.Dungeons.Add(new Dungeon
                        {
                            DungeonType = _rand.Next(2) == 0 ? "ancient_temple" : "cursed_crypt",
                            LocalX = localX,
                            LocalZ = localZ,
                            LastCompletedTime = DateTime.UtcNow.AddHours(-_rand.Next(0, 24))
                        });
                    }

                    _cities[city.Id] = city;
                }
            }
        }

        // ========== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ДЛЯ ПРОВЕРКИ НАЛОЖЕНИЙ ==========
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

        // ========== API МЕТОДЫ ==========
        [HttpGet("ping")]
        public IActionResult Ping() => Ok(new { message = "pong", serverTime = DateTime.UtcNow });

        [HttpGet("test")]
        public IActionResult Test() => Ok(new { Message = "Сервер работает", Status = "online", Time = DateTime.UtcNow, PlayersCount = _players.Count });

        [HttpGet("continents")]
        public IActionResult GetContinents([FromQuery] string? playerId = null)
        {
            Player? player = null;
            if (!string.IsNullOrEmpty(playerId)) _players.TryGetValue(playerId, out player);
            var result = _continents.Select(c => new
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

        [HttpGet("players")]
        public IActionResult GetPlayers()
        {
            var list = _players.Values.Select(p =>
            {
                var continent = p.ContinentId.HasValue ? _continents.FirstOrDefault(c => c.Id == p.ContinentId) : null;
                return new { p.Id, p.Username, p.Level, Continent = continent?.Name ?? "Не выбран", SpawnPosition = p.ContinentId.HasValue ? new { p.SpawnX, p.SpawnZ } : null, p.CreatedAt };
            });
            return Ok(list);
        }

        [HttpGet("player/{id}")]
        public IActionResult GetPlayer(string id)
        {
            if (!_players.TryGetValue(id, out var player)) return NotFound(new { error = "Игрок не найден" });
            var continent = player.ContinentId.HasValue ? _continents.FirstOrDefault(c => c.Id == player.ContinentId) : null;
            return Ok(new
            {
                player.Id,
                player.Username,
                player.Level,
                player.Experience,
                Continent = continent?.Name ?? "Не выбран",
                Element = continent?.Element,
                SpawnPosition = player.ContinentId.HasValue ? new { player.SpawnX, player.SpawnZ } : null,
                player.CreatedAt,
                player.LastLogin,
                CityId = player.CityId != -1 ? player.CityId : (int?)null,
                SettlementLocal = player.CityId != -1 ? new { player.SettlementLocalX, player.SettlementLocalZ } : null
            });
        }

        [HttpPost("register")]
        public IActionResult RegisterPlayer([FromBody] RegisterRequest? request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || request.Username.Length < 3 || request.Username.Length > 20)
                return BadRequest(new { error = "Имя должно быть 3-20 символов" });
            if (_players.Values.Any(p => p.Username.Equals(request.Username, StringComparison.OrdinalIgnoreCase)))
                return BadRequest(new { error = "Имя уже занято" });

            var player = new Player
            {
                Username = request.Username.Trim(),
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow,
                IsActive = true,
                Level = 1,
                Experience = 0
            };
            if (_players.TryAdd(player.Id, player))
                return Ok(new { success = true, playerId = player.Id, username = player.Username, message = "Зарегистрирован" });
            return StatusCode(500, new { error = "Ошибка регистрации" });
        }

        [HttpPost("spawn")]
        public IActionResult SpawnOnContinent([FromBody] SpawnRequest? request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PlayerId)) 
                return BadRequest(new { error = "PlayerId обязателен" });
            if (!_players.TryGetValue(request.PlayerId, out var player)) 
                return NotFound(new { error = "Игрок не найден" });
            if (player.CityId != -1) 
                return BadRequest(new { error = "Игрок уже имеет поселение" });

            var continent = _continents.FirstOrDefault(c => c.Id == request.ContinentId);
            if (continent == null) return NotFound(new { error = "Континент не найден" });
            if (!continent.IsStarter && player.Level < continent.LevelRequirement) 
                return BadRequest(new { error = $"Требуется уровень {continent.LevelRequirement}" });
            if (continent.CurrentPlayers >= continent.MaxPlayers) 
                return BadRequest(new { error = "Континент переполнен" });

            // Проверка cityId
            if (request.CityId <= 0)
                return BadRequest(new { error = "CityId обязателен" });
            if (!_cities.TryGetValue(request.CityId, out var city))
                return BadRequest(new { error = "Город не найден" });
            if (city.ContinentId != continent.Id)
                return BadRequest(new { error = "Город не принадлежит выбранному континенту" });
            if (city.Level != 1)
                return BadRequest(new { error = "Вы можете заселяться только в город первого уровня (внешний круг)"});
            if (city.Settlements.Count >= 40)
                return BadRequest(new { error = "В этом городе нет свободных мест" });

            // Генерация координат нового поместья
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

            if (!found)
                return BadRequest(new { error = "Не удалось найти свободное место в городе" });

            var settlement = new PlayerSettlement
            {
                PlayerId = player.Id,
                LocalX = localX,
                LocalZ = localZ,
                Level = 1
            };
            city.Settlements.Add(settlement);

            // Обновление игрока
            player.CityId = city.Id;
            player.SettlementLocalX = localX;
            player.SettlementLocalZ = localZ;
            player.ContinentId = continent.Id;

            // Мировые координаты
            float worldX = city.CenterX + (localX - 0.5f) * 20f;
            float worldZ = city.CenterZ + (localZ - 0.5f) * 20f;
            player.SpawnX = worldX;
            player.SpawnZ = worldZ;
            player.CurrentX = worldX;
            player.CurrentZ = worldZ;
            player.LastLogin = DateTime.UtcNow;

            // Обновление счётчиков континента
            if (player.ContinentId.HasValue)
            {
                var old = _continents.FirstOrDefault(c => c.Id == player.ContinentId.Value);
                if (old != null && old.CurrentPlayers > 0) old.CurrentPlayers--;
            }
            continent.CurrentPlayers++;

            return Ok(new
            {
                success = true,
                message = $"Добро пожаловать в город {city.Id} на {continent.Name}!",
                player = new
                {
                    player.Id,
                    player.Username,
                    player.Level,
                    Continent = continent.Name,
                    CityId = city.Id,
                    CityLevel = city.Level,
                    SettlementLocal = new { localX, localZ },
                    SpawnPosition = new { worldX, worldZ }
                }
            });
        }

        [HttpGet("cities")]
        public IActionResult GetCities(int continentId, float? minX = null, float? maxX = null, float? minZ = null, float? maxZ = null)
        {
            var cities = _cities.Values.Where(c => c.ContinentId == continentId);
            if (minX.HasValue && maxX.HasValue && minZ.HasValue && maxZ.HasValue)
                cities = cities.Where(c => c.CenterX >= minX && c.CenterX <= maxX && c.CenterZ >= minZ && c.CenterZ <= maxZ);
            var result = cities.Select(c => new { c.Id, c.Level, c.CenterX, c.CenterZ, SettlementsCount = c.Settlements.Count, c.CastleOwnerGuildId });
            return Ok(result);
        }

        [HttpGet("cities/{cityId}")]
        public IActionResult GetCity(int cityId)
        {
            if (!_cities.TryGetValue(cityId, out var city)) return NotFound("City not found");
            return Ok(new
            {
                city.Id,
                city.Level,
                city.CenterX,
                city.CenterZ,
                city.CastleOwnerGuildId,
                Settlements = city.Settlements.Select(s => new { s.PlayerId, s.LocalX, s.LocalZ, s.Level }),
                Mines = city.Mines.Select(m => new { m.Id, m.Type, m.LocalX, m.LocalZ, m.Amount, m.MaxAmount }),
                MobCamps = city.MobCamps.Select(m => new { m.Id, m.MobType, m.Level, m.LocalX, m.LocalZ, m.IsAlive }),
                Dungeons = city.Dungeons.Select(d => new { d.Id, d.DungeonType, d.LocalX, d.LocalZ, d.IsAvailable })
            });
        }

        [HttpGet("world-stats")]
        public IActionResult GetWorldStats()
        {
            UpdateContinentCounters();
            return Ok(new
            {
                totalPlayers = _players.Count,
                playersOnCentral = _players.Values.Count(p => p.ContinentId.HasValue && _continents.Any(c => c.Id == p.ContinentId.Value && c.IsCentral)),
                continents = _continents.Select(c => new { c.Id, c.Name, c.Element, players = c.CurrentPlayers, maxPlayers = c.MaxPlayers, status = c.CurrentPlayers >= c.MaxPlayers ? "full" : "available" })
            });
        }

        [HttpGet("world-map")]
        public IActionResult GetWorldMap()
        {
            var continents = _continents.Select(c => new
            {
                c.Id,
                c.Name,
                c.Element,
                Position = new { c.CenterX, c.CenterZ },
                Radius = c.IsCentral ? 150 : 100,
                Color = c.Element switch { "earth" => "#8B4513", "wind" => "#87CEEB", "fire" => "#FF4500", "water" => "#1E90FF", "void" => "#9370DB", _ => "#808080" },
                c.IsStarter,
                c.IsCentral,
                PlayerCount = c.CurrentPlayers
            });
            var connections = _continents.Where(c => !c.IsCentral).Select(c => new { From = c.Id, To = 5, Distance = Math.Sqrt(Math.Pow(c.CenterX, 2) + Math.Pow(c.CenterZ, 2)), Type = "elemental_to_central" });
            return Ok(new { continents, connections, centerContinentId = 5, timestamp = DateTime.UtcNow });
        }

        [HttpPost("gain-experience")]
        public IActionResult GainExperience([FromBody] ExperienceRequest? request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PlayerId)) return BadRequest(new { error = "Неверный запрос" });
            if (!_players.TryGetValue(request.PlayerId, out var player)) return NotFound(new { error = "Игрок не найден" });
            if (request.Amount <= 0) return BadRequest(new { error = "Опыт должен быть положительным" });

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
            return Ok(new { success = true, playerId = player.Id, username = player.Username, newLevel = player.Level, levelsGained = levels, currentExperience = player.Experience, experienceToNextLevel = expToNext, canEnterAtlantis = player.Level >= 10 });
        }

        [HttpGet("player/{id}/available-continents")]
        public IActionResult GetAvailableContinents(string id)
        {
            if (!_players.TryGetValue(id, out var player)) return NotFound(new { error = "Игрок не найден" });
            var available = _continents.Where(c => c.IsStarter || player.Level >= c.LevelRequirement).Select(c => new { c.Id, c.Name, c.Element, Players = $"{c.CurrentPlayers}/{c.MaxPlayers}", CanSpawn = true });
            var locked = _continents.Where(c => !c.IsStarter && player.Level < c.LevelRequirement).Select(c => new { c.Id, c.Name, RequiredLevel = c.LevelRequirement, CurrentLevel = player.Level });
            return Ok(new { playerId = player.Id, playerLevel = player.Level, available, locked, canAccessAtlantis = player.Level >= 10 });
        }

        private void UpdateContinentCounters()
        {
            foreach (var c in _continents) c.CurrentPlayers = 0;
            foreach (var p in _players.Values)
                if (p.ContinentId.HasValue)
                {
                    var cont = _continents.FirstOrDefault(c => c.Id == p.ContinentId.Value);
                    if (cont != null) cont.CurrentPlayers++;
                }
        }
    }
}