using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LazyCache.UnitTestsAspNet
{
    public interface IDbContext
    {
        Task<int> GetInfoFromDatabaseAsync();
    }
}
