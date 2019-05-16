namespace PathApi.Server.PathServices.Azure
{
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Management;

    /// <summary>
    /// Factory used to create Azure ServiceBus clients.
    /// </summary>
    internal sealed class ServiceBusFactory : IServiceBusFactory
    {
        /// <summary>
        /// Creates a Azure ServiceBus Management Client with the given connection string.
        /// </summary>
        public ManagementClient CreateManagementClient(string connectionString)
        {
            return new ManagementClient(connectionString);
        }

        /// <summary>
        /// Creates an Azure ServiceBus SubscriptionClient.
        /// </summary>
        public ISubscriptionClient CreateSubscriptionClient(string connectionString, string topicPath, string subscriptionName)
        {
            return new SubscriptionClient(connectionString, topicPath, subscriptionName);
        }
    }
}