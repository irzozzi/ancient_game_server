namespace AncientServer.Models
{
    public class Continent
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Element { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public float CenterX { get; set; }
        public float CenterZ { get; set; }
        public int MaxPlayers { get; set; }
        public int CurrentPlayers { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsStarter { get; set; }
        public bool IsCentral { get; set; } = false;
        public int LevelRequirement { get; set; } = 1;
        public string BonusType { get; set; } = "none";

    }
}