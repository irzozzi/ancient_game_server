using System;
using System.Collections.Generic;

namespace AncientServer.Models
{
    public class City
    {
        public int Id { get; set; }
        public int ContinentId { get; set; }
        public int Level { get; set; }          // 1..5
        public float CenterX { get; set; }
        public float CenterZ { get; set; }
        public string? CastleOwnerGuildId { get; set; }
        public List<PlayerSettlement> Settlements { get; set; } = new();
        public List<Mine> Mines { get; set; } = new();
        public List<MobCamp> MobCamps { get; set; } = new();
        public List<Dungeon> Dungeons { get; set; } = new();
    }

    public class PlayerSettlement
    {
        public string PlayerId { get; set; } = string.Empty;
        public float LocalX { get; set; }
        public float LocalZ { get; set; }
        public int Level { get; set; } = 1;
    }

    public class Mine
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; } = "platinum";
        public float LocalX { get; set; }
        public float LocalZ { get; set; }
        public int Amount { get; set; } = 1000;
        public int MaxAmount { get; set; } = 1000;
        public DateTime? RespawnTime { get; set; }
    }

    public class MobCamp
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string MobType { get; set; } = "goblin";
        public int Level { get; set; } = 1;
        public float LocalX { get; set; }
        public float LocalZ { get; set; }
        public bool IsAlive { get; set; } = true;
        public DateTime? RespawnTime { get; set; }
    }

    public class Dungeon
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string DungeonType { get; set; } = "ancient_temple";
        public float LocalX { get; set; }
        public float LocalZ { get; set; }
        public DateTime LastCompletedTime { get; set; }
        public int CooldownHours { get; set; } = 24;
        public bool IsAvailable => DateTime.UtcNow >= LastCompletedTime.AddHours(CooldownHours);
    }
}