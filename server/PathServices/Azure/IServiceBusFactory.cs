namespace PathApi.Server.PathServices.Azure
{
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Management;

    /// <summary>
    /// Factory used to create Azure ServiceBus clients.
    /// </summary>
    internal interface IServiceBusFactory
    {
        /// <summary>
        /// Creates an Azure ServiceBus ManagementClient with the given connection string.
        /// </summary>
        ManagementClient CreateManagementClient(string connectionString);

        /// <summary>
        /// Creates an Azure ServiceBus SubscriptionClient.
        /// </summary>
        ISubscriptionClient CreateSubscriptionClient(string connectionString, string topicPath, string subscriptionName);
    }
}