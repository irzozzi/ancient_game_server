using System;
using System.Collections.Generic;

namespace AncientServer.Models.HexMap
{
    // Вспомогательные классы должны быть объявлены ДО использования в HexCell
    public class Settlement
    {
        public string PlayerId { get; set; } = string.Empty;
        public float LocalX { get; set; } // 0..1 внутри соты
        public float LocalZ { get; set; }
        public int Level { get; set; }
    }

    public class ResourceNode
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; } = string.Empty;
        public float LocalX { get; set; }
        public float LocalZ { get; set; }
        public int Amount { get; set; }
        public int MaxAmount { get; set; }
    }

    public class Mob
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; } = string.Empty;
        public float LocalX { get; set; }
        public float LocalZ { get; set; }
        public int Level { get; set; }
        public int Health { get; set; }
    }

    public class Boss
    {
        public string Type { get; set; } = string.Empty;
        public float LocalX { get; set; }
        public float LocalZ { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }
    }

    public class HexCell
    {
        public int X { get; set; }
        public int Z { get; set; }
        public List<Settlement> Settlements { get; set; } = new();
        public List<ResourceNode> ResourceNodes { get; set; } = new();
        public List<Mob> Mobs { get; set; } = new();
        public Boss? Boss { get; set; }
        public DateTime BossRespawnTime { get; set; }
    }
}