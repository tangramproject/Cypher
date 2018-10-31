using System;
using System.Collections.Generic;
using System.Text;

namespace TangramCypher.ApplicationLayer.Commands
{
    internal class CommandDescriptorAttribute : Attribute
    {
        public string[] Name { get; }
        public string Description { get; }

        public CommandDescriptorAttribute(string[] name, string description = "") => (Name, Description) = (name, description);
    }
}
