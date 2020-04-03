using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LazyCache.UnitTestsAspNet
{
    public class DbContext : IDbContext
    {
        public const int EXPECTED_VALUE = 1;

        public async Task<int> GetInfoFromDatabaseAsync()
        {
            await Task.Delay(1000);//to fake async db call
            return EXPECTED_VALUE;
        }
    }
}
