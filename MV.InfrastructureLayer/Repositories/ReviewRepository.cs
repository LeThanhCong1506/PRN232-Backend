using Microsoft.EntityFrameworkCore;
using MV.DomainLayer.Entities;
using MV.InfrastructureLayer.DBContext;
using MV.InfrastructureLayer.Interfaces;

namespace MV.InfrastructureLayer.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly StemDbContext _context;

    public ReviewRepository(StemDbContext context)
    {
        _context = context;
    }

    public async Task<Review?> GetByIdAsync(int reviewId)
    {
        return await _context.Reviews.FindAsync(reviewId);
    }

    public async Task<List<Review>> GetByProductIdAsync(int productId, int page, int pageSize)
    {
        return await _context.Reviews
            .Include(r => r.User)
            .Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountByProductIdAsync(int productId)
    {
        return await _context.Reviews.CountAsync(r => r.ProductId == productId);
    }

    public async Task<Dictionary<int, int>> GetRatingDistributionAsync(int productId)
    {
        var distribution = await _context.Reviews
            .Where(r => r.ProductId == productId)
            .GroupBy(r => r.Rating)
            .Select(g => new { Rating = g.Key, Count = g.Count() })
            .ToListAsync();

        // Initialize with all 5 rating levels
        var result = new Dictionary<int, int>
        {
            { 5, 0 }, { 4, 0 }, { 3, 0 }, { 2, 0 }, { 1, 0 }
        };

        foreach (var item in distribution)
        {
            if (result.ContainsKey(item.Rating))
            {
                result[item.Rating] = item.Count;
            }
        }

        return result;
    }

    public async Task<bool> HasUserReviewedProductAsync(int userId, int productId)
    {
        return await _context.Reviews.AnyAsync(r => r.UserId == userId && r.ProductId == productId);
    }

    public async Task<Review> AddAsync(Review review)
    {
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();
        return review;
    }

    public async Task DeleteAsync(int reviewId)
    {
        var review = await _context.Reviews.FindAsync(reviewId);
        if (review != null)
        {
            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
        }
    }
}
