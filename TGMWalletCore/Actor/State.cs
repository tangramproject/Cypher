// TGMWalletCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

namespace TGMWalletCore.Actor
{
    public enum State
    {
        Audited,
        Burned,
        Keys,
        Committed,
        Agree,
        Redemption,
        Track,
        Payment,
        Holder,
        Owner,
        New,
        Completed,
        RedemptionKey,
        PublicKeyAgree,
        Failure,
        Reversed
    }
}