namespace AncientServer.Models.DTOs
{
    public class GatherMineRequest
    {
        public string PlayerId { get; set; } = string.Empty;
        public int? Amount { get; set; }
    }
}