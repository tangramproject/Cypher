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
using System.Threading;
using System.Threading.Tasks;
using Dawn;
using DotNetTor.SocksPort;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using TangramCypher.ApplicationLayer.Actor;

namespace TangramCypher.Helper.Http
{
    public class Client
    {
        internal const string ErrorMessage = "Please check the logs for any details.";

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
        public async Task<TaskResult<T>> AddAsync<T>(T payload, RestApiMethod apiMethod) where T : class
        {
            Guard.Argument(payload, nameof(payload)).Equals(null);

            var cts = new CancellationTokenSource();
            T result;

            try
            {
                var baseAddress = GetBaseAddress();
                var path = apiRestSection.GetSection(Constant.Routing).GetValue<string>(apiMethod.ToString());

                cts.CancelAfter(60000);
                result = await PostAsync(payload, baseAddress, path, cts.Token);

                if (result == null)
                {
                    return TaskResult<T>.CreateFailure(JObject.FromObject(new
                    {
                        success = false,
                        message = ErrorMessage
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

            return TaskResult<T>.CreateSuccess(result);
        }

        /// <summary>
        /// Get async.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="address">Address.</param>
        /// <param name="apiMethod">API method.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public async Task<TaskResult<T>> GetAsync<T>(string address, RestApiMethod apiMethod, params string[] args) where T : class
        {
            Guard.Argument(address, nameof(address)).NotNull().NotEmpty();

            var cts = new CancellationTokenSource();
            T result;

            try
            {
                var baseAddress = GetBaseAddress();
                var path = string.Empty;

                path = apiMethod.ToString().Equals(Constant.GetCoin)
                    ? string.Format(apiRestSection.GetSection(Constant.Routing).GetValue<string>(apiMethod.ToString()), address, args[0])
                    : string.Format(apiRestSection.GetSection(Constant.Routing).GetValue<string>(apiMethod.ToString()), address);

                cts.CancelAfter(60000);
                result = await GetAsync<T>(baseAddress, path, cts.Token);

                if (result == null)
                {
                    return TaskResult<T>.CreateFailure(JObject.FromObject(new
                    {
                        success = false,
                        message = ErrorMessage
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

            return TaskResult<T>.CreateSuccess(result);
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

            IEnumerable<T> messages = null;
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
        private async Task<T> GetAsync<T>(Uri baseAddress, string path, CancellationToken cancellationToken)
        {
            Guard.Argument(baseAddress, nameof(baseAddress)).NotNull();
            Guard.Argument(path, nameof(path)).NotNull().NotEmpty();

            var result = default(T);

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
                        var read = response.Content.ReadAsStringAsync().Result;
                        var jObject = JObject.Parse(read);
                        var jToken = jObject.GetValue("protobuf");
                        var byteArray = Convert.FromBase64String(jToken.Value<string>());

                        if (response.IsSuccessStatusCode)
                            result = Util.DeserializeProto<T>(byteArray);
                        else
                        {
                            var content = await response.Content.ReadAsStringAsync();
                            logger.LogError($"Result: {content}\n StatusCode: {(int)response.StatusCode}");
                            throw new Exception(content);
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
        /// Sends a POST request.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="payload"></param>
        /// <param name="baseAddress"></param>
        /// <param name="path"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<T> PostAsync<T>(T payload, Uri baseAddress, string path, CancellationToken cancellationToken)
        {
            Guard.Argument(baseAddress, nameof(baseAddress)).NotNull();
            Guard.Argument(path, nameof(path)).NotNull().NotEmpty();

            var result = default(T);

            using (var client = socksPortHandler == null ? new HttpClient() : new HttpClient(socksPortHandler))
            {
                client.BaseAddress = baseAddress;
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                try
                {
                    var proto = Util.SerializeProto(payload);

                    using (var response = await client.PostAsJsonAsync(path, proto, cancellationToken))
                    {
                        var read = response.Content.ReadAsStringAsync().Result;
                        var jObject = JObject.Parse(read);
                        var jToken = jObject.GetValue("protobuf");
                        var byteArray = Convert.FromBase64String(jToken.Value<string>());

                        if (response.IsSuccessStatusCode)
                        {
                            result = Util.DeserializeProto<T>(byteArray);
                        }
                        else
                        {
                            var content = await response.Content.ReadAsStringAsync();
                            logger.LogError($"Result: {content}\n StatusCode: {(int)response.StatusCode}");
                            throw new Exception(content);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"Message: {ex.Message}\n Stack: {ex.StackTrace}");
                }
            }

            return Task.FromResult(result).Result;
        }

    }
}

