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
using TangramCypher.Model;
using System.Reflection;
using LiteDB;
using ProtoBuf;
using System.IO.Compression;

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

        public static Stream TangramData(string id)
        {
            var wallets = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), "wallets");
            var wallet = Path.Combine(wallets, $"{id}.db");

            if (!Directory.Exists(wallets))
            {
                try
                {
                    Directory.CreateDirectory(wallets);
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }

            return File.Open(wallet, System.IO.FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
        }

        public static LiteRepository LiteRepositoryFactory(SecureString secret, string identifier)
        {
            return new LiteRepository(TangramData(identifier), null, secret.ToUnSecureString());
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
                    var js = new Newtonsoft.Json.JsonSerializer();
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
                    var js = new Newtonsoft.Json.JsonSerializer();
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
            var result = Math.Mod(subString, prime);

            return result;
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

        public async static Task<TaskResult<T>> TriesUntilCompleted<T>(Func<Task<TaskResult<T>>> action, int tries, int delay)
        {
            var result = default(TaskResult<T>);

            for (int i = 0; i < tries; i++)
            {
                try
                {
                    result = await action();
                    if (result.Result != null)
                        break;
                }
                finally
                {
                    await Task.Delay(delay);
                }
            }

            return result;
        }

        public static string GetPrimaryKeyName(object obj)
        {
            string pkName = null;
            var props = obj.GetType().GetProperties();

            foreach (PropertyInfo prop in props)
            {
                object[] attrs = prop.GetCustomAttributes(true);
                foreach (object attr in attrs)
                {
                    if (attr is PrimaryKey primaryKey)
                        pkName = prop.Name;
                }
            }

            return pkName;
        }

        public static T GetPropertyValue<T>(object obj, string propName)
        {
            return (T)obj.GetType().GetProperty(propName).GetValue(obj, null);
        }

        public static string GetPropertyValue(object obj, string propName)
        {
            return obj.GetType().GetProperty(propName).GetValue(obj, null).ToString();
        }

        public static void SetPropertyValue(object obj, string propName, ulong value)
        {
            obj.GetType().GetProperty(propName).SetValue(obj, value);
        }

        public static ulong Sum(IEnumerable<ulong> source)
        {
            var sum = 0UL;
            foreach (var number in source)
            {
                sum += number;
            }
            return sum;
        }

        public static ulong Sum(IEnumerable<TransactionDto> source, TransactionType transactionType)
        {
            var amounts = source.Where(tx => tx.TransactionType == transactionType).Select(p => p.Amount);
            var sum = 0UL;

            foreach (var amount in amounts)
            {
                sum += amount;
            }
            return sum;
        }

        public static byte[] SerializeProto<T>(T data)
        {
            try
            {
                using (var ms = new MemoryStream())
                {
                    Serializer.Serialize(ms, data);
                    return ms.ToArray();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Stream SerializeProtoToStream<T>(T data)
        {
            try
            {
                var memStream = new MemoryStream();

                using (var ms = new MemoryStream())
                {
                    Serializer.Serialize(ms, data);

                    ms.WriteTo(memStream);
                }

                return memStream;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static T DeserializeProto<T>(byte[] data)
        {
            try
            {
                using (var ms = new MemoryStream(data))
                {
                    return Serializer.Deserialize<T>(ms);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static byte[] SerializeCompressProto<T>(T data)
        {
            try
            {
                using (MemoryStream compressed = new MemoryStream())
                {
                    using (GZipStream gzip = new GZipStream(compressed, CompressionMode.Compress, true))
                    {
                        Serializer.Serialize(gzip, data);
                    }

                    byte[] b = new byte[compressed.Length];

                    Array.Copy(compressed.GetBuffer(), b, (int)compressed.Length);

                    return b;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static T DeserializeCompressedProto<T>(byte[] data)
        {
            try
            {
                using (MemoryStream stream = new MemoryStream(data))
                {
                    using (GZipStream gzip = new GZipStream(stream, CompressionMode.Decompress))
                    {
                        return Serializer.Deserialize<T>(gzip);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //public static byte[] GetBytes(string str)
        //{
        //    byte[] bytes = new byte[str.Length * sizeof(char)];
        //    System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
        //    return bytes;
        //}

        //public static string GetString(byte[] bytes)
        //{
        //    char[] chars = new char[bytes.Length / sizeof(char)];
        //    System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
        //    return new string(chars);
        //}

        public static unsafe byte[] GetBytes(string str)
        {
            if (str == null) throw new ArgumentNullException(nameof(str));
            if (str.Length == 0) return new byte[0];

            fixed (char* p = str)
            {
                return new Span<byte>(p, str.Length * sizeof(char)).ToArray();
            }
        }

        public static unsafe string GetString(byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (bytes.Length % sizeof(char) != 0) throw new ArgumentException($"Invalid {nameof(bytes)} length");
            if (bytes.Length == 0) return string.Empty;

            fixed (byte* p = bytes)
            {
                return new string(new Span<char>(p, bytes.Length / sizeof(char)));
            }
        }
    }
}
