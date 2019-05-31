// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using TangramCypher.ApplicationLayer.Actor;
using TangramCypher.ApplicationLayer.Vault;
using TangramCypher.Helper;

namespace TangramCypher.Model
{
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        private readonly IVaultServiceClient vaultServiceClient;
        private readonly ILogger logger;

        public Repository(IVaultServiceClient vaultServiceClient, ILogger logger)
        {
            this.vaultServiceClient = vaultServiceClient;
            this.logger = logger;
        }

        /// <summary>
        /// Returns a list of all entities.
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="password"></param>
        /// <param name="store"></param>
        /// <returns></returns>
        public async Task<IEnumerable<TEntity>> All(SecureString identifier, SecureString password, string store)
        {
            List<TEntity> List = null;

            using (var insecureIdentifier = identifier.Insecure())
            {
                try
                {
                    var vault = await vaultServiceClient.GetDataAsync(identifier, password, $"wallets/{insecureIdentifier.Value}/wallet");
                    if (vault.Data.TryGetValue(store, out object txs))
                    {
                        List = ((JArray)txs).ToObject<List<TEntity>>();
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                }
            }

            return List;
        }

        /// <summary>
        /// Get a single entity.
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="password"></param>
        /// <param name="store"></param>
        /// <param name="name"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<TEntity> Get(SecureString identifier, SecureString password, string store, StoreKeyApiMethod name, string key)
        {
            TEntity tEntity = default(TEntity);

            using (var insecureIdentifier = identifier.Insecure())
            {
                try
                {
                    var vault = await vaultServiceClient.GetDataAsync(identifier, password, $"wallets/{insecureIdentifier.Value}/wallet");

                    if (vault.Data.TryGetValue(store, out object stores))
                    {
                        foreach (JObject item in ((JArray)stores).Children().ToList())
                        {
                            var obj = item.GetValue(name.ToString());
                            if (obj.Value<string>().Equals(key))
                            {
                                tEntity = obj.ToObject<TEntity>();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                }
            }

            return tEntity;
        }

        /// <summary>
        /// Adds a new entity.
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="password"></param>
        /// <param name="store"></param>
        /// <param name="name"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<bool> Put(SecureString identifier, SecureString password, string store, StoreKeyApiMethod name, string key, TEntity value)
        {
            bool added = false;

            using (var insecureIdentifier = identifier.Insecure())
            {
                try
                {
                    var found = false;
                    var vault = await vaultServiceClient.GetDataAsync(identifier, password, $"wallets/{insecureIdentifier.Value}/wallet");

                    if (vault.Data.TryGetValue(store, out object txs))
                    {
                        foreach (JObject item in ((JArray)txs).Children().ToList())
                        {
                            var hash = item.GetValue(name.ToString());
                            found = hash.Value<string>().Equals(key);
                        }
                        if (!found)
                            ((JArray)txs).Add(JObject.FromObject(value));
                    }
                    else
                        vault.Data.Add(store, new List<TEntity> { value });

                    await vaultServiceClient.SaveDataAsync(identifier, password, $"wallets/{insecureIdentifier.Value}/wallet", vault.Data);

                    added = true;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                }
            }

            return added;
        }

        /// <summary>
        /// Removes the stored data.
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="password"></param>
        /// <param name="store"></param>
        /// <returns></returns>
        public async Task<bool> Truncate(SecureString identifier, SecureString password, string store)
        {
            bool cleared = false;

            using (var insecureIdentifier = identifier.Insecure())
            {
                try
                {
                    var vault = await vaultServiceClient.GetDataAsync(identifier, password, $"wallets/{insecureIdentifier.Value}/wallet");

                    if (vault.Data.TryGetValue(store, out object txs))
                        vault.Data.Clear();

                    await vaultServiceClient.SaveDataAsync(identifier, password, $"wallets/{insecureIdentifier.Value}/wallet", vault.Data);

                    cleared = true;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                }
            }

            return cleared;
        }
    }
}