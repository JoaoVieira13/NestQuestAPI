using NestQuest.Enum;
using NestQuest.Models;

public class Offer
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public string Location { get; set; }
    public OfferTimeUnit OfferTimeUnit { get; set; }
    public string OfferTimeUnitName => Enum.GetName(typeof(OfferTimeUnit), OfferTimeUnit);
    public Category Category { get; set; }
    public string StatusName => Enum.GetName(typeof(Status), Status);
    public Status Status { get; set; }
    public string PlaceName => Enum.GetName(typeof(Place), Place);
    public Place Place { get; set; }
    public string Image1 { get; set; }
    public string? Image2 { get; set; }
    public string? Image3 { get; set; }
    public User? CreatedBy { get; set; }
    public ICollection<Comment>? Comments { get; set; }
    public ICollection<Hiring>? Hirings { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
