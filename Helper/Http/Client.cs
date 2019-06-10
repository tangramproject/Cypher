// Cypher (c) by Tangram Inc
// 
// Cypher is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
// 
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dawn;
using DotNetTor.SocksPort;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TangramCypher.ApplicationLayer.Actor;

namespace TangramCypher.Helper.Http
{
    public class Client
    {
        private readonly SocksPortHandler socksPortHandler;
        private readonly ILogger logger;
        private readonly IConfigurationSection apiRestSection;
        public Client(IConfiguration apiRestSection, ILogger logger)
        {
             this.apiRestSection = apiRestSection.GetSection(Constant.ApiGateway);
            this.logger = logger;
        }

        public Client(IConfiguration apiRestSection, ILogger logger, SocksPortHandler socksPortHandler)
        {
            this.apiRestSection = apiRestSection.GetSection(Constant.ApiGateway);
            this.logger = logger;
            this.socksPortHandler = socksPortHandler;
        }

        /// <summary>
        /// Add async.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="payload">Payload.</param>
        /// <param name="apiMethod">API method.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public async Task<TaskResult<T>> AddAsync<T>(T payload, RestApiMethod apiMethod)
        {
            Guard.Argument(payload, nameof(payload)).Equals(null);

            JObject jObject = null;
            var cts = new CancellationTokenSource();

            try
            {
                var baseAddress = GetBaseAddress();
                var path = apiRestSection.GetSection(Constant.Routing).GetValue<string>(apiMethod.ToString());

                cts.CancelAfter(60000);
                jObject = await PostAsync(payload, baseAddress, path, cts.Token);

                if (jObject == null)
                {
                    return TaskResult<T>.CreateFailure(JObject.FromObject(new
                    {
                        success = false,
                        message = "Please check the logs for any details."
                    }));
                }
            }
            catch (OperationCanceledException ex)
            {
                logger.LogWarning(ex.Message);
                return TaskResult<T>.CreateFailure(ex);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex.Message);
                return TaskResult<T>.CreateFailure(ex);
            }

            return TaskResult<T>.CreateSuccess(jObject.ToObject<T>());
        }

        /// <summary>
        /// Get async.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="address">Address.</param>
        /// <param name="apiMethod">API method.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public async Task<TaskResult<T>> GetAsync<T>(string address, RestApiMethod apiMethod)
        {
            Guard.Argument(address, nameof(address)).NotNull().NotEmpty();

            JObject jObject = null;
            var cts = new CancellationTokenSource();

            try
            {
                var baseAddress = GetBaseAddress();
                var path = string.Format(apiRestSection.GetSection(Constant.Routing).GetValue<string>(apiMethod.ToString()), address);

                cts.CancelAfter(60000);
                jObject = await GetAsync<T>(baseAddress, path, cts.Token);

                if (jObject == null)
                {
                    return TaskResult<T>.CreateFailure(JObject.FromObject(new
                    {
                        success = false,
                        message = "Please check the logs for any details."
                    }));
                }
            }
            catch (OperationCanceledException ex)
            {
                logger.LogWarning(ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex.Message);
                return TaskResult<T>.CreateFailure(ex);
            }

            return TaskResult<T>.CreateSuccess(jObject.ToObject<T>());
        }

        /// <summary>
        /// Get range async.
        /// </summary>
        /// <returns>The range async.</returns>
        /// <param name="address">Address.</param>
        /// <param name="skip">Skip.</param>
        /// <param name="take">Take.</param>
        /// <param name="apiMethod">API method.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public async Task<IEnumerable<T>> GetRangeAsync<T>(string address, int skip, int take, RestApiMethod apiMethod)
        {
            Guard.Argument(address, nameof(address)).NotNull().NotEmpty();

            IEnumerable<T> messages = null; ;
            var cts = new CancellationTokenSource();

            try
            {
                var baseAddress = GetBaseAddress();
                var path = string.Format(apiRestSection.GetSection(Constant.Routing).GetValue<string>(apiMethod.ToString()), address, skip, take);

                cts.CancelAfter(60000);

                var returnMessages = await GetRangeAsync(baseAddress, path, cts.Token);

                messages = returnMessages?.Select(m => m.ToObject<T>());
            }
            catch (OperationCanceledException ex)
            {
                logger.LogWarning(ex.Message);
            }

            return Task.FromResult(messages).Result;
        }

        /// <summary>
        /// Get async.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="baseAddress">Base address.</param>
        /// <param name="path">Path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        private async Task<JObject> GetAsync<T>(Uri baseAddress, string path, CancellationToken cancellationToken)
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

        private Uri GetBaseAddress()
        {
            return new Uri(apiRestSection.GetValue<string>(Constant.Endpoint));
        }

        /// <summary>
        /// Get range async.
        /// </summary>
        /// <returns>The range async.</returns>
        /// <param name="baseAddress">Base address.</param>
        /// <param name="path">Path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        private async Task<IEnumerable<JObject>> GetRangeAsync(Uri baseAddress, string path, CancellationToken cancellationToken)
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
        private async Task<JObject> PostAsync<T>(T payload, Uri baseAddress, string path, CancellationToken cancellationToken)
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

