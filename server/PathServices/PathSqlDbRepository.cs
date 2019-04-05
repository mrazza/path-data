namespace PathApi.Server.PathServices
{
    using System.Threading;
    using System.Threading.Tasks;
    using Serilog;
    using System.Data.SQLite;
    using System.IO;
    using System.Data;
    using System;
    using System.IO.Compression;
    using Nito.AsyncEx;

    internal sealed class PathSqlDbRepository : IPathSqlDbRepository, IStartupTask
    {
        private readonly Flags flags;
        private readonly PathApiClient pathApiClient;
        private string latestChecksum;
        private SQLiteConnection sqliteConnection;
        private Timer updateTimer;
        private AsyncReaderWriterLock readerWriterLock;
        private readonly TimeSpan refreshTimeSpan;

        public event EventHandler<EventArgs> OnDatabaseUpdate;

        public PathSqlDbRepository(Flags flags, PathApiClient pathApiClient)
        {
            this.flags = flags;
            this.pathApiClient = pathApiClient;
            this.latestChecksum = flags.InitialPathDbChecksum;
            this.sqliteConnection = null;
            this.readerWriterLock = new AsyncReaderWriterLock();
            this.refreshTimeSpan = new TimeSpan(0, 0, this.flags.SqlUpdateCheckFrequencySecs);
        }

        public async Task OnStartup()
        {
            Log.Logger.Here().Information("Preparing PATH SQL repository...");
            using (await this.readerWriterLock.WriterLockAsync())
            {
                this.latestChecksum = await this.pathApiClient.GetLatestChecksum(this.latestChecksum);
                await this.DownloadDatabase();
            }
            this.InvokeUpdateEvent();

            this.updateTimer = new Timer(this.UpdateEvent, null, (int)this.refreshTimeSpan.TotalMilliseconds, (int)this.refreshTimeSpan.TotalMilliseconds);
        }

        public async Task<string> GetServiceBusKey()
        {
            using (await this.readerWriterLock.ReaderLockAsync())
            {
                this.AssertConnected();
                SQLiteCommand command = this.sqliteConnection.CreateCommand();
                command.CommandText = $"SELECT configuration_value FROM tblConfigurationData WHERE configuration_key = @key;";
                command.Parameters.Add(new SQLiteParameter("@key", this.flags.ServiceBusConfigurationKeyName));
                return (string)await command.ExecuteScalarAsync();
            }
        }

        private async void UpdateEvent(object ignored)
        {
            Log.Logger.Here().Information("Checking for a PATH SQLite DB update...");
            var newChecksum = await this.pathApiClient.GetLatestChecksum(this.latestChecksum);

            if (this.latestChecksum != newChecksum)
            {
                bool updateNeeded = false;
                using (await this.readerWriterLock.WriterLockAsync())
                {
                    if (this.latestChecksum != newChecksum)
                    {
                        Log.Logger.Here().Information("PATH SQLite DB update needed.");
                        updateNeeded = true;
                        this.sqliteConnection.Close();
                        this.sqliteConnection.Dispose();
                        await this.DownloadDatabase();
                    }
                }

                if (updateNeeded)
                {
                    this.InvokeUpdateEvent();
                }
            }
        }

        private void InvokeUpdateEvent()
        {
            var updateEvent = this.OnDatabaseUpdate;
            if (updateEvent != null)
            {
                updateEvent.Invoke(this, new EventArgs());
            }
        }

        private async Task DownloadDatabase()
        {
            using (var databaseStream = await this.pathApiClient.GetDatabaseAsStream(this.latestChecksum))
            {
                using (var archive = new ZipArchive(databaseStream))
                {
                    using (var decompressedDatabase = archive.Entries[0].Open())
                    {
                        using (var databaseOut = new FileStream(this.GetSqliteFilename(this.latestChecksum), FileMode.Create))
                        {
                            await decompressedDatabase.CopyToAsync(databaseOut);
                        }
                    }
                }
            }
            this.sqliteConnection = new SQLiteConnection($"Data Source={this.GetSqliteFilename(this.latestChecksum)};", true);
            this.sqliteConnection.Open();
            this.AssertConnected();
            Log.Logger.Here().Information($"Database downloaded, connection established to {this.sqliteConnection.DataSource}.");
        }

        private string GetSqliteFilename(string checksum)
        {
            return $"{checksum}.db";
        }

        private void AssertConnected()
        {
            if (sqliteConnection == null || sqliteConnection.State != ConnectionState.Open)
            {
                throw new InvalidOperationException($"PATH SQL Database is not connected ({sqliteConnection?.State}). Are you making queries before startup has completed?");
            }
        }
    }
}