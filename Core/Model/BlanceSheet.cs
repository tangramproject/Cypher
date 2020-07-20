// Core (c) by Tangram Inc
// 
// Core is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;

namespace Tangram.Core.Model
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