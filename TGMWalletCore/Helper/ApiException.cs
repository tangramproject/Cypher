// TGMWalletCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
namespace TGMWalletCore.Helper
{
    public class ApiException: Exception
    {
        public int StatusCode { get; set; }

        public string Content { get; set; }
    }
}
