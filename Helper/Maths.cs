// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Numerics;

namespace TangramCypher.Helper
{
    public static class Maths
    {
        public static BigInteger Mod(BigInteger a, BigInteger n)
        {
            var result = a % n;
            if ((result < 0 && n > 0) || (result > 0 && n < 0))
                result += n;

            return result;
        }

        public static BigInteger PickGenerator(BigInteger p)
        {
            for (BigInteger i = 1; i < p; i++)
            {
                var rand = i;
                BigInteger exp = 1;
                var next = Mod(rand, p);
                while (next != 1)
                {
                    next = Mod(BigInteger.Multiply(next, rand), p);
                    exp++;
                    if (exp.Equals(BigInteger.Subtract(p, 1)))
                        return rand;
                }
            }

            return 0;
        }

        public static BigInteger GeneratePrime()
        {
            BigInteger prime = 1;
            while (!IsPrime(prime)) { prime = Sodium.SodiumCore.GetRandomNumber(sizeof(uint)); }
            return prime;
        }

        public static bool IsPrime(BigInteger number)
        {
            if (number == 1) return false;
            if (number == 2) return true;
            var boundary = Sqrt(number);
            for (BigInteger i = 2; i <= boundary; ++i)
            {
                if (number % i == 0) return false;
            }
            return true;
        }

        public static BigInteger Sqrt(BigInteger n)
        {
            if (n == 0) return 0;
            if (n > 0)
            {
                int bitLength = Convert.ToInt32(Math.Ceiling(BigInteger.Log(n, 2)));
                var root = BigInteger.One << (bitLength / 2);

                while (!IsSqrt(n, root))
                {
                    root += n / root;
                    root /= 2;
                }

                return root;
            }

            throw new ArithmeticException("NaN");
        }

        public static bool IsSqrt(BigInteger n, BigInteger root)
        {
            var lowerBound = root * root;
            var upperBound = (root + 1) * (root + 1);

            return n >= lowerBound && n < upperBound;
        }
    }
}
