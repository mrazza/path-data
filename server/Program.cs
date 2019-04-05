namespace PathApi.Server
{
    using System;
    using System.Threading.Tasks;
    using System.Threading;
    using SimpleInjector;
    using System.Reflection;
    using PathApi.Server.GrpcApi;
    using Grpc.Core;
    using Serilog;
    using CommandLine;
    using System.Linq;
    using PathApi.Server.PathServices;

    class Program
    {
        private static void HandleUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs args)
        {
            Log.Logger.Here().Error(args.Exception, "Unobserved task exception caught!");
            args.SetObserved();
        }

        static void Main(string[] args)
        {
            // Setup Flags
            var parsedResult = Parser.Default.ParseArguments<Flags>(args);
            var flags = parsedResult as Parsed<Flags>;
            if (flags == null)
            {
                return;
            }

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
            container.RegisterSingleton<IPathSqlDbRepository, PathSqlDbRepository>();
            container.RegisterSingleton<RealtimeDataRepository>();
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
