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
using Dawn;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using TangramCypher.ApplicationLayer.Vault;
using TangramCypher.Helper;

namespace TangramCypher.Model
{
    public class KeySetRepository : Repository<KeySetDto>, IKeySetRepository
    {
        private readonly IVaultServiceClient vaultServiceClient;
        private readonly ILogger logger;

        public KeySetRepository(IVaultServiceClient vaultServiceClient, ILogger logger)
            : base(StoreName.StoreKeys, vaultServiceClient, logger)
        {
            this.vaultServiceClient = vaultServiceClient;
            this.logger = logger;
        }

        public async Task<string> RandomAddress(SecureString identifier, SecureString password)
        {
            Guard.Argument(identifier, nameof(identifier)).NotNull();
            Guard.Argument(password, nameof(password)).NotNull();

            string address = null;

            using (var insecureIdentifier = identifier.Insecure())
            {
                try
                {
                    var vault = await vaultServiceClient.GetDataAsync(identifier, password, $"wallets/{insecureIdentifier.Value}/wallet");

                    if (vault.Data.TryGetValue(StoreName.StoreKeys.ToString(), out object keys))
                    {
                        var rnd = new Random();
                        var pkSks = ((JArray)keys).ToObject<List<KeySetDto>>();

                        address = pkSks[rnd.Next(pkSks.Count())].Address;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message);
                    throw ex;
                }
            }

            return address;
        }
    }
}