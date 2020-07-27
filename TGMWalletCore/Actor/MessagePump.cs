// TGMWalletCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
namespace TGMWalletCore.Actor
{
    public delegate void MessagePumpEventHandler(object sender, MessagePumpEventArgs e);

    public class MessagePumpEventArgs : EventArgs
    {
        public string Message { get; set; }
        public WalletCommandApiMethod WalletCommandApi { get; set; }
    }
}
