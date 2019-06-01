// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using Microsoft.Extensions.Logging;
using TangramCypher.ApplicationLayer.Actor;
using TangramCypher.ApplicationLayer.Vault;
using TangramCypher.ApplicationLayer.Wallet;

namespace TangramCypher.Model
{
    public class RedemptionRepository : Repository<MessageStoreDto>, IRedemptionRepository
    {
        public RedemptionRepository(IVaultServiceClient vaultServiceClient, ILogger logger)
            : base(StoreName.Redemption, vaultServiceClient, logger)
        {
        }
    }
}