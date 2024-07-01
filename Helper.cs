using Azure.Messaging.ServiceBus;

namespace SampleCI
{
    public interface IServiceBusFactory
    {
        ServiceBusClient CreateServiceBusClient(string serviceBusNameSapce, string serviceBusConnectionString);
        ServiceBusSender CreateServiceBusSender(string serviceBusNameSapce, string queueOrTopicName);
        void SendMessage(string serviceBusNameSapce, string queueOrTopicName, ServiceBusMessage message);
    }

    public interface IServiceBusSenderFactory
    {
        ServiceBusSender CreateServiceBusSender(string queueOrTopicName);
        void SendMessage(string queueOrTopicName, ServiceBusMessage message);
    }

    public class ServiceBusSenderFactory : IServiceBusSenderFactory, IAsyncDisposable
    {
        public ServiceBusSenderFactory(string serviceBusConnectionString)
        {
            ServiceBusClient = new ServiceBusClient(serviceBusConnectionString);
        }

        public ServiceBusClient? ServiceBusClient { get; private set; }
        public Dictionary<string, ServiceBusSender>? Senders { get; private set; } = new Dictionary<string, ServiceBusSender>();
        
        public ServiceBusSender CreateServiceBusSender(string queueOrTopicName)
        {
            if (ServiceBusClient == null)
                throw new Exception("Null reference -- ServiceBusClient is null");
            if (Senders == null)
                Senders = new Dictionary<string, ServiceBusSender>();

            if (Senders.ContainsKey(queueOrTopicName))
                return Senders[queueOrTopicName];

            ServiceBusSender sender = ServiceBusClient.CreateSender(queueOrTopicName);
            Senders.Add(queueOrTopicName, sender);
            return Senders[queueOrTopicName];
        }
        
        public void SendMessage(string queueOrTopicName, ServiceBusMessage message)
        {
            if (ServiceBusClient == null)
                throw new Exception("Null reference -- ServiceBusClient is null");
            if (Senders == null)
                throw new Exception("Null reference -- ServiceBusSenders are null");
            
            if (!Senders.ContainsKey(queueOrTopicName))
                throw new Exception("No sender -- Cannot find sender by queue or topic name");

            Senders[queueOrTopicName].SendMessageAsync(message).GetAwaiter().GetResult();
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            GC.SuppressFinalize(this);
        }

        protected async ValueTask DisposeAsyncCore()
        {
            if (ServiceBusClient != null)
            {
                await ServiceBusClient.DisposeAsync().ConfigureAwait(false);
                ServiceBusClient = null;
            }
            if (Senders != null)
            {
                foreach (ServiceBusSender? serviceBusSender in Senders.Values)
                {
                    if (serviceBusSender != null)
                    {
                        await serviceBusSender.DisposeAsync().ConfigureAwait(false);
                    }
                }
                Senders = null;
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
                if (clients[serviceBusNameSpace].ServiceBusClient != null)
                    return clients[serviceBusNameSpace].ServiceBusClient!;
                else
                {
                    clients[serviceBusNameSpace] = new ServiceBusSenderFactory(serviceBusConnectionString);
                    return clients[serviceBusNameSpace].ServiceBusClient!;
                }
            }
            else
            {
                clients.Add(serviceBusNameSpace, new ServiceBusSenderFactory(serviceBusConnectionString));
                return clients[serviceBusNameSpace].ServiceBusClient!;
            }
        }

        public ServiceBusSender CreateServiceBusSender(string serviceBusNameSpace, string queueOrTopicName)
        {
            if (clients == null || !clients.ContainsKey(serviceBusNameSpace))
                throw new Exception("Cannot find the Service Bus Client by the key!");
            return clients[serviceBusNameSpace].CreateServiceBusSender(queueOrTopicName);  
        }

        public void SendMessage(string serviceBusNameSpace, string queueOrTopicName, ServiceBusMessage message)
        {
            if (clients == null || !clients.ContainsKey(serviceBusNameSpace))
                throw new Exception("Cannot find the Service Bus Client by the key!");
            clients[serviceBusNameSpace].SendMessage(queueOrTopicName, message);
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
