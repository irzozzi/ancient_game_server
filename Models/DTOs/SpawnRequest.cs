namespace AncientServer.Models.DTOs
{
    public class SpawnRequest
    {
        public string PlayerId { get; set; } = string.Empty;
        public int ContinentId { get; set; }
        public int CityId { get; set; }  // добавлено
    }
}