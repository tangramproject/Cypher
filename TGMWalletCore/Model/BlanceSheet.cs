// TGMWalletCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;

namespace TGMWalletCore.Model
{

    public class BlanceSheet
    {
        public DateTime DateTime { get; set; }
        public string Memo { get; set; }
        public string MoneyOut { get; set; }
        public string MoneyIn { get; set; }
        public string Balance { get; set; }
    }
}