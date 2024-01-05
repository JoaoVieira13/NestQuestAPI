namespace NestQuest.Models
{
    public class Token
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string TokenValue { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpirationDate { get; set;}
    }
}
