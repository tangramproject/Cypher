﻿// TGMWalletCore by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.IO;
using System.Net;
using System.Security;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Sodium;
using TGMWalletCore.Model;
using LiteDB;
using ProtoBuf;

namespace TGMWalletCore.Helper
{
    public static class Util
    {
        internal static Random _random = new Random();

        public static IEnumerable<string> Split(string str, int chunkSize)
        {
            return Enumerable.Range(0, str.Length / chunkSize)
                .Select(i => str.Substring(i * chunkSize, chunkSize));
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

        public static string WalletPath(string id)
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

            return wallet;
        }

        public static LiteRepository LiteRepositoryFactory(SecureString secret, string identifier)
        {
            var connectionString = new ConnectionString
            {
                Filename = WalletPath(identifier),
                Password = secret.ToUnSecureString()
            };

            return new LiteRepository(connectionString);
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

        public async static Task<TaskResult<T>> TriesUntilCompleted<T>(Func<Task<TaskResult<T>>> action, int tries, int delay) where T : class
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

        public static ulong Sum(IEnumerable<ulong> source)
        {
            var sum = 0UL;
            foreach (var number in source)
            {
                sum += number;
            }
            return sum;
        }

        public static ulong Sum(IEnumerable<Transaction> source, TransactionType transactionType)
        {
            var amounts = source.Where(tx => tx.TransactionType == transactionType).Select(p => p.Output);
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

        public static IEnumerable<T> DeserializeListProto<T>(byte[] data) where T : class
        {
            List<T> list = new List<T>();

            try
            {
                using (var ms = new MemoryStream(data))
                {
                    T item;
                    while ((item = Serializer.DeserializeWithLengthPrefix<T>(ms, PrefixStyle.Base128, fieldNumber: 1)) != null)
                    {
                        list.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return list.AsEnumerable();
        }

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public static byte[] Combine(byte[] first, byte[] second)
        {
            byte[] ret = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
            return ret;
        }
    }
}
