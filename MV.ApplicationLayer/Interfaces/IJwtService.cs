using MV.DomainLayer.Entities;

namespace MV.ApplicationLayer.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(User user);
    }
}
