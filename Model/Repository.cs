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
using System.Threading;
using System.Threading.Tasks;
using Dawn;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using TangramCypher.ApplicationLayer.Actor;
using TangramCypher.ApplicationLayer.Vault;
using TangramCypher.Helper;

namespace TangramCypher.Model
{
    //TODO Better repository handling...
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
        /// Adds or replaces entity.
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="password"></param>
        /// <param name="name"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<TaskResult<bool>> AddOrReplace(Session session, StoreKey name, string key, TEntity value)
        {
            Guard.Argument(session, nameof(session)).NotNull();
            Guard.Argument(name, nameof(name)).In(new StoreKey[]
            {
                StoreKey.AddressKey, StoreKey.HashKey, StoreKey.PublicKey, StoreKey.SecretKey, StoreKey.TransactionIdKey
            });
            Guard.Argument(key, nameof(key)).NotNull().NotEmpty();
            Guard.Argument(value, nameof(value)).NotNull();

            using (await addOrReplaceMutex.LockAsync())
            {
                try
                {
                    var vault = await vaultServiceClient.GetDataAsync(session.Identifier, session.MasterKey, $"wallets/{session.Identifier.ToUnSecureString()}/wallet");

                    if (vault.Data.TryGetValue(store.ToString(), out object d))
                    {
                        var wallet = (JArray)d;
                        var jToken = wallet.FirstOrDefault(x => x.Value<string>(name.ToString()) == key);

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
        /// <param name="identifier"></param>
        /// <param name="password"></param>
        /// <param name="store"></param>
        /// <returns></returns>
        public async Task<TaskResult<IEnumerable<TEntity>>> All(Session session)
        {
            Guard.Argument(session, nameof(session)).NotNull();

            IEnumerable<TEntity> List = null;

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
        /// <param name="identifier"></param>
        /// <param name="password"></param>
        /// <param name="store"></param>
        /// <param name="name"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<TaskResult<TEntity>> Get(Session session, StoreKey name, string key)
        {
            Guard.Argument(session, nameof(session)).NotNull();
            Guard.Argument(name, nameof(name)).In(new StoreKey[]
            {
                StoreKey.AddressKey, StoreKey.HashKey, StoreKey.PublicKey, StoreKey.SecretKey, StoreKey.TransactionIdKey
            });
            Guard.Argument(key, nameof(key)).NotNull().NotEmpty();

            TEntity tEntity = default(TEntity);

            try
            {
                var vault = await vaultServiceClient.GetDataAsync(session.Identifier, session.MasterKey, $"wallets/{session.Identifier.ToUnSecureString()}/wallet");

                if (vault.Data.TryGetValue(store.ToString(), out object d))
                {
                    var wallet = (JArray)d;
                    var jToken = wallet.FirstOrDefault(x => x.Value<string>(name.ToString()) == key);

                    if (jToken != null)
                        tEntity = jToken.ToObject<TEntity>();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                return TaskResult<TEntity>.CreateFailure(ex);
            }

            return TaskResult<TEntity>.CreateSuccess(tEntity);
        }

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
        /// <param name="identifier"></param>
        /// <param name="password"></param>
        /// <param name="store"></param>
        /// <param name="name"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<TaskResult<bool>> Put(Session session, StoreKey name, string key, TEntity value)
        {
            Guard.Argument(session, nameof(session)).NotNull();
            Guard.Argument(name, nameof(name)).In(new StoreKey[]
            {
                StoreKey.AddressKey, StoreKey.HashKey, StoreKey.PublicKey, StoreKey.SecretKey, StoreKey.TransactionIdKey
            });
            Guard.Argument(key, nameof(key)).NotNull().NotEmpty();
            Guard.Argument(value, nameof(value)).NotNull();

            using (await putMutex.LockAsync())
            {
                try
                {
                    var vault = await vaultServiceClient.GetDataAsync(session.Identifier, session.MasterKey, $"wallets/{session.Identifier.ToUnSecureString()}/wallet");

                    if (vault.Data.TryGetValue(store.ToString(), out object d))
                    {
                        var wallet = (JArray)d;
                        var jToken = wallet.FirstOrDefault(x => x.Value<string>(name.ToString()) == key);

                        if (jToken == null)
                            wallet.Add(JObject.FromObject(value));
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
        /// <param name="identifier"></param>
        /// <param name="password"></param>
        /// <param name="store"></param>
        /// <returns></returns>
        public async Task<TaskResult<bool>> Truncate(Session session)
        {
            Guard.Argument(session, nameof(session)).NotNull();

            using (await truncateMutex.LockAsync())
            {
                try
                {
                    var vault = await vaultServiceClient.GetDataAsync(session.Identifier, session.MasterKey, $"wallets/{session.Identifier.ToUnSecureString()}/wallet");

                    if (vault.Data.TryGetValue(store.ToString(), out object txs))
                        vault.Data.Clear();

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