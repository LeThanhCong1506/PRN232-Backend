using MV.DomainLayer.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MV.ApplicationLayer.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(User user);
    }
}
