// Core (c) by Tangram Inc
// 
// Core is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Collections.Generic;
using System.Security;
using Newtonsoft.Json.Linq;

namespace Tangram.Core.Actor
{
    public class Session : IEqualityComparer<Session>
    {
        public ulong Amount { get; set; }
        public bool HasFunds { get; set; }
        public SecureString Identifier { get; }
        public JObject LastError { get; set; }
        public string Memo { get; set; }
        public SecureString Passphrase { get; }
        public string RecipientAddress { get; set; }
        public string SenderAddress { get; set; }
        public Guid SessionId { get; }

        public Session(SecureString identifier, SecureString passphrase)
        {
            Identifier = identifier;
            Passphrase = passphrase;
            SessionId = Guid.NewGuid();
        }

        public bool Equals(Session x, Session y)
        {
            return x.Identifier == y.Identifier && x.Passphrase == y.Passphrase && x.SenderAddress == y.SenderAddress && x.RecipientAddress == y.RecipientAddress && x.SessionId == y.SessionId;
        }

        public int GetHashCode(Session session)
        {
            Session s = session;
            return s.SenderAddress.GetHashCode();
        }
    }
}