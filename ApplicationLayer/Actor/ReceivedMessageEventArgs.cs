using System;
namespace TangramCypher.ApplicationLayer.Actor
{
    public class ReceivedMessageEventArgs : EventArgs
    {
        public bool ThroughSystem { get; set; }
        public object Message { get; set; }
    }
}
