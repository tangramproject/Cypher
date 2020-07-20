// Bamboo (c) by Tangram Inc
// 
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;

namespace Tangram.Bamboo.ApplicationLayer.Commands
{
    internal class CommandDescriptorAttribute : Attribute
    {
        public string[] Name { get; }
        public string Description { get; }

        public CommandDescriptorAttribute(string[] name, string description = "") => (Name, Description) = (name, description);
    }
}
