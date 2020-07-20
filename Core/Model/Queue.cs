// Core (c) by Tangram Inc
// 
// Core is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using LiteDB;

namespace Tangram.Core.Model
{

    public class Queue
    {
        public DateTime DateTime { get; set; }
        public bool PaymentFailed { get; set; }
        public bool PublicAgreementFailed { get; set; }
        public bool ReceiverFailed { get; set; }
        [BsonId]
        public Guid TransactionId { get; set; }
    }
}