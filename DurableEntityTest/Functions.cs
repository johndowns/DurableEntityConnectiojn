using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using DurableEntityTest.Entities;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace DurableEntityTest
{
    public static class Function1
    {
        public const string ProviderName = "Provider1";

        // HTTP triggers

        [FunctionName("StartConnectionHttp")]
        public static async Task<HttpResponseMessage> StartConnectionHttp(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("StartConnection", null, ProviderName);

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName("EstablishConnectionHttp")]
        public static async Task<HttpResponseMessage> EstablishConnectionHttp(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("EstablishConnection", null, ProviderName);

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName("ReceivePayloadHttp")]
        public static async Task<HttpResponseMessage> ReceivePayloadHttp(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("ReceivePayload", null, (ProviderName, "TODO payload"));

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName("DisconnectHttp")]
        public static async Task<HttpResponseMessage> DisconnectHttp(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("Disconnect", null, ProviderName);

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        // Orchestrations

        [FunctionName("StartConnection")]
        public static async Task StartConnectionAsync(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var entityKey = context.GetInput<string>();
            var entityId = new EntityId(nameof(ConnectionEntity), entityKey);

            var entityProxy = context.CreateEntityProxy<IConnectionEntity>(entityId);
            await entityProxy.InitializeAsync();
            await entityProxy.RequestStartConnectionAsync();
        }

        [FunctionName("EstablishConnection")]
        public static async Task EstablishConnection(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var entityKey = context.GetInput<string>();
            var entityId = new EntityId(nameof(ConnectionEntity), entityKey);

            var entityProxy = context.CreateEntityProxy<IConnectionEntity>(entityId);
            await entityProxy.EstablishConnectionAsync();
        }

        [FunctionName("ReceivePayload")]
        public static async Task ReceivePayloadAsync(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            (string entityKey, string payload) = context.GetInput<(string, string)>();
            var entityId = new EntityId(nameof(ConnectionEntity), entityKey);

            var entityProxy = context.CreateEntityProxy<IConnectionEntity>(entityId);
            await entityProxy.ReceivePayloadAsync(payload);
        }

        [FunctionName("Disconnect")]
        public static async Task DisconnectAsync(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var entityKey = context.GetInput<string>();
            var entityId = new EntityId(nameof(ConnectionEntity), entityKey);

            var entityProxy = context.CreateEntityProxy<IConnectionEntity>(entityId);
            await entityProxy.DisconnectAsync();
        }
    }
}