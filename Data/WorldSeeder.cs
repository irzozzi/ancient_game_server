using AncientServer.Models;
using AncientServer.Services;
using Microsoft.EntityFrameworkCore;

namespace AncientServer.Data
{
    public static class WorldSeeder
    {
        public static async Task SeedAsync(AncientGameContext context)
        {
            Console.WriteLine("[WorldSeeder] === Старт сидера ===");

            bool hasContinents = await context.Continents.AnyAsync();
            bool hasCities = await context.Cities.AnyAsync();
            Console.WriteLine($"[WorldSeeder] Континенты в БД: {hasContinents}, города в БД: {hasCities}");

            if (hasContinents || hasCities)
            {
                Console.WriteLine("[WorldSeeder] Данные уже есть, пропускаем.");
                return;
            }

            var continents = new List<Continent>
            {
                new Continent { Id = 1, Name = "Макошь", Element = "earth", Description = "Континент земли", CenterX = 200, CenterZ = 200, MaxPlayers = 1000, CurrentPlayers = 0, IsAvailable = true, IsStarter = true, LevelRequirement = 1, BonusType = "defense", IsCentral = false },
                new Continent { Id = 2, Name = "Стрибог", Element = "wind", Description = "Континент ветра", CenterX = -200, CenterZ = 200, MaxPlayers = 1000, CurrentPlayers = 0, IsAvailable = true, IsStarter = true, LevelRequirement = 1, BonusType = "speed", IsCentral = false },
                new Continent { Id = 3, Name = "Семаргл", Element = "fire", Description = "Континент огня", CenterX = -200, CenterZ = -200, MaxPlayers = 1000, CurrentPlayers = 0, IsAvailable = true, IsStarter = true, LevelRequirement = 1, BonusType = "attack", IsCentral = false },
                new Continent { Id = 4, Name = "Давана", Element = "water", Description = "Континент воды", CenterX = 200, CenterZ = -200, MaxPlayers = 1000, CurrentPlayers = 0, IsAvailable = true, IsStarter = true, LevelRequirement = 1, BonusType = "wisdom", IsCentral = false },
                new Continent { Id = 5, Name = "Атлантида", Element = "void", Description = "Центральный континент", CenterX = 0, CenterZ = 0, MaxPlayers = 4000, CurrentPlayers = 0, IsAvailable = true, IsStarter = false, LevelRequirement = 15, BonusType = "all", IsCentral = true }
            };

            Console.WriteLine("[WorldSeeder] Добавляем континенты...");
            context.Continents.AddRange(continents);
            await context.SaveChangesAsync();
            Console.WriteLine("[WorldSeeder] Континенты сохранены.");

            var allCities = new List<City>();
            int nextCityId = 1;

            for (int continentId = 1; continentId <= 4; continentId++)
            {
                var cities = WorldGenerator.GenerateCitiesForContinent(continentId, ref nextCityId);
                allCities.AddRange(cities);
                Console.WriteLine($"[WorldSeeder] Сгенерировано {cities.Count} городов для континента {continentId}");
            }

            Console.WriteLine($"[WorldSeeder] Сохраняем {allCities.Count} городов...");
            context.Cities.AddRange(allCities);
            await context.SaveChangesAsync();

            Console.WriteLine($"[WorldSeeder] === Загружено {continents.Count} континентов и {allCities.Count} городов ===");
        }
    }
}