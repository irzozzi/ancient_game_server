using System;

namespace AncientServer.Models
{
    public class Player
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Username { get; set; } = string.Empty;
        public int? ContinentId { get; set; }
        public float SpawnX { get; set; }
        public float SpawnZ { get; set; }
        public float CurrentX { get; set; }
        public float CurrentZ { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastLogin { get; set; }
        public bool IsActive { get; set; }
        public int Level { get; set; } = 1;
        public int Experience { get; set; } = 0;
        public string GuildId { get; set; } = string.Empty;
        public int Platinum { get; set; } = 0;
        public int SpiritEnergy { get; set; } = 0;

        // Поля для города и поместья (вместо старых CellX/CellZ)
        public int CityId { get; set; } = -1;
        public float SettlementLocalX { get; set; }
        public float SettlementLocalZ { get; set; }
    }
}