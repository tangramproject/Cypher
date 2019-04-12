using System;
namespace TangramCypher.ApplicationLayer.Actor
{
    public delegate void MessagePumpEventHandler(object sender, MessagePumpEventArgs e);

    public class MessagePumpEventArgs: EventArgs
    {
        public string Message { get; set; }
    }
}
