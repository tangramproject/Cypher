// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Numerics;
using TangramCypher.Helper;
using TangramCypher.Helper.LibSodium;

namespace TangramCypher.ApplicationLayer.Helper.ZeroKP
{
    public struct Proof
    {
        public BigInteger C { get; set; }
        public BigInteger R { get; set; }
        public BigInteger T { get; set; }
        public BigInteger Y { get; set; }
    }

    public static class Prover
    {
        public const string n = "304725736006641064630309168029524485973";

        private static readonly BigInteger prime;

        private static byte[] hash;
        private static BigInteger g = 2;
        private static BigInteger v;

        static Prover()
        {
            prime = BigInteger.Parse(n);
        }

        public static Proof GetProof(byte[] hash)
        {
            Prover.hash = hash;
            v = BigInteger.Abs(new BigInteger(Cryptography.RandomBytes(16)));

            var c = BigInteger.Abs(Heuristic());
            var proof = new Proof { C = c, R = R(c), T = T(), Y = T() };

            //Console.WriteLine($"C: {proof.C.ToString()}\nR: {proof.R.ToString()}\nT: {proof.T.ToString()}\nY: {proof.Y.ToString()}");
            //Console.WriteLine($"v: {v.ToString()}");
            //Console.WriteLine($"X: {X().ToString()}");

            return proof;
        }

        public static BigInteger GetHashStringNumber(byte[] hash)
        {
            Prover.hash = hash;
            return X();
        }

        private static BigInteger R(BigInteger c)
        {
            return BigInteger.Subtract(v, BigInteger.Multiply(c, X()));
        }

        private static BigInteger T()
        {
            return BigInteger.ModPow(g, v, prime);
        }

        private static BigInteger Y()
        {
            return BigInteger.ModPow(g, X(), prime);
        }

        private static BigInteger X()
        {
            return Util.GetHashNumber(hash, prime, 8);
        }

        private static BigInteger Heuristic()
        {
            var h = Cryptography.GenericHashNoKey(g.ToString() + Y().ToString() + T().ToString());
            return BitConverter.ToInt32(h, 0) % prime;
        }
    }
}
