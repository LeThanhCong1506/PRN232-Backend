using System.ComponentModel.DataAnnotations;

namespace MV.DomainLayer.DTOs.Review.Request;

public class CreateReviewRequest
{
    [Required]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
    public int Rating { get; set; }

    [MaxLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters")]
    [MinLength(10, ErrorMessage = "Comment must be at least 10 characters")]
    public string? Comment { get; set; }
}
