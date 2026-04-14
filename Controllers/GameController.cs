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
                        Settlements = new List<PlayerSettlement>(),
                        Mines = new List<Mine>(),
                        MobCamps = new List<MobCamp>(),
                        Dungeons = new List<Dungeon>()
                    };

                    // 10 шахт
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

                    // 20 мобов
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
                        
                        int health = 50 + ring.Level * 10;
                        city.MobCamps.Add(new MobCamp
                        {
                            MobType = _rand.Next(3) switch { 0 => "goblin", 1 => "wolf", _ => "skeleton" },
                            Level = _rand.Next(1, city.Level + 2),
                            LocalX = localX,
                            LocalZ = localZ,
                            IsAlive = true,
                            Health = health,
                            MaxHealth = health,
                            AttackPower = 10 + city.Level * 2,
                            RewardExp = 50 + city.Level * 10,
                            RewardMinPlatinum = 10,
                            RewardMaxPlatinum = 50
                        });
                    }

                    // 3 подземелья
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

        // ========== РЕСПАВН ШАХТЫ ==========
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
                return new { p.Id, p.Username, p.Level, p.Platinum, p.SpiritEnergy, Continent = continent?.Name ?? "Не выбран", SpawnPosition = p.ContinentId.HasValue ? new { p.SpawnX, p.SpawnZ } : null, p.CreatedAt };
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
                Experience = 0,
                Platinum = 0,
                SpiritEnergy = 0
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

            player.CityId = city.Id;
            player.SettlementLocalX = localX;
            player.SettlementLocalZ = localZ;
            player.ContinentId = continent.Id;

            float worldX = city.CenterX + (localX - 0.5f) * 20f;
            float worldZ = city.CenterZ + (localZ - 0.5f) * 20f;
            player.SpawnX = worldX;
            player.SpawnZ = worldZ;
            player.CurrentX = worldX;
            player.CurrentZ = worldZ;
            player.LastLogin = DateTime.UtcNow;

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

        // ========== API ДЛЯ СБОРА РЕСУРСОВ ==========
        [HttpPost("mines/{mineId}/gather")]
        public IActionResult GatherMine(string mineId, [FromBody] GatherMineRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PlayerId))
                return BadRequest(new { error = "PlayerId обязателен" });
            if (!_players.TryGetValue(request.PlayerId, out var player))
                return NotFound(new { error = "Игрок не найден" });

            var city = _cities.Values.FirstOrDefault(c => c.Mines.Any(m => m.Id == mineId));
            if (city == null)
                return NotFound(new { error = "Шахта не найдена" });
            if (player.CityId != city.Id)
                return BadRequest(new { error = "Вы находитесь не в том городе" });

            var mine = city.Mines.First(m => m.Id == mineId);
            if (mine.Amount <= 0)
                return BadRequest(new { error = "Шахта истощена" });

            int gathered = Math.Min(request.Amount ?? 100, mine.Amount);
            
            var continent = _continents.First(c => c.Id == player.ContinentId);
            if (continent.BonusType == "defense" && mine.Type == "platinum")
                gathered = (int)(gathered * 1.1f);
            else if (continent.BonusType == "speed" && mine.Type == "spirit")
                gathered = (int)(gathered * 1.1f);

            mine.Amount -= gathered;

            if (mine.Type == "platinum")
                player.Platinum += gathered;
            else
                player.SpiritEnergy += gathered;

            if (mine.Amount <= 0)
            {
                RespawnMine(city, mine);
            }

            return Ok(new
            {
                success = true,
                gathered = gathered,
                resourceType = mine.Type,
                remaining = mine.Amount,
                playerResources = new { player.Platinum, player.SpiritEnergy }
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
            
            // Проверка респавна мобов
            foreach (var mob in city.MobCamps)
            {
                if (!mob.IsAlive && mob.RespawnTime.HasValue && DateTime.UtcNow >= mob.RespawnTime.Value)
                {
                    mob.IsAlive = true;
                    mob.Health = mob.MaxHealth;
                    mob.RespawnTime = null;
                }
            }
            
            return Ok(new
            {
                city.Id,
                city.Level,
                city.CenterX,
                city.CenterZ,
                city.CastleOwnerGuildId,
                Settlements = city.Settlements.Select(s => new { s.PlayerId, s.LocalX, s.LocalZ, s.Level }),
                Mines = city.Mines.Select(m => new { m.Id, m.Type, m.LocalX, m.LocalZ, m.Amount, m.MaxAmount }),
                MobCamps = city.MobCamps.Select(m => new { m.Id, m.MobType, m.Level, m.LocalX, m.LocalZ, m.IsAlive, m.Health, m.MaxHealth, m.RespawnTime }),
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

        // ========== API ДЛЯ АТАКИ МОБА ==========
        [HttpPost("mobs/{mobId}/attack")]
        public IActionResult AttackMob(string mobId, [FromBody] AttackMobRequest request)
        {
            // 1. Проверить игрока
            if (!_players.TryGetValue(request.PlayerId, out var player))
                return NotFound(new { error = "Игрок не найден" });

            // 2. Найти город, в котором находится моб
            var city = _cities.Values.FirstOrDefault(c => c.MobCamps.Any(m => m.Id == mobId));
            if (city == null)
                return NotFound(new { error = "Моб не найден" });
            
            // 3. Проверить, что игрок находится в этом городе
            if (player.CityId != city.Id)
                return BadRequest(new { error = "Вы находитесь не в том городе" });
            
            // 4. Найти моба
            var mob = city.MobCamps.First(m => m.Id == mobId);
            
            // 5. Проверить, что моб жив
            if (!mob.IsAlive)
                return BadRequest(new { error = "Моб уже повержен. Ожидайте респавна." });
            
            // 6. Рассчитать урон
            int damage = player.Level * 10;
            mob.Health -= damage;
            
            // 7. Проверить, убит ли моб
            bool isKilled = mob.Health <= 0;

            if (isKilled)
            {
                // Начисление опыта
                int expReward = mob.RewardExp;
                player.Experience += expReward;

                // Проверка повышения уровня
                int levelsGained = 0;
                int expToNext = 100 + (player.Level - 1) * 50;
                while (player.Experience >= expToNext)
                {
                    player.Experience -= expToNext;
                    player.Level++;
                    levelsGained++;
                    expToNext = 100 + (player.Level - 1) * 50;
                }

                // Начисление ресурсов
                int platinumReward = _rand.Next(mob.RewardMinPlatinum, mob.RewardMaxPlatinum + 1);
                player.Platinum += platinumReward;

                // Пометить моба мёртвым
                mob.IsAlive = false;
                mob.Health = 0;
                mob.RespawnTime = DateTime.UtcNow.AddMinutes(1);

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