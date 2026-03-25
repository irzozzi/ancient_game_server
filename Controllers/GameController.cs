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
        // Временные хранилища в памяти
        private static readonly ConcurrentDictionary<string, Player> _players = new();
        private static readonly List<Continent> _continents = new()
        {
            // Стихийные континенты (стартовые)
            new Continent
            {
                Id = 1,
                Name = "Макошь",
                Element = "earth",
                Description = "Континент земли и стабильности",
                CenterX = 200,
                CenterZ = 200,
                MaxPlayers = 1000,
                CurrentPlayers = 0,
                IsAvailable = true,
                IsStarter = true,
                LevelRequirement = 1,
                BonusType = "defense",
                IsCentral = false
            },
            new Continent
            {
                Id = 2,
                Name = "Стрибог",
                Element = "wind",
                Description = "Континент ветра и скорости",
                CenterX = -200,
                CenterZ = 200,
                MaxPlayers = 1000,
                CurrentPlayers = 0,
                IsAvailable = true,
                IsStarter = true,
                LevelRequirement = 1,
                BonusType = "speed",
                IsCentral = false
            },
            new Continent
            {
                Id = 3,
                Name = "Семаргл",
                Element = "fire",
                Description = "Континент огня и силы",
                CenterX = -200,
                CenterZ = -200,
                MaxPlayers = 1000,
                CurrentPlayers = 0,
                IsAvailable = true,
                IsStarter = true,
                LevelRequirement = 1,
                BonusType = "attack",
                IsCentral = false
            },
            new Continent
            {
                Id = 4,
                Name = "Давана",
                Element = "water",
                Description = "Континент воды и мудрости",
                CenterX = 200,
                CenterZ = -200,
                MaxPlayers = 1000,
                CurrentPlayers = 0,
                IsAvailable = true,
                IsStarter = true,
                LevelRequirement = 1,
                BonusType = "wisdom",
                IsCentral = false
            },
            
            // Центральный континент (Атлантида)
            new Continent
            {
                Id = 5,
                Name = "Атлантида",
                Element = "void",
                Description = "Затерянный центральный континент",
                CenterX = 0,
                CenterZ = 0,
                MaxPlayers = 4000,
                CurrentPlayers = 0,
                IsAvailable = true,
                IsStarter = false,
                LevelRequirement = 15,
                BonusType = "all",
                IsCentral = true
            }
        };

        //  СИСТЕМНЫЕ МЕТОДЫ 

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new { message = "pong", serverTime = DateTime.UtcNow, version = "1.0.0" });
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new
            {
                Message = "Сервер игры 'Древние' работает!",
                Status = "online",
                Time = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                PlayersCount = _players.Count
            });
        }

        //  МЕТОДЫ КОНТИНЕНТОВ 

        [HttpGet("continents")]
        public IActionResult GetContinents([FromQuery] string? playerId = null)
        {
            Player? player = null;
            if (!string.IsNullOrEmpty(playerId))
            {
                _players.TryGetValue(playerId, out player);
            }

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

        //  МЕТОДЫ ИГРОКОВ 

        [HttpGet("players")]
        public IActionResult GetPlayers()
        {
            var playersList = _players.Values.Select(p =>
            {
                var continent = p.ContinentId.HasValue
                    ? _continents.FirstOrDefault(c => c.Id == p.ContinentId.Value)
                    : null;

                return new
                {
                    p.Id,
                    p.Username,
                    p.Level,
                    Continent = continent?.Name ?? "Не выбран",
                    SpawnPosition = p.ContinentId.HasValue
                        ? new { p.SpawnX, p.SpawnZ }
                        : null,
                    p.CreatedAt
                };
            }).ToList();

            return Ok(playersList);
        }

        [HttpGet("player/{id}")]
        public IActionResult GetPlayer(string id)
        {
            if (!_players.TryGetValue(id, out var player))
                return NotFound(new { error = "Игрок не найден" });

            var continent = player.ContinentId.HasValue
                ? _continents.FirstOrDefault(c => c.Id == player.ContinentId)
                : null;

            return Ok(new
            {
                player.Id,
                player.Username,
                player.Level,
                player.Experience,
                Continent = continent?.Name ?? "Не выбран",
                Element = continent?.Element,
                SpawnPosition = player.ContinentId.HasValue
                    ? new { player.SpawnX, player.SpawnZ }
                    : null,
                player.CreatedAt,
                player.LastLogin
            });
        }

        [HttpPost("register")]
        public IActionResult RegisterPlayer([FromBody] RegisterRequest? request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username))
                return BadRequest(new { error = "Имя игрока обязательно" });

            if (request.Username.Length < 3)
                return BadRequest(new { error = "Имя игрока должно содержать минимум 3 символа" });

            if (request.Username.Length > 20)
                return BadRequest(new { error = "Имя игрока не должно превышать 20 символов" });

            // Проверяем по Username
            if (_players.Values.Any(p =>
                p.Username.Equals(request.Username, StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest(new { error = "Имя игрока уже занято" });
            }

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
            {
                return Ok(new
                {
                    success = true,
                    playerId = player.Id,
                    username = player.Username,
                    message = "Игрок успешно зарегистрирован"
                });
            }

            return StatusCode(500, new { error = "Ошибка при регистрации игрока" });
        }

        [HttpPost("spawn")]
        public IActionResult SpawnOnContinent([FromBody] SpawnRequest? request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PlayerId))
                return BadRequest(new { error = "PlayerId обязательно" });

            if (!_players.TryGetValue(request.PlayerId, out var player))
                return NotFound(new { error = "Игрок не найден" });

            var continent = _continents.FirstOrDefault(c => c.Id == request.ContinentId);
            if (continent == null)
                return NotFound(new { error = "Континент не найден" });

            // Проверяем требования
            if (!continent.IsStarter && player.Level < continent.LevelRequirement)
            {
                return BadRequest(new
                {
                    error = $"Требуется {continent.LevelRequirement} уровень"
                });
            }

            if (continent.CurrentPlayers >= continent.MaxPlayers)
                return BadRequest(new { error = "Континент переполнен" });

            // Уменьшаем счетчик старого континента
            if (player.ContinentId.HasValue)
            {
                var oldContinent = _continents.FirstOrDefault(c => c.Id == player.ContinentId.Value);
                if (oldContinent != null && oldContinent.CurrentPlayers > 0)
                {
                    oldContinent.CurrentPlayers--;
                }
            }

            // Обновляем данные игрока
            player.ContinentId = continent.Id;

            var random = new Random();
            player.SpawnX = continent.CenterX + random.Next(-50, 50);
            player.SpawnZ = continent.CenterZ + random.Next(-50, 50);
            player.CurrentX = player.SpawnX;
            player.CurrentZ = player.SpawnZ;
            player.LastLogin = DateTime.UtcNow;

            // Увеличиваем счетчик
            continent.CurrentPlayers++;

            return Ok(new
            {
                success = true,
                message = $"Добро пожаловать на {continent.Name}!",
                player = new
                {
                    player.Id,
                    player.Username,
                    player.Level,
                    Continent = continent.Name,
                    SpawnPosition = new { player.SpawnX, player.SpawnZ }
                }
            });
        }

        //  МЕТОДЫ СТАТИСТИКИ 

        [HttpGet("world-stats")]
        public IActionResult GetWorldStats()
        {
            // Пересчитываем счетчики
            UpdateContinentCounters();

            var totalPlayers = _players.Count;
            var playersOnCentral = _players.Values.Count(p =>
                p.ContinentId.HasValue &&
                _continents.Any(c => c.Id == p.ContinentId.Value && c.IsCentral));

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

        //  Доп API МЕТОДЫ 

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
            }).ToList();

            var connections = new List<object>();

            // Связи от Атлантиды ко всем остальным континентам
            foreach (var continent in _continents.Where(c => !c.IsCentral))
            {
                connections.Add(new
                {
                    From = continent.Id,
                    To = 5,
                    Distance = CalculateDistance(continent.CenterX, continent.CenterZ, 0, 0),
                    Type = "elemental_to_central"
                });
            }

            return Ok(new
            {
                continents = continentPositions,
                connections = connections,
                centerContinentId = 5,
                timestamp = DateTime.UtcNow
            });
        }

        [HttpPost("gain-experience")]
        public IActionResult GainExperience([FromBody] ExperienceRequest? request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PlayerId))
                return BadRequest(new { error = "Неверный запрос" });

            if (!_players.TryGetValue(request.PlayerId, out var player))
                return NotFound(new { error = "Игрок не найден" });

            if (request.Amount <= 0)
                return BadRequest(new { error = "Количество опыта должно быть положительным" });

            player.Experience += request.Amount;

            // Проверяем повышение уровня
            var expToNextLevel = CalculateExpToNextLevel(player.Level);
            var levelsGained = 0;

            while (player.Experience >= expToNextLevel)
            {
                player.Experience -= expToNextLevel;
                player.Level++;
                levelsGained++;
                expToNextLevel = CalculateExpToNextLevel(player.Level);
            }

            player.LastLogin = DateTime.UtcNow;

            return Ok(new
            {
                success = true,
                playerId = player.Id,
                username = player.Username,
                newLevel = player.Level,
                levelsGained = levelsGained,
                currentExperience = player.Experience,
                experienceToNextLevel = CalculateExpToNextLevel(player.Level),
                canEnterAtlantis = player.Level >= 10
            });
        }

        [HttpGet("player/{id}/available-continents")]
        public IActionResult GetAvailableContinents(string id)
        {
            if (!_players.TryGetValue(id, out var player))
                return NotFound(new { error = "Игрок не найден" });

            var availableContinents = _continents
                .Where(c => c.IsStarter || player.Level >= c.LevelRequirement)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Element,
                    Players = $"{c.CurrentPlayers}/{c.MaxPlayers}",
                    CanSpawn = true
                });

            var lockedContinents = _continents
                .Where(c => !c.IsStarter && player.Level < c.LevelRequirement)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    RequiredLevel = c.LevelRequirement,
                    CurrentLevel = player.Level
                });

            return Ok(new
            {
                playerId = player.Id,
                playerLevel = player.Level,
                available = availableContinents,
                locked = lockedContinents,
                canAccessAtlantis = player.Level >= 10
            });
        }

        //  ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ 

        private void UpdateContinentCounters()
        {
            // Обнуляем счетчики
            foreach (var continent in _continents)
            {
                continent.CurrentPlayers = 0;
            }

            // Пересчитываем
            foreach (var player in _players.Values)
            {
                if (player.ContinentId.HasValue)
                {
                    var continent = _continents.FirstOrDefault(c => c.Id == player.ContinentId.Value);
                    if (continent != null)
                    {
                        continent.CurrentPlayers++;
                    }
                }
            }
        }

        private static string GetContinentColor(string element)
        {
            return element switch
            {
                "earth" => "#8B4513",
                "wind" => "#87CEEB",
                "fire" => "#FF4500",
                "water" => "#1E90FF",
                "void" => "#9370DB",
                _ => "#808080"
            };
        }

        private static double CalculateDistance(float x1, float z1, float x2, float z2)
        {
            return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(z2 - z1, 2));
        }

        private static int CalculateExpToNextLevel(int level)
        {
            return 100 + (level - 1) * 50;
        }
    }
}