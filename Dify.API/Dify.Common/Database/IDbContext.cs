using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace Dify.Common.Database
{
    public interface IDbContext
    {
        Task<IDbConnection> OpenConnection();

        Task<(IDbConnection, IDbTransaction)> OpenConnectionAndBeginTransaction();

        Task<List<T>> QueryAsync<T>(string sql, object parameters = null);

        Task<int> ExecuteAsync(string sql, object parameters = null);

        Task<int> ExecuteAsync(NpgsqlTransaction transaction, string sql, object parameters = null);

        Task<List<T>> QueryNonQueryAsync<T>(string prodedureName, object parameters = null);

        Task<int> ExecuteNonQueryAsync(string prodedureName, object parameters = null);

        Task<int> ExecuteNonQueryAsync(NpgsqlTransaction transaction, string prodedureName, object parameters = null);

    }
}
