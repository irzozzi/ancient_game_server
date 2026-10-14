namespace AncientServer.Models
{
    public class CityData
    {
        public int Id { get; set; }
        public int Level { get; set; }
        public int ContinentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Buff { get; set; } = string.Empty;
    }
}