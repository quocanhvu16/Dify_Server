using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dify.Common.Helper
{
    public class ConfigHelper
    {
        public static IConfiguration AppSettings { get; }

        public static IConfiguration ConnectionStrings { get; }

        static ConfigHelper()
        {
            if (AppSettings == null)
            {
                string basePath = Directory.GetCurrentDirectory();
                //string configPath = Path.Combine(Directory.GetParent(basePath).FullName, "config");
                string appSettingsPath = Path.Combine(basePath, "appsettings.json");
                AppSettings = (new ConfigurationBuilder()).AddJsonFile(appSettingsPath, optional: false, reloadOnChange: false).Build();
            }
            if (ConnectionStrings == null)
            {
                string basePath = Directory.GetCurrentDirectory();
                //string configPath = Path.Combine(Directory.GetParent(basePath).FullName, "config");
                string appSettingsPath = Path.Combine(basePath, "connections.json");
                ConnectionStrings = (new ConfigurationBuilder()).AddJsonFile(appSettingsPath, optional: false, reloadOnChange: false).Build();
            }
        }

        public static T GetSetting<T>(string key, T defaultValue = default)
        {
            // Giả sử AppSettings là Dictionary<string, string> hoặc tương tự
            var config = AppSettings[$"{key}"];
            if (string.IsNullOrEmpty(config))
            {
                return defaultValue;
            }

            try
            {
                // Sử dụng Convert.ChangeType để chuyển đổi giá trị từ string sang kiểu T
                return (T)Convert.ChangeType(config, typeof(T));
            }
            catch
            {
                // Nếu chuyển đổi thất bại, trả về defaultValue
                return defaultValue;
            }
        }

        public static string GetConnectionString(string key = "MySQLConnection")
        {
            return ConnectionStrings[key];
        }
    }
}
