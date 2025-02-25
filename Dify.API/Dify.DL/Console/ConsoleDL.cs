using Dify.Common.Database;
using Dify.DL.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dify.DL.Console
{
    public class ConsoleDL : BaseDL, IConsoleDL
    {
        public ConsoleDL(IDbContext dbContext) : base(dbContext)
        {
        }
    }
}
