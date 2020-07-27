// TGMWalletCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System.ComponentModel.DataAnnotations;

namespace TGMWalletCore.Model
{
    //TODO: Use byte array..
    public class Credentials
    {
        [Required]
        public string Identifier { get; set; }
        [Required]
        public string[] Mnemonic { get; set; }
        public string Passphrase { get; set; }
    }
}
