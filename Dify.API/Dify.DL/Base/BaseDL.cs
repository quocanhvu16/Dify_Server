using Dapper;
using Dify.Common.Database;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dify.DL.Base
{
    public class BaseDL : IBaseDL
    {
        protected readonly IDbContext _dbContext;

        public BaseDL(IDbContext dbContext)
        {
            _dbContext = dbContext;
        }
    }
}
