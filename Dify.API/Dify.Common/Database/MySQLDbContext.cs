using Dapper;
using Dify.Common.Entities;
using Dify.Common.Helper;
//using MySql.Data.MySqlClient;
using MySqlConnector;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dify.Common.Database
{
    public class MySQLDbContext : IDbContext
    {
        private readonly string _connectionString;

        private readonly string paramSymbol1 = "@";

        private readonly string paramSymbol2 = "$";

        public MySQLDbContext()
        {
            _connectionString = ConfigHelper.GetConnectionString();
        }

        public MySQLDbContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<IDbConnection> OpenConnection()
        {
            var cnn = new MySqlConnection(_connectionString);
            await cnn.OpenAsync();
            return cnn;
        }

        public async Task<(IDbConnection, IDbTransaction)> OpenConnectionAndBeginTransaction()
        {
            var cnn = new MySqlConnection(_connectionString);
            await cnn.OpenAsync();
            var tran = await cnn.BeginTransactionAsync();
            return (cnn, tran);
        }

        public async Task<List<T>> QueryAsync<T>(string sql, object parameters = null)
        {
            var result = new List<T>();
            using (var cnn = await OpenConnection())
            {
                result = (await cnn.QueryAsync<T>(sql, parameters, commandType: CommandType.Text)).ToList();
            }
            return result;
        }

        public async Task<int> ExecuteAsync(string sql, object parameters = null)
        {
            int result = 0;
            using (var cnn = await OpenConnection())
            {
                result = await cnn.ExecuteAsync(sql, parameters, commandType: CommandType.Text);
            }
            return result;
        }

        public async Task<int> ExecuteAsync(IDbTransaction transaction, string sql, object parameters = null)
        {
            int result = 0;
            result = await transaction.Connection.ExecuteAsync(sql, parameters, transaction, commandType: CommandType.Text);
            return result;
        }

        public async Task<List<T>> QueryNonQueryAsync<T>(string prodedureName, object parameters = null)
        {
            var result = new List<T>();
            using (var cnn = await OpenConnection())
            {
                var dynamicParameters = BuildDynamicParametersEntityAsync(prodedureName, cnn, null, parameters);
                result = (await cnn.QueryAsync<T>(prodedureName, dynamicParameters, commandType: CommandType.StoredProcedure)).ToList();
            }
            return result;
        }

        public async Task<int> ExecuteNonQueryAsync(string prodedureName, object parameters = null)
        {
            int result = 0;
            using (var cnn = await OpenConnection())
            {
                var dynamicParameters = BuildDynamicParametersEntityAsync(prodedureName, cnn, null, parameters);
                result = await cnn.ExecuteAsync(prodedureName, parameters, commandType: CommandType.StoredProcedure);
            }
            return result;
        }

        public async Task<int> ExecuteNonQueryAsync(IDbTransaction transaction, string prodedureName, object parameters = null)
        {
            int result = 0;
            var dynamicParameters = BuildDynamicParametersEntityAsync(prodedureName, transaction.Connection, transaction, parameters);
            result = await transaction.Connection.ExecuteAsync(prodedureName, parameters, transaction, commandType: CommandType.StoredProcedure);
            return result;
        }

        public async Task<DynamicParameters> BuildDynamicParametersDictionaryAsync(string procedureName, IDbConnection cnn, IDbTransaction transaction, Dictionary<string, object> param)
        {
            var dynamicParameters = new DynamicParameters();

            if (param == null || param.Count == 0)
            {
                return dynamicParameters;
            }

            var deriveParameters = await DeriveParametersAsync(procedureName, cnn, transaction);

            MapCommandParametersDictionary(dynamicParameters, deriveParameters, param);

            return dynamicParameters;
        }

        public void MapCommandParametersDictionary(DynamicParameters dynamicParameters, List<string> deriveParameters, Dictionary<string, object> param)
        {
            var comparer = StringComparer.OrdinalIgnoreCase;
            var paramIgnoreCase = new Dictionary<string, object>(param, comparer);
            foreach (var item in deriveParameters)
            {
                if (item == "@RETURN_VALUE")
                {
                    continue;
                }
                string paramName = item.Replace(paramSymbol1, string.Empty).Replace(paramSymbol2, string.Empty);
                var pr = paramIgnoreCase.Where(x => x.Key.Equals(paramName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                if (pr.Key != null)
                {
                    dynamicParameters.Add(item, paramIgnoreCase[paramName]);
                }
                else
                {
                    dynamicParameters.Add(item, null);
                }
            }
        }

        public async Task<DynamicParameters> BuildDynamicParametersEntityAsync(string procedureName, IDbConnection cnn, IDbTransaction transaction, object entity)
        {
            var dynamicParameters = new DynamicParameters();

            if (entity == null)
            {
                return dynamicParameters;
            }

            var deriveParameters = await DeriveParametersAsync(procedureName, cnn, transaction);

            MapCommandParametersEntity(dynamicParameters, deriveParameters, entity);

            return dynamicParameters;
        }

        public void MapCommandParametersEntity(DynamicParameters dynamicParameters, List<string> deriveParameters, object entity)
        {
            var type = entity.GetType();
            foreach (var item in deriveParameters)
            {
                if (item == "@RETURN_VALUE")
                {
                    continue;
                }
                string paramName = item.Replace(paramSymbol1, string.Empty).Replace(paramSymbol2, string.Empty);
                var pr = type.GetProperty(paramName);
                if (pr != null)
                {
                    dynamicParameters.Add(item, pr.GetValue(entity));
                }
                else
                {
                    dynamicParameters.Add(item, null);
                }
            }
        }

        public async Task<DynamicParameters> BuildDynamicParametersArrayAsync(string procedureName, IDbConnection cnn, IDbTransaction transaction, params object[] parameters)
        {
            var dynamicParameters = new DynamicParameters();

            if (parameters == null || parameters.Length == 0)
            {
                return dynamicParameters;
            }

            var deriveParameters = await DeriveParametersAsync(procedureName, cnn, transaction);

            MapCommandParametersArray(dynamicParameters, deriveParameters, parameters);

            return dynamicParameters;
        }

        public void MapCommandParametersArray(DynamicParameters dynamicParameters, List<string> deriveParameters, params object[] parameters)
        {
            var len = Math.Min(deriveParameters.Count, parameters.Length);
            for (int i = 0; i < len; i++)
            {
                var item = deriveParameters[i];
                dynamicParameters.Add(item, parameters[i]);
            }
        }

        public async Task<List<string>> DeriveParametersAsync(string procedureName, IDbConnection cnn, IDbTransaction transaction = null)
        {
            using (var cmd = (MySqlCommand)cnn.CreateCommand())
            {
                cmd.Transaction = (MySqlTransaction)transaction;
                cmd.CommandText = procedureName;
                cmd.CommandType = CommandType.StoredProcedure;

                await MySqlCommandBuilder.DeriveParametersAsync(cmd);

                return cmd.Parameters.Select(p => p.ParameterName).ToList();
            }
        }

        public static void RegisterTypeHandler()
        {
            SqlMapper.AddTypeHandler(new GraphTypeHandler());
        }
    }

    public class GraphTypeHandler : SqlMapper.TypeHandler<Graph>
    {
        public override Graph Parse(object value)
        {
            return JsonConvert.DeserializeObject<Graph>(value.ToString());
        }

        public override void SetValue(System.Data.IDbDataParameter parameter, Graph value)
        {
            parameter.Value = JsonConvert.SerializeObject(value);
        }
    }
}
