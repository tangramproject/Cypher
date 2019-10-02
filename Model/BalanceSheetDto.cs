// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;

namespace TangramCypher.Model
{

    public class BlanceSheetDto
    {
        public DateTime DateTime { get; set; }
        public string Memo { get; set; }
        public string MoneyOut { get; set; }
        public string MoneyIn { get; set; }
        public string Balance { get; set; }
    }
}