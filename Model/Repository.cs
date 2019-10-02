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
using System.Threading.Tasks;
using Dawn;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using TangramCypher.ApplicationLayer.Actor;
using TangramCypher.ApplicationLayer.Vault;
using TangramCypher.Helper;

namespace TangramCypher.Model
{
    public abstract class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        private static readonly AsyncLock addOrReplaceMutex = new AsyncLock();
        private static readonly AsyncLock putMutex = new AsyncLock();
        private static readonly AsyncLock truncateMutex = new AsyncLock();
        private static readonly AsyncLock deleteMutex = new AsyncLock();

        private readonly IVaultServiceClient vaultServiceClient;
        private readonly ILogger logger;
        private readonly StoreName store;

        public Repository(StoreName store, IVaultServiceClient vaultServiceClient, ILogger logger)
        {
            this.store = store;
            this.vaultServiceClient = vaultServiceClient;
            this.logger = logger;
        }

        /// <summary>
        /// Adds the or replace.
        /// </summary>
        /// <returns>The or replace.</returns>
        /// <param name="session">Session.</param>
        /// <param name="name">Name.</param>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        public async Task<TaskResult<bool>> AddOrReplace(Session session, TEntity value)
        {
            Guard.Argument(session, nameof(session)).NotNull();
            Guard.Argument(value, nameof(value)).NotNull();

            using (await addOrReplaceMutex.LockAsync())
            {
                try
                {
                    var primaryKey = Util.GetPrimaryKeyName(value);
                    var vault = await vaultServiceClient.GetDataAsync(session.Identifier, session.MasterKey, $"wallets/{session.Identifier.ToUnSecureString()}/wallet");
                    if (vault.Data.TryGetValue(store.ToString(), out object d))
                    {
                        var wallet = (JArray)d;
                        var jToken = wallet.FirstOrDefault(x => x.Value<string>(primaryKey) == Util.GetPropertyValue(value, primaryKey));

                        switch (jToken)
                        {
                            case null:
                                wallet.Add(JObject.FromObject(value));
                                break;
                            default:
                                wallet.RemoveAt(wallet.IndexOf(jToken));
                                wallet.Add(JObject.FromObject(value));
                                break;
                        }
                    }
                    else
                    {
                        vault.Data.Add(store.ToString(), new List<TEntity> { value });
                    }

                    await vaultServiceClient.SaveDataAsync(session.Identifier, session.MasterKey, $"wallets/{session.Identifier.ToUnSecureString()}/wallet", vault.Data);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                    return TaskResult<bool>.CreateFailure(ex);
                }
            }

            return TaskResult<bool>.CreateSuccess(true);
        }

