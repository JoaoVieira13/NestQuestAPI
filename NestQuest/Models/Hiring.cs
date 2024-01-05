namespace NestQuest.Models
{
    public class Hiring
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int OfferId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
