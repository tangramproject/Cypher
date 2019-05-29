// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using SimpleBase;
using System.Collections.Generic;
using TangramCypher.ApplicationLayer.Actor;
using System.Text;
using System.Linq;
using System.Runtime.InteropServices;
using System.IO;
using System.Net;
using System.Security;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Numerics;
using System.Dynamic;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Sodium;

namespace TangramCypher.Helper
{
    public static class Util
    {
        internal static Random _Random = new Random();

        public static string Pop(string value, string delimiter)
        {
            var stack = new Stack<string>(value.Split(new string[] { delimiter }, StringSplitOptions.None));
            return stack.Pop();
        }

        public static RedemptionKeyDto FreeCommitmentKey(string base58Key)
        {
            var base58 = Base58.Bitcoin.Decode(base58Key);
            var proof = Encoding.UTF8.GetString(base58).Substring(0, 64);
            var key1 = Encoding.UTF8.GetString(base58).Substring(64, 128);
            var key2 = Encoding.UTF8.GetString(base58).Substring(128, 192);

            return new RedemptionKeyDto() { Key1 = key1, Key2 = key1, Stamp = proof };
        }

        public static IEnumerable<string> Split(string str, int chunkSize)
        {
            return Enumerable.Range(0, str.Length / chunkSize)
                .Select(i => str.Substring(i * chunkSize, chunkSize));
        }

        public static void Shuffle<T>(T[] array)
        {
            int n = array.Length;
            for (int i = 0; i < n; i++)
            {
                int r = i + _Random.Next(n - i);
                T t = array[r];
                array[r] = array[i];
                array[i] = t;
            }
        }

        public static OSPlatform GetOSPlatform()
        {
            OSPlatform osPlatform = OSPlatform.Create("Other Platform");
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            osPlatform = isWindows ? OSPlatform.Windows : osPlatform;

            bool isOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            osPlatform = isOSX ? OSPlatform.OSX : osPlatform;

            bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            osPlatform = isLinux ? OSPlatform.Linux : osPlatform;

            return osPlatform;
        }

        public static string AppDomainDirectory()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        public static string ToPlainString(SecureString secure)
        {
            return new NetworkCredential(string.Empty, secure).Password;
        }

        public static T DeserializeJsonFromStream<T>(Stream stream)
        {
            if (stream == null || stream.CanRead == false)
                return default;

            using (var sr = new StreamReader(stream))
            using (var jtr = new JsonTextReader(sr))
            {
                try
                {
                    var js = new JsonSerializer();
                    var searchResult = js.Deserialize<T>(jtr);
                    return searchResult;
                }
                catch (JsonSerializationException ex)
                {
                    throw ex;
                }

            }
        }

        public static IEnumerable<T> DeserializeJsonEnumerable<T>(Stream stream)
        {
            if (stream == null || stream.CanRead == false)
                return default;

            using (var sr = new StreamReader(stream))
            using (var jtr = new JsonTextReader(sr))
            {
                try
                {
                    var js = new JsonSerializer();
                    var searchResult = js.Deserialize<IEnumerable<T>>(jtr);
                    return searchResult;
                }
                catch (JsonSerializationException ex)
                {
                    throw ex;
                }

            }
        }

        public static async Task<string> StreamToStringAsync(Stream stream)
        {
            string content = null;

            if (stream != null)
                using (var sr = new StreamReader(stream))
                    content = await sr.ReadToEndAsync();

            return content;
        }

        public static string GetFileHash(FileInfo file)
        {
            return GetFileHash(file.FullName);
        }

        public static string GetFileHash(string fileFullName)
        {
            var bytes = File.ReadAllBytes(fileFullName);
            var hash = CryptoHash.Sha256(bytes);
            return Utilities.BinaryToHex(hash);
        }

        [CLSCompliant(false)]
#pragma warning disable CS3021 // Type or member does not need a CLSCompliant attribute because the assembly does not have a CLSCompliant attribute
        public static InsecureString Insecure(this SecureString secureString) => new InsecureString(secureString);
#pragma warning restore CS3021 // Type or member does not need a CLSCompliant attribute because the assembly does not have a CLSCompliant attribute


        public static BigInteger GetHashNumber(byte[] hash, BigInteger prime, int bytes)
        {
            var intH = new BigInteger(hash);
            var subString = BigInteger.Parse(intH.ToString().Substring(0, bytes));
            var result = Maths.Mod(subString, prime);

            return result;
        }

        public static void AddProperty(ExpandoObject expando, string propertyName, object propertyValue)
        {
            var expandoDict = expando as IDictionary<string, object>;
            if (expandoDict.ContainsKey(propertyName))
                expandoDict[propertyName] = propertyValue;
            else
                expandoDict.Add(propertyName, propertyValue);
        }

        public static void LogException(IConsole console, ILogger logger, Exception e)
        {
            console.BackgroundColor = ConsoleColor.Red;
            console.ForegroundColor = ConsoleColor.White;
            console.WriteLine(e.ToString());
            logger.LogError(e, Environment.NewLine);
            console.ResetColor();
        }

        public static void LogWarning(IConsole console, ILogger logger, string message)
        {
            console.ForegroundColor = ConsoleColor.Yellow;
            console.WriteLine(message);
            console.ResetColor();
            logger.LogWarning(message);
        }

        public static byte[] FormatNetworkAddress(byte[] networkAddress)
        {
            try
            {
                byte[] pk = new byte[32];
                Array.Copy(networkAddress, 1, pk, 0, 32);
                return pk;
            }
            catch
            {
            }

            return networkAddress;
        }

        public async static Task<T> TriesUntilCompleted<T>(Func<Task<T>> action, int tries, int delay, T expected)
        {
            var result = default(T);

            for (int i = 0; i < tries; i++)
            {
                try
                {
                    result = await action();
                    if (result.Equals(expected))
                        break;
                }
                finally
                {
                    await Task.Delay(delay);
                }
            }

            return result;
        }

        public async static Task<T> TriesUntilCompleted<T>(Func<Task<T>> action, int tries, int delay)
        {
            var result = default(T);

            for (int i = 0; i < tries; i++)
            {
                try
                {
                    result = await action();
                    if (result != null)
                        break;
                }
                finally
                {
                    await Task.Delay(delay);
                }
            }

            return result;
        }
    }
}