        /// <summary>
        /// Returns a list of all entities.
        /// </summary>
        /// <returns>The all.</returns>
        /// <param name="session">Session.</param>
        public async Task<TaskResult<IEnumerable<TEntity>>> All(Session session)
        {
            Guard.Argument(session, nameof(session)).NotNull();

            IEnumerable<TEntity> List = Enumerable.Empty<TEntity>();

            try
            {
                var vault = await vaultServiceClient.GetDataAsync(session.Identifier, session.MasterKey, $"wallets/{session.Identifier.ToUnSecureString()}/wallet");
                if (vault.Data.TryGetValue(store.ToString(), out object txs))
                {
                    List = ((JArray)txs).ToObject<IEnumerable<TEntity>>();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return TaskResult<IEnumerable<TEntity>>.CreateFailure(ex);
            }

            return TaskResult<IEnumerable<TEntity>>.CreateSuccess(List);
        }

        /// <summary>
        /// Get a single entity.
        /// </summary>
        /// <returns>The get.</returns>
        /// <param name="session">Session.</param>
        /// <param name="name">Name.</param>
        /// <param name="key">Key.</param>
        public async Task<TaskResult<TEntity>> Get(Session session, StoreKey name, string key)
        {
            Guard.Argument(session, nameof(session)).NotNull();
            Guard.Argument(name, nameof(name)).In(new StoreKey[]
            {
                StoreKey.AddressKey, StoreKey.HashKey, StoreKey.PublicKey, StoreKey.SecretKey, StoreKey.TransactionIdKey
            });
            Guard.Argument(key, nameof(key)).NotNull().NotEmpty();

            TEntity tEntity = default;

            try
            {
                var vault = await vaultServiceClient.GetDataAsync(session.Identifier, session.MasterKey, $"wallets/{session.Identifier.ToUnSecureString()}/wallet");
                if (vault.Data.TryGetValue(store.ToString(), out object d))
                {
                    var wallet = (JArray)d;
                    var jToken = wallet.FirstOrDefault(x => x.Value<string>(name.ToString()) == key);

                    if (jToken != null)
                    {
                        tEntity = jToken.ToObject<TEntity>();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return TaskResult<TEntity>.CreateFailure(ex);
            }

            return TaskResult<TEntity>.CreateSuccess(tEntity);
        }

        /// <summary>
        /// Delete the specified session, name and key.
        /// </summary>
        /// <returns>The delete.</returns>
        /// <param name="session">Session.</param>
        /// <param name="name">Name.</param>
        /// <param name="key">Key.</param>
        public async Task<TaskResult<bool>> Delete(Session session, StoreKey name, string key)
        {
            Guard.Argument(session, nameof(session)).NotNull();
            Guard.Argument(name, nameof(name)).In(new StoreKey[]
            {
                StoreKey.AddressKey, StoreKey.HashKey, StoreKey.PublicKey, StoreKey.SecretKey, StoreKey.TransactionIdKey
            });
            Guard.Argument(key, nameof(key)).NotNull().NotEmpty();

            using (await deleteMutex.LockAsync())
            {
                try
                {
                    var vault = await vaultServiceClient.GetDataAsync(session.Identifier, session.MasterKey, $"wallets/{session.Identifier.ToUnSecureString()}/wallet");

                    if (vault.Data.TryGetValue(store.ToString(), out object d))
                    {
                        var wallet = (JArray)d;
                        var jToken = wallet.FirstOrDefault(x => x.Value<string>(name.ToString()) == key);

                        if (jToken != null)
                        {
                            wallet.RemoveAt(wallet.IndexOf(jToken));
                            await vaultServiceClient.SaveDataAsync(session.Identifier, session.MasterKey, $"wallets/{session.Identifier.ToUnSecureString()}/wallet", vault.Data);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                    return TaskResult<bool>.CreateFailure(ex);
                }
            }

            return TaskResult<bool>.CreateSuccess(true);
        }

        /// <summary>
        /// Adds a new entity.
        /// </summary>
        /// <returns>The put.</returns>
        /// <param name="session">Session.</param>
        /// <param name="name">Name.</param>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        public async Task<TaskResult<bool>> Put(Session session, TEntity value)
        {
            Guard.Argument(session, nameof(session)).NotNull();
            Guard.Argument(value, nameof(value)).NotNull();

            using (await putMutex.LockAsync())
            {
                try
                {
                    var primaryKey = Util.GetPrimaryKeyName(value);
                    var vault = await vaultServiceClient.GetDataAsync(session.Identifier, session.MasterKey, $"wallets/{session.Identifier.ToUnSecureString()}/wallet");
                    if (vault.Data.TryGetValue(store.ToString(), out object d))
                    {
                        var wallet = (JArray)d;
                        var jToken = wallet.FirstOrDefault(x => x.Value<string>(primaryKey) == Util.GetPropertyValue(value, primaryKey));

                        if (jToken == null)
                        {
                            wallet.Add(JObject.FromObject(value));
                        }
                    }
                    else
                    {
                        vault.Data.Add(store.ToString(), new List<TEntity> { value });
                    }

                    await vaultServiceClient.SaveDataAsync(session.Identifier, session.MasterKey, $"wallets/{session.Identifier.ToUnSecureString()}/wallet", vault.Data);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                    return TaskResult<bool>.CreateFailure(ex);
                }
            }

            return TaskResult<bool>.CreateSuccess(true);
        }


        /// <summary>
        /// Removes the stored data.
        /// </summary>
        /// <returns>The truncate.</returns>
        /// <param name="session">Session.</param>
        public async Task<TaskResult<bool>> Truncate(Session session)
        {
            Guard.Argument(session, nameof(session)).NotNull();

            using (await truncateMutex.LockAsync())
            {
                try
                {
                    var vault = await vaultServiceClient.GetDataAsync(session.Identifier, session.MasterKey, $"wallets/{session.Identifier.ToUnSecureString()}/wallet");
                    if (vault.Data.TryGetValue(store.ToString(), out object txs))
                    {
                        vault.Data.Clear();
                    }

                    await vaultServiceClient.SaveDataAsync(session.Identifier, session.MasterKey, $"wallets/{session.Identifier.ToUnSecureString()}/wallet", vault.Data);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                    return TaskResult<bool>.CreateFailure(true);
                }
            }

            return TaskResult<bool>.CreateSuccess(true);
        }
    }
}