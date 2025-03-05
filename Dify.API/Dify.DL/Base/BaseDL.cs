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

        protected readonly string _tableName;

        public BaseDL(IDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<T>> GetAll<T>(List<string> columns = null)
        {
            try
            {
                string columnList = (columns != null && columns.Count > 0)
                ? string.Join(", ", columns)
                : "*";

                string tableName = !string.IsNullOrEmpty(_tableName) ? _tableName : typeof(T).Name;

                string sql = $"SELECT {columnList} from {tableName};";

                var records = await _dbContext.QueryAsync<T>(sql);

                return records;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw new NotImplementedException();
            }
        }

        public async Task<List<T>> GetRecordById<T>(Guid id)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@recordID", id);

                string sql = $"SELECT * from {typeof(T).Name} WHERE {typeof(T).Name}ID = @recordID;";

                var records = await _dbContext.QueryAsync<T>(sql, parameters);

                return records;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw new NotImplementedException();
            }
        }

        public async Task<List<T>> SearchRecords<T>(Dictionary<string, object> searchParams) where T : class
        {
            // 1. Tạo tham số cho Dapper
            var parameters = new DynamicParameters();

            // 2. Xây dựng điều kiện WHERE
            var conditions = new List<string>();

            // 3. Duyệt qua từng tham số và xử lý toán tử
            foreach (var param in searchParams)
            {
                string fieldName = param.Key;
                object value = param.Value;
                string operatorSymbol = "="; // Mặc định là toán tử bằng

                // Kiểm tra nếu tham số chứa toán tử
                string[] operators = { ">=", "<=", ">", "<", "!=", "=" };
                foreach (var op in operators)
                {
                    if (fieldName.Contains(op))
                    {
                        int opIndex = fieldName.IndexOf(op);
                        operatorSymbol = op;
                        fieldName = fieldName.Substring(0, opIndex).Trim(); // Lấy tên thuộc tính
                        break;
                    }
                }

                // Thêm điều kiện vào danh sách
                conditions.Add($"{fieldName} {operatorSymbol} @{fieldName}");
                parameters.Add($"@{fieldName}", value);
            }

            // 4. Kết hợp điều kiện WHERE
            var whereClause = string.Join(" AND ", conditions);

            // 5. Tạo câu truy vấn SQL
            var query = $"SELECT * FROM {typeof(T).Name} WHERE {whereClause}";

            // 6. Thực thi truy vấn với Dapper
            return await _dbContext.QueryAsync<T>(query, parameters);
        }
    }
}
