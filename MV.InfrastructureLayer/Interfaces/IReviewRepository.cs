using MV.DomainLayer.Entities;

namespace MV.InfrastructureLayer.Interfaces;

public interface IReviewRepository
{
    Task<Review?> GetByIdAsync(int reviewId);
    Task<List<Review>> GetByProductIdAsync(int productId, int page, int pageSize);
    Task<int> CountByProductIdAsync(int productId);
    Task<Dictionary<int, int>> GetRatingDistributionAsync(int productId);
    Task DeleteAsync(int reviewId);
}
