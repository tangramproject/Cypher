// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Collections.Generic;

namespace TangramCypher.ApplicationLayer.Coin
{
    public class EnvelopeDto
    {
        public string Commitment { get; set; }
        public string Proof { get; set; }
        public string PublicKey { get; set; }
        public string Signature { get; set; }
    }
}
