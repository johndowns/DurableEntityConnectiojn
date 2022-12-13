using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using DurableEntityTest.Enums;

namespace DurableEntityTest.Entities
{
    public interface IConnectionEntity
    {
        Task InitializeAsync();

        Task RequestStartConnectionAsync();

        Task EstablishConnectionAsync();

        Task ReceivePayloadAsync(string payload);

        Task DisconnectAsync();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class ConnectionEntity : IConnectionEntity
    {
        private IDurableEntityContext _context;
        private ILogger _log;

        private const int HealthCheckIntervalSeconds = 10;

        [FunctionName(nameof(ConnectionEntity))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx, ILogger log)
        {
            return ctx.DispatchAsync<ConnectionEntity>(ctx, log);
        }

        public ConnectionEntity(IDurableEntityContext ctx, ILogger log)
        {
            _context = ctx;
            _log = log;
        }

        [JsonProperty("initializedTime")]
        public DateTime? InitializedTime { get; set; }

        [JsonProperty("currentConnectionStatus")]
        public ConnectionStatus CurrentConnectionStatus { get; set; }

        public Task InitializeAsync()
        {
            if (InitializedTime.HasValue)
            {
                _log.LogInformation($"{_context.EntityKey} Entity is already initialized.");
                return Task.CompletedTask;
            }

            _log.LogInformation($"{_context.EntityKey} Initialising entity.");
            InitializedTime = DateTime.UtcNow;
            CurrentConnectionStatus = ConnectionStatus.NotConnected;

            return Task.CompletedTask;
        }

        public Task RequestStartConnectionAsync()
        {
            _log.LogInformation($"{_context.EntityKey} MOCK: Make external HTTP call to provider, requesting that a connection be established.");

            CurrentConnectionStatus = ConnectionStatus.AwaitingConnectionEstablish;

            _log.LogInformation($"{_context.EntityKey} Connection request sent. Waiting for connection to be established by provider.");

            return Task.CompletedTask;
        }

        public Task EstablishConnectionAsync()
        {
            // TODO what happens if the connection state is not AwaitingConnectionEstablish?

            _log.LogInformation($"{_context.EntityKey} Received connection confirmation from provider.");

            CurrentConnectionStatus = ConnectionStatus.Connected;

            // Schedule health checks.
            var firstHealthCheckTime = InitializedTime.Value.AddSeconds(HealthCheckIntervalSeconds);
            _context.SignalEntity(_context.EntityId, firstHealthCheckTime, nameof(CheckHealthStatusAsync));

            return Task.CompletedTask;
        }

        public Task ReceivePayloadAsync(string payload)
        {
            // TODO what happens if the connection state is not Connected?

            _log.LogInformation($"{_context.EntityKey} Received payload: {payload}");

            return Task.CompletedTask;
        }

        public Task DisconnectAsync()
        {
            _log.LogInformation($"{_context.EntityKey} MOCK: Make external HTTP call to provider, advising that the connection is disconnected.");

            CurrentConnectionStatus = ConnectionStatus.NotConnected;

            return Task.CompletedTask;
        }

        public Task CheckHealthStatusAsync()
        {
            if (CurrentConnectionStatus != ConnectionStatus.Connected )
            {
                _log.LogInformation($"{_context.EntityKey} Suspending health checks because connection is not yet established.");
                return Task.CompletedTask;
            }

            _log.LogInformation($"{_context.EntityKey} Checking health status...");

            // Schedule the next health check.
            var nextCheckTime = DateTime.UtcNow.AddSeconds(HealthCheckIntervalSeconds);
            _context.SignalEntity(_context.EntityId, nextCheckTime, nameof(CheckHealthStatusAsync));

            return Task.CompletedTask;
        }
    }
}
