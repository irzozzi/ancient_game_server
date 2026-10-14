using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AncientServer.Models;
using ExcelDataReader;

namespace AncientServer.Services
{
    public static class CityExcelLoader
    {
        public static List<CityData> LoadFromExcel(string filePath)
        {
            var cities = new List<CityData>();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            var result = reader.AsDataSet();
            var table = result.Tables[0];

            for (int row = 2; row < table.Rows.Count; row++)
            {
                var levelCell = table.Rows[row][3]?.ToString();
                if (string.IsNullOrEmpty(levelCell))
                    continue;

                if (!int.TryParse(levelCell, out int level))
                    continue;

                var idCell = table.Rows[row][4]?.ToString();
                if (!int.TryParse(idCell, out int id))
                    continue;

                string name1 = table.Rows[row][5]?.ToString()?.Trim() ?? string.Empty;
                string name2 = table.Rows[row][6]?.ToString()?.Trim() ?? string.Empty;
                string name3 = table.Rows[row][7]?.ToString()?.Trim() ?? string.Empty;
                string name4 = table.Rows[row][8]?.ToString()?.Trim() ?? string.Empty;
                string buff = table.Rows[row][9]?.ToString()?.Trim() ?? string.Empty;

                if (!string.IsNullOrEmpty(name1))
                    cities.Add(new CityData { Id = id, Level = level, ContinentId = 1, Name = name1, Buff = buff });

                if (!string.IsNullOrEmpty(name2))
                    cities.Add(new CityData { Id = id, Level = level, ContinentId = 2, Name = name2, Buff = buff });

                if (!string.IsNullOrEmpty(name3))
                    cities.Add(new CityData { Id = id, Level = level, ContinentId = 3, Name = name3, Buff = buff });

                if (!string.IsNullOrEmpty(name4))
                    cities.Add(new CityData { Id = id, Level = level, ContinentId = 4, Name = name4, Buff = buff });
            }

            return cities;
        }
    }
}