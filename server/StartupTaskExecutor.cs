namespace PathApi.Server
{
    using Serilog;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Asynchronously executes a collection of <see cref="IStartupTask"/>s.
    /// </summary>
    /// <remarks>
    /// <see cref="ExecuteTasks"/> is to be called (and awaited) at app startup before any
    /// <see cref="GrpcServer"/>s are started.
    /// 
    /// Note that all startup tasks are executed asynchronously so there are no guarantees about
    /// execution order.
    /// </remarks>
    /// <seealso cref="IStartupTask"/>
    internal sealed class StartupTaskExecutor
    {
        private readonly IEnumerable<IStartupTask> startupTasks;

        /// <summary>
        /// Constructs a new instance of <see cref="StartupTaskExecutor"/>.
        /// </summary>
        /// <param name="startupTasks">The collection of <see cref="IStartupTask"/>s to execute.</param>
        public StartupTaskExecutor(IEnumerable<IStartupTask> startupTasks)
        {
            this.startupTasks = startupTasks;
        }

        /// <summary>
        /// Executes the startup tasks tracked by this instance.
        /// </summary>
        /// <returns>A task that completes when all startup tasks have finished.</returns>
        public async Task ExecuteTasks()
        {
            Log.Logger.Here().Information("Executing startup tasks...");
            await Task.WhenAll(this.startupTasks.Select((task) => task.OnStartup()));
            Log.Logger.Here().Information("Startup tasks complete.");
        }
    }
}