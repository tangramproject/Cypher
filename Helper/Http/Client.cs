using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TangramCypher.Helper.Http
{
    public static class Client
    {
        public static async Task<JObject> GetAsync<T>(Uri baseAddress, string path, CancellationToken cancellationToken)
        {
            if (baseAddress == null)
                throw new ArgumentNullException(nameof(baseAddress));

            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path is missing!", nameof(path));

            using (var client = new HttpClient())
            {
                // ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                client.BaseAddress = baseAddress;
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using (var request = new HttpRequestMessage(HttpMethod.Get, path))
                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    var stream = await response.Content.ReadAsStreamAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var result = Util.DeserializeJsonFromStream<JObject>(stream);
                        return Task.FromResult(result).Result;
                    }

                    var content = await Util.StreamToStringAsync(stream);
                    throw new ApiException
                    {
                        StatusCode = (int)response.StatusCode,
                        Content = content
                    };
                }
            }
        }


        public static async Task<IEnumerable<JObject>> GetRangeAsync(Uri baseAddress, string path, CancellationToken cancellationToken)
        {
            if (baseAddress == null)
                throw new ArgumentNullException(nameof(baseAddress));

            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path is missing!", nameof(path));

            using (var client = new HttpClient())
            {
                // ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                client.BaseAddress = baseAddress;
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using (var request = new HttpRequestMessage(HttpMethod.Get, path))
                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    var stream = await response.Content.ReadAsStreamAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var result = Util.DeserializeJsonEnumerable<JObject>(stream);
                        return Task.FromResult(result).Result;
                    }

                    var content = await Util.StreamToStringAsync(stream);
                    throw new ApiException
                    {
                        StatusCode = (int)response.StatusCode,
                        Content = content
                    };
                }
            }
        }

        public static async Task<JObject> PostAsync<T>(T payload, Uri baseAddress, string path, CancellationToken cancellationToken)
        {
            if (baseAddress == null)
                throw new ArgumentNullException(nameof(baseAddress));

            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path is missing!", nameof(path));

            using (var client = new HttpClient())
            {
                // ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                client.BaseAddress = baseAddress;
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using (var request = new HttpRequestMessage(HttpMethod.Post, path))
                {
                    var content = JsonConvert.SerializeObject(payload, Formatting.Indented);
                    var buffer = Encoding.UTF8.GetBytes(content);

                    request.Content = new StringContent(content, Encoding.UTF8, "application/json");

                    try
                    {
                        using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                        {
                            var stream = await response.Content.ReadAsStreamAsync();

                            if (response.IsSuccessStatusCode)
                            {
                                var result = Util.DeserializeJsonFromStream<JObject>(stream);
                                return Task.FromResult(result).Result;
                            }

                            var contentResult = await Util.StreamToStringAsync(stream);
                            throw new ApiException
                            {
                                StatusCode = (int)response.StatusCode,
                                Content = contentResult
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }

            return null;
        }
    }
}
