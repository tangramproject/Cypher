using System;
namespace Tangram.Core.Model
{
    public class TransactionMessage
    {
        public ulong Amount { get; set; }
        public string Blind { get; set; }
    }
}
