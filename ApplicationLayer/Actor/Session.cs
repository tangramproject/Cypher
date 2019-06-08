// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Collections.Generic;
using System.Security;
using Newtonsoft.Json.Linq;
using TangramCypher.Model;

namespace TangramCypher.ApplicationLayer.Actor
{
    public class Session : IEqualityComparer<Session>
    {
        public ulong Amount { get; set; }
        public bool ForwardMessage { get; set; }
        public SecureString Identifier { get; }
        public JObject LastError { get; set; }
        public SecureString MasterKey { get; }
        public string Memo { get; set; }
        public SecureString PublicKey { get; set; }
        public string RecipientAddress { get; set; }
        public SecureString SecretKey { get; set; }
        public string SenderAddress { get; set; }
        public bool SufficientFunds { get; set; }
        public Guid SessionId { get; }

        public Session(SecureString identifier, SecureString masterKey)
        {
            Identifier = identifier;
            MasterKey = masterKey;
            SessionId = Guid.NewGuid();
        }

        public bool Equals(Session x, Session y)
        {
            return x.Identifier == y.Identifier && x.MasterKey == y.MasterKey && x.SenderAddress == y.SenderAddress && x.RecipientAddress == y.RecipientAddress && x.SessionId == y.SessionId;
        }

        public int GetHashCode(Session session)
        {
            Session s = (Session)session;
            return s.SenderAddress.GetHashCode();
        }
    }
}