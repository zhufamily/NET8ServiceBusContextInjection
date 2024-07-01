# Helper interfaces / classes for Service Bus context injection with .NET 8 Azure funcions with isolated mode
## Background
After we migrated our Azure functions from .NET 6 in-process mode to the latest .NET8 isolated mode, we noticed that chanllenges from output Service Bus messages.
1. Any SDK Service Bus message type is not supported, you cannot really return a ServiceBusMessage object or a collection of that.
2. Even though you can return a JSON object or a collection of JSON object, this poses several challenges.
   - Some properties on ServiceBusMessage itself, such as subject / label cannot be set somewhere else on JSON objects.
   - All messages will return all at once while a function is completed, you cannot call flush like in-process mode to send messages as needed.
## Approach
