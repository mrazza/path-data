namespace PathApi.Server
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Linq;
    using Serilog;

    internal sealed class StartupTaskExecutor
    {
        private readonly IEnumerable<IStartupTask> startupTasks;

        public StartupTaskExecutor(IEnumerable<IStartupTask> startupTasks)
        {
            this.startupTasks = startupTasks;
        }

        public async Task ExecuteTasks()
        {
            Log.Logger.Here().Information("Executing startup tasks...");
            await Task.WhenAll(this.startupTasks.Select((task) => task.OnStartup()));
            Log.Logger.Here().Information("Startup tasks complete.");
        }
    }
}