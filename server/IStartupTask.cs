namespace PathApi.Server
{
    using System.Threading.Tasks;

    /// <summary>
    /// Interface that represents a task to be executed at app startup.
    /// </summary>
    /// <seealso cref="StartupTaskExecutor"/>
    internal interface IStartupTask
    {
        /// <summary>
        /// Called when the server is starting up. Implement this to do arbitrary work.
        /// </summary>
        /// <returns>A task that completes when the startup work has finished.</returns>
        Task OnStartup();
    }
}