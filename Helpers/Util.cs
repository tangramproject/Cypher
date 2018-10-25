using System;

namespace TangramCypher.Helpers
{
    public static class Util
    {
        public static string ToHex(this byte[] data) {
            return BitConverter.ToString(data).Replace("-", string.Empty);
        }
    }
}
