using AncientServer.Models;

namespace AncientServer.Services
{
    public static class WorldGenerator
    {
        private const float CENTER_RADIUS = 0.15f;
        private const float MIN_DISTANCE = 0.1f;
        private static readonly Random _rand = new Random();

        public static List<City> GenerateCitiesForContinent(int continentId, ref int nextCityId)
        {
            var cities = new List<City>();

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
                        Id = nextCityId++,
                        ContinentId = continentId,
                        Level = ring.Level,
                        CenterX = x,
                        CenterZ = z,
                        Name = $"City {nextCityId - 1}",
                        Buff = ""
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

                    cities.Add(city);
                }
            }

            return cities;
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
    }
}