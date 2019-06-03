// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Threading.Tasks;
using Stateless.Graph;
using TangramCypher.Helper;
using TangramCypher.Model;
using State = TangramCypher.ApplicationLayer.Actor.State;

namespace TangramCypher.ApplicationLayer.Actor
{
    public partial class ActorService
    {
        private bool canSpend;
        public bool CanSpend => canSpend;

        private CoinDto burnt;
        public CoinDto Burnt => burnt;

        private bool committed;
        public bool Committed => committed;

        public State State => machine.State;

        public string Graph() => UmlDotGraph.Format(machine.GetInfo());

        public async Task Tansfer(SendPaymentDto sender) => await machine.FireAsync(transferTrigger, sender);

        private void Configure()
        {
            machine.Configure(State.New)
                   .Permit(Trigger.Verify, State.Audited);

            machine.Configure(State.Audited)
                .SubstateOf(State.New)
                .OnEntryFromAsync(transferTrigger, OnCheckBalance)
                .PermitReentry(Trigger.Verify)
                .PermitIf(Trigger.Unlock, State.Keys, () => CanSpend);

            machine.Configure(State.Keys)
                .OnEntryAsync(async () =>
               {
                   await Unlock();
                   await machine.FireAsync(Trigger.Torch);
               })
                .Permit(Trigger.Torch, State.Burned);

            machine.Configure(State.Burned)
                .OnEntryAsync(async () =>
                {
                    burnt = await Util.TriesUntilCompleted<CoinDto>(async () => { return await Spend(); }, 10, 100);
                    await machine.FireAsync(Trigger.Commit);
                })
                .PermitIf(Trigger.Commit, State.Committed, () => Burnt == null ? false : true);

            machine.Configure(State.Committed)
                .OnEntryAsync(async () =>
                {
                    committed = await CommitReceiver();
                });
        }

        protected async Task OnCheckBalance(SendPaymentDto sender)
        {
            this
                .MasterKey(sender.Credentials.Password.ToSecureString())
                .Identifier(sender.Credentials.Identifier.ToSecureString())
                .Amount(sender.Amount)
                .Memo(sender.Memo)
                .ToAddress(sender.ToAddress);

            canSpend = await Spendable();

            await machine.FireAsync(Trigger.Unlock);
        }
    }
}