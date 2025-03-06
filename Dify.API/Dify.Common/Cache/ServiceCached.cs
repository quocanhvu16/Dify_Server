using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dify.Common.Cache
{
    public class ServiceCached
    {
        private readonly IDistributedCache _serviceCached;

        public ServiceCached(IDistributedCache serviceCached)
        {
            _serviceCached = serviceCached;
        }

        #region Cache Temp

        public async Task<string> GetCacheAsync(string key)
        {
            var value = await _serviceCached.GetStringAsync(key);
            return value;
        }

        public async Task<T> GetCacheAsync<T>(string key)
        {
            var value = await _serviceCached.GetStringAsync(key);
            return JsonConvert.DeserializeObject<T>(value);
        }

        public async Task SetCacheAsync(string key, string value, TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null)
        {
            var cacheEntryOptions = new DistributedCacheEntryOptions();

            if (absoluteExpiration.HasValue)
            {
                cacheEntryOptions.SetSlidingExpiration(absoluteExpiration.Value);
            }
            else
            {
                cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));
            }

            if (slidingExpiration.HasValue)
            {
                cacheEntryOptions.SetSlidingExpiration(slidingExpiration.Value);
            }

            await _serviceCached.SetStringAsync(key, value, cacheEntryOptions);
        }

        public async Task SetCacheAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null)
        {
            var cacheEntryOptions = new DistributedCacheEntryOptions();
            if (absoluteExpiration.HasValue)
            {
                cacheEntryOptions.SetSlidingExpiration(absoluteExpiration.Value);
            }
            else
            {
                cacheEntryOptions.SetSlidingExpiration(TimeSpan.FromHours(1));
            }

            if (slidingExpiration.HasValue)
            {
                cacheEntryOptions.SetSlidingExpiration(slidingExpiration.Value);
            }

            if (value != null)
            {
                await _serviceCached.SetStringAsync(key, JsonConvert.SerializeObject(value), cacheEntryOptions);
            }
        }

        public async Task RemoveCacheAsync(string key)
        {
            await _serviceCached.RemoveAsync(key);
        }
        #endregion
    }
}
