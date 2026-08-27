using FurShield.Data;

namespace FurShield.Models
{
    // Loose view-model used only to render the shared ratings partial
    // (Views/Shared/_RatingWidget.cshtml). Not persisted to the database.
    public class RatingWidgetViewModel
    {
        public string TargetType { get; set; } = string.Empty; // "Product" | "Veterinarian" | "Shelter"
        public int TargetId { get; set; }
        public List<Rating> Ratings { get; set; } = new();
        public bool CanRate { get; set; }
        public int? MyScore { get; set; }
        public string? MyComment { get; set; }

        public double AverageScore => Ratings.Any() ? Math.Round(Ratings.Average(r => r.Score), 1) : 0;
        public int Count => Ratings.Count;
    }
}
