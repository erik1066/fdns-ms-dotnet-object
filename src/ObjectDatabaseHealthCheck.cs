using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Diagnostics.HealthChecks;

using Foundation.ObjectService.Data;

namespace Foundation.ObjectService.WebUI
{
    /// <summary>
    /// Health check for a NoSQL database
    /// </summary>
    public sealed class ObjectDatabaseHealthCheck : IHealthCheck
    {
        private readonly int _degradationThreshold;
        private readonly int _cancellationThreshold;
        private readonly string _description;
        private readonly IObjectService _service = null;
        private readonly bool _shouldCreateFakeObject = false;

        /// <summary>
        /// Name of the dummy database to use for checks
        /// </summary>
        public string DatabaseName { get; private set; } = "_healthcheckdatabase_";

        /// <summary>
        /// Name of the dummy collection in the dummy database to use for checks
        /// </summary>
        public string CollectionName { get; private set; } = "_healthcheckcollection_";

        /// <summary>
        /// Id of the object to use as part of the health check
        /// </summary>
        public string Id { get; private set; } = "1";

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="description">Description of the health check</param>
        /// <param name="service">The NoSQL object service to use for the check</param>
        /// <param name="databaseName">Name of the database to use for checks</param>
        /// <param name="collectionName">Name of the collection in the dummy database to use for checks</param>
        /// <param name="id">Id of the fake object to retrieve</param>
        /// <param name="shouldCreateFakeObject">Whether to create a fake object with the provided id</param>
        /// <param name="degradationThreshold">The threshold in milliseconds after which to consider the database degraded</param>
        /// <param name="cancellationThreshold">The threshold in milliseconds after which to cancel the check and consider the database unavailable</param>
        public ObjectDatabaseHealthCheck(string description, IObjectService service, string databaseName, string collectionName, string id, bool shouldCreateFakeObject, int degradationThreshold = 1000, int cancellationThreshold = 2000)
        {
            #region Input validation
            if (string.IsNullOrEmpty(description))
            {
                throw new ArgumentNullException(nameof(description));
            }
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }
            if (string.IsNullOrEmpty(databaseName))
            {
                throw new ArgumentNullException(nameof(databaseName));
            }
            if (string.IsNullOrEmpty(collectionName))
            {
                throw new ArgumentNullException(nameof(collectionName));
            }
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id));
            }
            if (degradationThreshold < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(degradationThreshold));
            }
            if (cancellationThreshold < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cancellationThreshold));
            }
            if (cancellationThreshold < degradationThreshold)
            {
                throw new InvalidOperationException("Cancellation threshold cannot be less than degredation threshold");
            }
            #endregion // Input validation

            _description = description;
            _service = service;
            DatabaseName = databaseName;
            CollectionName = collectionName;
            Id = id;
            _shouldCreateFakeObject = shouldCreateFakeObject;
            _degradationThreshold = degradationThreshold;
            _cancellationThreshold = cancellationThreshold;
        }

        /// <summary>
        /// Checks the health of the database
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>HealthCheckResult</returns>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(_cancellationThreshold);

            var t = Task.Factory.StartNew(async () =>
           {
               HealthCheckResult checkResult;

               try
               {
                   var sw = new Stopwatch();
                   sw.Start();

                   if (_shouldCreateFakeObject)
                   {
                       await _service.DeleteAsync(DatabaseName, CollectionName, Id);
                       await _service.InsertAsync(DatabaseName, CollectionName, Id, "{ 'name' : 'the nameless ones' }");
                   }
                   await _service.GetAsync(DatabaseName, CollectionName, Id);

                   sw.Stop();
                   var elapsed = sw.Elapsed.TotalMilliseconds.ToString("N0");

                   if (sw.Elapsed.TotalMilliseconds > _degradationThreshold)
                   {
                       checkResult = HealthCheckResult.Degraded(
                           data: new Dictionary<string, object> { ["elapsed"] = elapsed },
                           description: $"{_description} liveness check took more than {_degradationThreshold} milliseconds");
                   }
                   else
                   {
                       checkResult = HealthCheckResult.Healthy(
                           data: new Dictionary<string, object> { ["elapsed"] = elapsed },
                           description: $"{_description} liveness check completed in {elapsed} milliseconds");
                   }
               }
               catch (Exception ex)
               {
                   checkResult = HealthCheckResult.Unhealthy(
                       data: new Dictionary<string, object> { ["exceptionType"] = ex.GetType().ToString() },
                       description: $"{_description} liveness check failed due to exception");
               }

               return checkResult;

           }, cancellationTokenSource.Token, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default).Unwrap();

            try
            {
                t.Wait(cancellationTokenSource.Token);
            }
            catch (System.OperationCanceledException)
            {
                return HealthCheckResult.Unhealthy(
                    description: $"{_description} liveness check canceled by server for taking more than {_cancellationThreshold} milliseconds");
            }

            return await t;
        }
    }
}