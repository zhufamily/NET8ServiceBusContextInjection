using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace SampleCI
{
    public class Function1
    {
        private readonly ILogger<Function1> _logger;
        private readonly IServiceBusFactory _factory;
        public Function1(ILogger<Function1> logger, IServiceBusFactory factory)
        {
            _logger = logger;
            _factory = factory;
            _factory.CreateServiceBusClient("<your_namespace>", "<your_connection_string>");
            _factory.CreateServiceBusSender("<your_namespace>", "<your_queue_or_topic_name>");
        }

        [Function("Function1")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            
            dynamic temp = new { time = DateTime.UtcNow };
            string content = JsonConvert.SerializeObject(temp);

            ServiceBusMessage msg = new ServiceBusMessage();
            msg.ContentType = "application/json";
            msg.Body = BinaryData.FromString(content);
            _factory.SendMessage("sgsldspcx", "playground", msg);
            return new OkObjectResult("Good");
        }
    }
}
