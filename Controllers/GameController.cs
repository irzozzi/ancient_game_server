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
        // ========== ХРАНИЛИЩА ==========
        private static readonly ConcurrentDictionary<string, Player> _players = new();
        private static readonly List<Continent> _continents = new()
        {
            new Continent { Id = 1, Name = "Макошь", Element = "earth", Description = "Континент земли и стабильности", CenterX = 200, CenterZ = 200, MaxPlayers = 1000, CurrentPlayers = 0, IsAvailable = true, IsStarter = true, LevelRequirement = 1, BonusType = "defense", IsCentral = false },
            new Continent { Id = 2, Name = "Стрибог", Element = "wind", Description = "Континент ветра и скорости", CenterX = -200, CenterZ = 200, MaxPlayers = 1000, CurrentPlayers = 0, IsAvailable = true, IsStarter = true, LevelRequirement = 1, BonusType = "speed", IsCentral = false },
            new Continent { Id = 3, Name = "Семаргл", Element = "fire", Description = "Континент огня и силы", CenterX = -200, CenterZ = -200, MaxPlayers = 1000, CurrentPlayers = 0, IsAvailable = true, IsStarter = true, LevelRequirement = 1, BonusType = "attack", IsCentral = false },
            new Continent { Id = 4, Name = "Давана", Element = "water", Description = "Континент воды и мудрости", CenterX = 200, CenterZ = -200, MaxPlayers = 1000, CurrentPlayers = 0, IsAvailable = true, IsStarter = true, LevelRequirement = 1, BonusType = "wisdom", IsCentral = false },
            new Continent { Id = 5, Name = "Атлантида", Element = "void", Description = "Затерянный центральный континент", CenterX = 0, CenterZ = 0, MaxPlayers = 4000, CurrentPlayers = 0, IsAvailable = true, IsStarter = false, LevelRequirement = 15, BonusType = "all", IsCentral = true }
        };

        // Хранилище городов
        private static readonly Dictionary<int, City> _cities = new();
        private static int _nextCityId = 1;

        // Статический конструктор – генерация городов для континентов 1-4
        static GameController()
        {
            GenerateCitiesForContinent(1);
            GenerateCitiesForContinent(2);
            GenerateCitiesForContinent(3);
            GenerateCitiesForContinent(4);
        }

        // ========== ГЕНЕРАЦИЯ ГОРОДОВ ==========
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

            var rand = new Random();
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
                        CenterZ = z
                    };

                    // Генерация шахт (10)
                    for (int m = 0; m < 10; m++)
                    {
                        float localX, localZ;
                        do
                        {
                            localX = (float)rand.NextDouble();
                            localZ = (float)rand.NextDouble();
                        } while (IsTooCloseToCenter(localX, localZ, 0.15f));
                        city.Mines.Add(new Mine
                        {
                            Type = rand.Next(2) == 0 ? "platinum" : "spirit",
                            LocalX = localX,
                            LocalZ = localZ,
                            Amount = rand.Next(800, 1200),
                            MaxAmount = 1200
                        });
                    }

                    // Генерация мобов (20)
                    for (int m = 0; m < 20; m++)
                    {
                        float localX, localZ;
                        do
                        {
                            localX = (float)rand.NextDouble();
                            localZ = (float)rand.NextDouble();
                        } while (IsTooCloseToCenter(localX, localZ, 0.1f));
                        city.MobCamps.Add(new MobCamp
                        {
                            MobType = rand.Next(3) switch { 0 => "goblin", 1 => "wolf", _ => "skeleton" },
                            Level = rand.Next(1, city.Level + 2),
                            LocalX = localX,
                            LocalZ = localZ,
                            IsAlive = true
                        });
                    }

                    // Генерация подземелий (3)
                    for (int m = 0; m < 3; m++)
                    {
                        float localX, localZ;
                        do
                        {
                            localX = (float)rand.NextDouble();
                            localZ = (float)rand.NextDouble();
                        } while (IsTooCloseToCenter(localX, localZ, 0.2f));
                        city.Dungeons.Add(new Dungeon
                        {
                            DungeonType = rand.Next(2) == 0 ? "ancient_temple" : "cursed_crypt",
                            LocalX = localX,
                            LocalZ = localZ,
                            LastCompletedTime = DateTime.UtcNow.AddHours(-rand.Next(0, 24))
                        });
                    }

                    _cities[city.Id] = city;
                }
            }
        }

        private static bool IsTooCloseToCenter(float x, float z, float minDistance)
        {
            float dx = x - 0.5f;
            float dz = z - 0.5f;
            return Math.Sqrt(dx * dx + dz * dz) < minDistance;
        }

        // ========== СИСТЕМНЫЕ МЕТОДЫ ==========
        [HttpGet("ping")]
        public IActionResult Ping() => Ok(new { message = "pong", serverTime = DateTime.UtcNow, version = "1.0.0" });

        [HttpGet("test")]
        public IActionResult Test() => Ok(new
        {
            Message = "Сервер игры 'Древные' работает!",
            Status = "online",
            Time = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            PlayersCount = _players.Count
        });

        // ========== КОНТИНЕНТЫ ==========
        [HttpGet("continents")]
        public IActionResult GetContinents([FromQuery] string? playerId = null)
        {
            Player? player = null;
            if (!string.IsNullOrEmpty(playerId))
                _players.TryGetValue(playerId, out player);

            var continentsInfo = _continents.Select(c =>
            {
                var canSpawn = c.IsStarter || (player != null && player.Level >= c.LevelRequirement);
                return new
                {
                    c.Id,
                    c.Name,
                    c.Element,
                    c.Description,
                    Players = $"{c.CurrentPlayers}/{c.MaxPlayers}",
                    c.LevelRequirement,
                    CanSpawn = canSpawn,
                    IsLocked = !canSpawn
                };
            });
            return Ok(continentsInfo);
        }

        // ========== ИГРОКИ ==========
        [HttpGet("players")]
        public IActionResult GetPlayers()
        {
            var playersList = _players.Values.Select(p =>
            {
                var continent = p.ContinentId.HasValue ? _continents.FirstOrDefault(c => c.Id == p.ContinentId) : null;
                return new
                {
                    p.Id,
                    p.Username,
                    p.Level,
                    Continent = continent?.Name ?? "Не выбран",
                    SpawnPosition = p.ContinentId.HasValue ? new { p.SpawnX, p.SpawnZ } : null,
                    p.CreatedAt
                };
            });
            return Ok(playersList);
        }

        [HttpGet("player/{id}")]
        public IActionResult GetPlayer(string id)
        {
            if (!_players.TryGetValue(id, out var player))
                return NotFound(new { error = "Игрок не найден" });

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
            if (request == null || string.IsNullOrWhiteSpace(request.Username))
                return BadRequest(new { error = "Имя игрока обязательно" });
            if (request.Username.Length < 3 || request.Username.Length > 20)
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
                Experience = 0,
                CurrentX = 0,
                CurrentZ = 0
            };
            if (_players.TryAdd(player.Id, player))
                return Ok(new { success = true, playerId = player.Id, username = player.Username, message = "Игрок зарегистрирован" });
            return StatusCode(500, new { error = "Ошибка регистрации" });
        }

        // ========== СПАВН ИГРОКА В ГОРОДЕ ==========
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

            // Поиск города со свободными поместьями (меньше 40)
            var city = _cities.Values.FirstOrDefault(c => c.ContinentId == continent.Id && c.Settlements.Count < 40);
            if (city == null)
                return BadRequest(new { error = "Нет свободных городов на континенте" });

            // Вычисляем локальные координаты нового поместья (равномерно по окружности)
            int index = city.Settlements.Count;
            float radius = 0.6f;
            float angle = index * (2 * MathF.PI / 40);
            float localX = 0.5f + radius * MathF.Cos(angle);
            float localZ = 0.5f + radius * MathF.Sin(angle);

            var settlement = new PlayerSettlement
            {
                PlayerId = player.Id,
                LocalX = localX,
                LocalZ = localZ,
                Level = 1
            };
            city.Settlements.Add(settlement);

            // Сохраняем информацию об игроке
            player.CityId = city.Id;
            player.SettlementLocalX = localX;
            player.SettlementLocalZ = localZ;
            player.ContinentId = continent.Id;

            // Мировые координаты спавна (центр города + смещение)
            float worldX = city.CenterX + (localX - 0.5f) * 20f;
            float worldZ = city.CenterZ + (localZ - 0.5f) * 20f;
            player.SpawnX = worldX;
            player.SpawnZ = worldZ;
            player.CurrentX = worldX;
            player.CurrentZ = worldZ;
            player.LastLogin = DateTime.UtcNow;

            // Обновляем счётчик континента
            if (player.ContinentId.HasValue)
            {
                var oldContinent = _continents.FirstOrDefault(c => c.Id == player.ContinentId.Value);
                if (oldContinent != null && oldContinent.CurrentPlayers > 0)
                    oldContinent.CurrentPlayers--;
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

        // ========== API ДЛЯ РАБОТЫ С ГОРОДАМИ ==========
        [HttpGet("cities")]
        public IActionResult GetCities(int continentId, float? minX = null, float? maxX = null, float? minZ = null, float? maxZ = null)
        {
            var cities = _cities.Values.Where(c => c.ContinentId == continentId);
            if (minX.HasValue && maxX.HasValue && minZ.HasValue && maxZ.HasValue)
                cities = cities.Where(c => c.CenterX >= minX && c.CenterX <= maxX && c.CenterZ >= minZ && c.CenterZ <= maxZ);
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
        public IActionResult GetCity(int cityId)
        {
            if (!_cities.TryGetValue(cityId, out var city))
                return NotFound("City not found");
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

        // ========== СТАТИСТИКА ==========
        [HttpGet("world-stats")]
        public IActionResult GetWorldStats()
        {
            UpdateContinentCounters();
            var totalPlayers = _players.Count;
            var playersOnCentral = _players.Values.Count(p => p.ContinentId.HasValue && _continents.Any(c => c.Id == p.ContinentId.Value && c.IsCentral));
            return Ok(new
            {
                totalPlayers,
                playersOnCentral,
                playersOnElemental = totalPlayers - playersOnCentral,
                continents = _continents.Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Element,
                    players = c.CurrentPlayers,
                    maxPlayers = c.MaxPlayers,
                    status = c.CurrentPlayers >= c.MaxPlayers ? "full" : "available"
                }),
                timestamp = DateTime.UtcNow
            });
        }

        // ========== ПРОЧИЕ API (world-map, gain-experience, available-continents) ==========
        [HttpGet("world-map")]
        public IActionResult GetWorldMap()
        {
            var continentPositions = _continents.Select(c => new
            {
                c.Id,
                c.Name,
                c.Element,
                Position = new { c.CenterX, c.CenterZ },
                Radius = c.IsCentral ? 150 : 100,
                Color = GetContinentColor(c.Element),
                c.IsStarter,
                c.IsCentral,
                PlayerCount = c.CurrentPlayers
            });
            var connections = _continents.Where(c => !c.IsCentral).Select(c => new
            {
                From = c.Id,
                To = 5,
                Distance = CalculateDistance(c.CenterX, c.CenterZ, 0, 0),
                Type = "elemental_to_central"
            });
            return Ok(new { continents = continentPositions, connections, centerContinentId = 5, timestamp = DateTime.UtcNow });
        }

        [HttpPost("gain-experience")]
        public IActionResult GainExperience([FromBody] ExperienceRequest? request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PlayerId)) return BadRequest(new { error = "Неверный запрос" });
            if (!_players.TryGetValue(request.PlayerId, out var player)) return NotFound(new { error = "Игрок не найден" });
            if (request.Amount <= 0) return BadRequest(new { error = "Опыт должен быть положительным" });

            player.Experience += request.Amount;
            int levelsGained = 0;
            int expToNext = CalculateExpToNextLevel(player.Level);
            while (player.Experience >= expToNext)
            {
                player.Experience -= expToNext;
                player.Level++;
                levelsGained++;
                expToNext = CalculateExpToNextLevel(player.Level);
            }
            player.LastLogin = DateTime.UtcNow;
            return Ok(new
            {
                success = true,
                playerId = player.Id,
                username = player.Username,
                newLevel = player.Level,
                levelsGained,
                currentExperience = player.Experience,
                experienceToNextLevel = CalculateExpToNextLevel(player.Level),
                canEnterAtlantis = player.Level >= 10
            });
        }

        [HttpGet("player/{id}/available-continents")]
        public IActionResult GetAvailableContinents(string id)
        {
            if (!_players.TryGetValue(id, out var player)) return NotFound(new { error = "Игрок не найден" });
            var available = _continents.Where(c => c.IsStarter || player.Level >= c.LevelRequirement)
                .Select(c => new { c.Id, c.Name, c.Element, Players = $"{c.CurrentPlayers}/{c.MaxPlayers}", CanSpawn = true });
            var locked = _continents.Where(c => !c.IsStarter && player.Level < c.LevelRequirement)
                .Select(c => new { c.Id, c.Name, RequiredLevel = c.LevelRequirement, CurrentLevel = player.Level });
            return Ok(new { playerId = player.Id, playerLevel = player.Level, available, locked, canAccessAtlantis = player.Level >= 10 });
        }

        // ========== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ==========
        private void UpdateContinentCounters()
        {
            foreach (var continent in _continents) continent.CurrentPlayers = 0;
            foreach (var player in _players.Values)
                if (player.ContinentId.HasValue)
                {
                    var continent = _continents.FirstOrDefault(c => c.Id == player.ContinentId.Value);
                    if (continent != null) continent.CurrentPlayers++;
                }
        }

        private static string GetContinentColor(string element) => element switch
        {
            "earth" => "#8B4513",
            "wind" => "#87CEEB",
            "fire" => "#FF4500",
            "water" => "#1E90FF",
            "void" => "#9370DB",
            _ => "#808080"
        };

        private static double CalculateDistance(float x1, float z1, float x2, float z2) => Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(z2 - z1, 2));
        private static int CalculateExpToNextLevel(int level) => 100 + (level - 1) * 50;
    }
}