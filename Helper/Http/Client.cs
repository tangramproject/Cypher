// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DotNetTor.SocksPort;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TangramCypher.Helper.Http
{
    public class Client
    {
        private readonly SocksPortHandler socksPortHandler;
        private readonly ILogger logger;

        public Client(ILogger logger)
        {
            this.logger = logger;
        }

        public Client(ILogger logger, SocksPortHandler socksPortHandler)
        {
            this.logger = logger;
            this.socksPortHandler = socksPortHandler;
        }

        /// <summary>
        /// Get async.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="baseAddress">Base address.</param>
        /// <param name="path">Path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public async Task<JObject> GetAsync<T>(Uri baseAddress, string path, CancellationToken cancellationToken)
        {
            if (baseAddress == null)
                throw new ArgumentNullException(nameof(baseAddress));

            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path is missing!", nameof(path));

            using (var client = socksPortHandler == null ? new HttpClient() : new HttpClient(socksPortHandler))
            {
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

        /// <summary>
        /// Get range async.
        /// </summary>
        /// <returns>The range async.</returns>
        /// <param name="baseAddress">Base address.</param>
        /// <param name="path">Path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<IEnumerable<JObject>> GetRangeAsync(Uri baseAddress, string path, CancellationToken cancellationToken)
        {
            if (baseAddress == null)
                throw new ArgumentNullException(nameof(baseAddress));

            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path is missing!", nameof(path));

            using (var client = socksPortHandler == null ? new HttpClient() : new HttpClient(socksPortHandler))
            {
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

        /// <summary>
        /// Post async.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="payload">Payload.</param>
        /// <param name="baseAddress">Base address.</param>
        /// <param name="path">Path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public async Task<JObject> PostAsync<T>(T payload, Uri baseAddress, string path, CancellationToken cancellationToken)
        {
            if (baseAddress == null)
                throw new ArgumentNullException(nameof(baseAddress));

            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path is missing!", nameof(path));

            using (var client = socksPortHandler == null ? new HttpClient() : new HttpClient(socksPortHandler))
            {
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
                        logger.LogError(ex.StackTrace);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Changes the tor circuit.
        /// </summary>
        /// <param name="host">Host.</param>
        /// <param name="port">Port.</param>
        public void ChangeCircuit(string host, int port)
        {
            try
            {
                var controlPortClient = new DotNetTor.ControlPort.Client(host, port);
                controlPortClient.ChangeCircuitAsync().Wait();
            }
            catch (Exception ex)
            {
                logger.LogError(ex.StackTrace);
            }
        }

    }
}

