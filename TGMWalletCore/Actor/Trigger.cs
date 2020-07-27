// TGMWalletCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

namespace TGMWalletCore.Actor
{
    public enum Trigger
    {
        Transfer,
        Accept,
        Verify,
        Unlock,
        Torch,
        Commit,
        PaymentAgreement,
        PrepareRedemptionKey,
        PublicKeyAgreement,
        Complete,
        Failed,
        RollBack
    }
}