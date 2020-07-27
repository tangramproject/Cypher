// TGMWalletCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;

namespace TGMWalletCore.Model
{
    public interface ICoin
    {
        Guid TransactionId { get; set; }
        int Ver { get; set; }
        string PreImage { get; set; }
        int Mix { get; set; }
        Vin Vin { get; set; }
        Vout Vout { get; set; }
    }
}