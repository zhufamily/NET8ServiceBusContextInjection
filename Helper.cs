using Azure.Messaging.ServiceBus;

namespace SampleCI
{
    public interface IServiceBusFactory
    {
        ServiceBusClient CreateServiceBusClient(string serviceBusNameSapce, string serviceBusConnectionString);
        ServiceBusSender CreateServiceBusSender(string serviceBusNameSapce, string queueOrTopicName);
        void SendMessage(string serviceBusNameSapce, string queueOrTopicName, ServiceBusMessage message);
    }

    public class ServiceBusSenderFactory : IAsyncDisposable
    {
        public ServiceBusSenderFactory(ServiceBusClient client)
        {
            serviceBusClient = client;
        }
        public ServiceBusClient? serviceBusClient { get; set; }
        public Dictionary<string, ServiceBusSender>? senders { get; set; } = new Dictionary<string, ServiceBusSender>();
        
        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            GC.SuppressFinalize(this);
        }

        protected async ValueTask DisposeAsyncCore()
        {
            if (serviceBusClient != null)
            {
                await serviceBusClient.DisposeAsync().ConfigureAwait(false);
                serviceBusClient = null;
            }
            if (senders != null)
            {
                foreach (ServiceBusSender? serviceBusSender in senders.Values)
                {
                    if (serviceBusSender != null)
                    {
                        await serviceBusSender.DisposeAsync().ConfigureAwait(false);
                    }
                }
                senders = null;
            }
        }
    }

    public class ServiceBusFactory : IServiceBusFactory, IAsyncDisposable
    {
        private Dictionary<string, ServiceBusSenderFactory>? clients  = new Dictionary<string, ServiceBusSenderFactory>();

        public ServiceBusClient CreateServiceBusClient(string serviceBusNameSpace, string serviceBusConnectionString)
        {
            if(clients == null)
                clients = new Dictionary<string, ServiceBusSenderFactory>();

            if (clients.ContainsKey(serviceBusNameSpace))
            {
                if (clients[serviceBusNameSpace].serviceBusClient != null)
                    return clients[serviceBusNameSpace].serviceBusClient!;
                else
                {
                    clients[serviceBusNameSpace].serviceBusClient = new ServiceBusClient(serviceBusConnectionString);
                    return clients[serviceBusNameSpace].serviceBusClient!;
                }
            }
            else
            {
                ServiceBusClient client = new ServiceBusClient(serviceBusConnectionString);
                clients.Add(serviceBusNameSpace, new ServiceBusSenderFactory(client));
                return clients[serviceBusNameSpace].serviceBusClient!;
            }
        }

        public ServiceBusSender CreateServiceBusSender(string serviceBusNameSpace, string queueOrTopicName)
        {
            if (clients == null || !clients.ContainsKey(serviceBusNameSpace))
                throw new Exception("Cannot find the Service Bus Client by the key!");
            if (clients[serviceBusNameSpace].serviceBusClient == null)
                throw new Exception("Null reference -- the Service Bus Client by the key!");

            if (clients[serviceBusNameSpace].senders == null)
            {
                clients[serviceBusNameSpace].senders = new Dictionary<string, ServiceBusSender>();
            }

            if (clients[serviceBusNameSpace].senders!.ContainsKey(queueOrTopicName))
            {
                return clients[serviceBusNameSpace].senders![queueOrTopicName];
            }
            else
            {
                ServiceBusSender sender = clients[serviceBusNameSpace].serviceBusClient!.CreateSender(queueOrTopicName);
                clients[serviceBusNameSpace].senders!.Add(queueOrTopicName, sender);
                return clients[serviceBusNameSpace].senders![queueOrTopicName];
            }
        }

        public void SendMessage(string serviceBusNameSpace, string queueOrTopicName, ServiceBusMessage message)
        {
            if (clients == null || !clients.ContainsKey(serviceBusNameSpace))
                throw new Exception("Cannot find the Service Bus Client by the key!");
            if (clients[serviceBusNameSpace].serviceBusClient == null)
                throw new Exception("Null reference -- the Service Bus Client by the key!");
            if (clients[serviceBusNameSpace].senders == null || !clients[serviceBusNameSpace].senders!.ContainsKey(queueOrTopicName))
                throw new Exception("Cannot find the Service Bus Sender by the queue or topic name!");

            ServiceBusSender sender = clients[serviceBusNameSpace].senders![queueOrTopicName];
            sender.SendMessageAsync(message).GetAwaiter().GetResult();
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            GC.SuppressFinalize(this);
        }

        protected async ValueTask DisposeAsyncCore()
        {
            if (clients != null)
            {
                foreach (ServiceBusSenderFactory factory in clients.Values)
                {
                    if(factory != null)
                        await factory.DisposeAsync().ConfigureAwait(false);
                }
                clients = null;
            }
        }
    }
}
