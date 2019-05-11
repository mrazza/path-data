namespace PathApi.Server
{
    using CommandLine;
    using PathApi.Server.GrpcApi;
    using PathApi.Server.PathServices;
    using Serilog;
    using SimpleInjector;
    using System.Reflection;
    using System.Threading.Tasks;

    class Program
    {
        private static void HandleUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs args)
        {
            Log.Logger.Here().Error(args.Exception, "Unobserved task exception caught!");
            args.SetObserved();
        }

        static void Main(string[] args)
        {
            if (Parser.Default.ParseArguments<Flags>(args) is Parsed<Flags> flags)
            {
                // Setup Logging
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .Enrich.FromLogContext()
                    .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] ({FilePath}.{MemberName}:{LineNumber}) {Message}{NewLine}{Exception}")
                    .CreateLogger();
                TaskScheduler.UnobservedTaskException += HandleUnobservedTaskException;

                // Setup Injector
                Container container = new Container();
                container.RegisterSingleton<PathApiClient>();
                container.RegisterSingleton<PathSqlDbRepository>();
                container.RegisterSingleton<IPathApiClient, PathApiClient>();
                container.RegisterSingleton<IPathDataRepository, PathSqlDbRepository>();
                container.RegisterSingleton<IRealtimeDataRepository, ServiceBusRealtimeDataRepository>();
                container.Collection.Register<IGrpcApi>(Assembly.GetExecutingAssembly());
                container.Collection.Register<IStartupTask>(Assembly.GetExecutingAssembly());
                container.RegisterInstance<Flags>(flags.Value);
                container.Verify();

                // Execute startup tasks
                container.GetInstance<StartupTaskExecutor>().ExecuteTasks().Wait();

                // Start the server and run forever.
                container.GetInstance<GrpcServer>().Run().Wait();
            }
        }
    }
}
