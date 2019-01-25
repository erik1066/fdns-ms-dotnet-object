using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Foundation.ObjectService.WebUI
{
#pragma warning disable 1591 // disables the warnings about missing Xml code comments
    public class HttpHealthCheck : IHealthCheck
    {
        private readonly string _url;
        private readonly int _degradationThreshold;
        private readonly string _description;
        private readonly HttpClient _client;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="description">Description of the health check</param>
        /// <param name="url">The HTTP URL to use for the check</param>
        /// <param name="clientFactory">The HTTP client factory to use for the check</param>
        /// <param name="degradationThreshold">The threshold in milliseconds after which to consider the service degraded</param>
        /// <param name="cancellationThreshold">The threshold in milliseconds after which to cancel the check and consider the service unavailable</param>
        public HttpHealthCheck(string description, string url, IHttpClientFactory clientFactory, int degradationThreshold = 1000, int cancellationThreshold = 2000)
        {
            #region Input validation
            if (string.IsNullOrEmpty(description))
            {
                throw new ArgumentNullException(nameof(description));
            }
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException(nameof(url));
            }
            if (clientFactory == null)
            {
                throw new ArgumentNullException(nameof(clientFactory));
            }
            if (degradationThreshold < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(degradationThreshold));
            }
            if (cancellationThreshold < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cancellationThreshold));
            }
            #endregion // Input validation

            _description = description;
            _url = url;
            _degradationThreshold = degradationThreshold;
            _client = clientFactory.CreateClient(description);
            _client.Timeout = new TimeSpan(0, 0, 0, cancellationThreshold); // five-second timeout
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            HealthCheckResult checkResult;

            using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, _url))
            {
                var sw = new Stopwatch();
                sw.Start();

                try 
                {
                    HttpStatusCode status;
                    bool isSuccessCode = false;

                    using (HttpResponseMessage response = await _client.SendAsync(requestMessage))
                    {
                        status = response.StatusCode;
                        isSuccessCode = response.IsSuccessStatusCode;
                    }

                    sw.Stop();
                    var elapsed = sw.Elapsed.TotalMilliseconds.ToString("N0");

                    if (!isSuccessCode)
                    {
                        checkResult = HealthCheckResult.Unhealthy(
                            data: new Dictionary<string, object> { ["elapsed"] = elapsed },
                            description: $"{_description} liveness probe failed due to {status} HTTP response");
                    }
                    else if (sw.Elapsed.TotalMilliseconds > _degradationThreshold)
                    {
                        checkResult = HealthCheckResult.Degraded(
                            data: new Dictionary<string, object> { ["elapsed"] = elapsed },
                            description: $"{_description} liveness probe took more than {_degradationThreshold} milliseconds");
                    }
                    else 
                    {
                        checkResult = HealthCheckResult.Healthy(
                            data: new Dictionary<string, object> { ["elapsed"] = elapsed },
                            description: $"{_description} liveness probe completed in {elapsed} milliseconds");
                    }
                }
                catch (Exception ex)
                {
                    checkResult = HealthCheckResult.Unhealthy(
                        data: new Dictionary<string, object> { ["exceptionType"] = ex.GetType().ToString() },
                        description: $"{_description} liveness probe failed due to exception");
                }
                finally
                {
                    sw.Stop();
                }
            }

            return checkResult;
        }
    }
#pragma warning restore 1591
}