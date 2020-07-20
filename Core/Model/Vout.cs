// Core (c) by Tangram Inc
// 
// Core is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

namespace Tangram.Core.Model
{
    public class Vout
    {
        public string[] C { get; set; }
        public string[] E { get; set; }
        public string[] N { get; set; }
        public string[] P { get; set; }
        public string[] R { get; set; }

        public Vout(int size)
        {
            C = new string[size];
            E = new string[size];
            N = new string[size];
            P = new string[size];
            R = new string[size];
        }
    }
}
