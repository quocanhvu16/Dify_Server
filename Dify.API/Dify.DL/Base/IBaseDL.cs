using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dify.DL.Base
{
    public interface IBaseDL
    {
        Task<List<T>> SearchRecords<T>(Dictionary<string, object> searchParams) where T : class;
    }
}
