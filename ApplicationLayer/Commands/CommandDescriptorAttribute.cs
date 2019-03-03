// Cypher (c) by Tangram LLC
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

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
