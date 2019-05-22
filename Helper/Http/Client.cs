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
using Dawn;
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
            Guard.Argument(baseAddress, nameof(baseAddress)).NotNull();
            Guard.Argument(path, nameof(path)).NotNull().NotEmpty();

            JObject result = null;

            using (var client = socksPortHandler == null ? new HttpClient() : new HttpClient(socksPortHandler))
            {
                client.BaseAddress = baseAddress;
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                try
                {
                    using (var request = new HttpRequestMessage(HttpMethod.Get, path))
                    using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                    {
                        var stream = await response.Content.ReadAsStreamAsync();

                        if (response.IsSuccessStatusCode)
                            result = Util.DeserializeJsonFromStream<JObject>(stream);
                        else
                        {
                            var content = await Util.StreamToStringAsync(stream);
                            logger.LogError($"Message: {content}\n StatusCode: {(int)response.StatusCode}");
                            throw new ApiException
                            {
                                StatusCode = (int)response.StatusCode,
                                Content = content
                            };
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
                }

                return Task.FromResult(result).Result;
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
            Guard.Argument(baseAddress, nameof(baseAddress)).NotNull();
            Guard.Argument(path, nameof(path)).NotNull().NotEmpty();

            IEnumerable<JObject> results = null;

            using (var client = socksPortHandler == null ? new HttpClient() : new HttpClient(socksPortHandler))
            {
                client.BaseAddress = baseAddress;
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                try
                {
                    using (var request = new HttpRequestMessage(HttpMethod.Get, path))
                    using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                    {
                        var stream = await response.Content.ReadAsStreamAsync();

                        if (response.IsSuccessStatusCode)
                            results = Util.DeserializeJsonEnumerable<JObject>(stream);
                        else
                        {
                            var content = await Util.StreamToStringAsync(stream);
                            logger.LogError($"Message: {content}\n StatusCode: {(int)response.StatusCode}");
                            throw new ApiException
                            {
                                StatusCode = (int)response.StatusCode,
                                Content = content
                            };
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
                }

                return Task.FromResult(results).Result;
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
            Guard.Argument<T>(payload, nameof(payload)).Equals(null);
            Guard.Argument(baseAddress, nameof(baseAddress)).NotNull();
            Guard.Argument(path, nameof(path)).NotNull().NotEmpty();

            JObject result = null;

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
                                result = Util.DeserializeJsonFromStream<JObject>(stream);
                            else
                            {
                                var contentResult = await Util.StreamToStringAsync(stream);
                                logger.LogError($"Result: {contentResult}\n Content: {content}\n StatusCode: {(int)response.StatusCode}");
                                throw new ApiException
                                {
                                    StatusCode = (int)response.StatusCode,
                                    Content = contentResult
                                };
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
                    }
                }
            }

            return Task.FromResult(result).Result;
        }

    }
}

