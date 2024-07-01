# Helper interfaces / classes for Service Bus context injection with .NET 8 Azure funcions with isolated mode
## Background
After we migrated our Azure functions from .NET 6 in-process mode to the latest .NET8 isolated mode, we noticed that chanllenges from output Service Bus messages.
1. Any SDK Service Bus message type is not supported, you cannot really return a ServiceBusMessage object or a collection of that.
2. Even though you can return a JSON object or a collection of JSON object, this poses several challenges.
   - Some properties on ServiceBusMessage itself, such as subject / label cannot be set somewhere else on JSON objects.
   - All messages will return all at once while a function is completed, you cannot call flush like in-process mode to send messages as needed.
## Approach
After some research MS suggests context injection for output ServiceBus messages, but AFAIK cannot find any good sample codes or migration plans for how to map old .NET6 in-process output binding to new context injection.  After somne research, I come up with my own implementation of a ServiceBusSendFactory, if all of your messages go to the same Service Bus namesapce, which should account for most cases; meanwhile I also implement a ServiceBusFactory, if you do need send messages to different Service Bus namespaces.  I haven't yet tested performance gain while using context injection vs. initialize a ServiceBusClient and a ServiceBusSender everytime while sending messages.  Per Microsoft documentation, it is better to maintain one ServiceBusClient and one ServiceBusSender through your applications, therefore, it should be a better approach adopting context injection 

