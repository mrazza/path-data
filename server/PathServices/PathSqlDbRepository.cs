namespace PathApi.Server.PathServices
{
    using Nito.AsyncEx;
    using PathApi.Server.PathServices.Models;
    using PathApi.V1;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SQLite;
    using System.Globalization;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A self-updating repository that provides access to the latest version of the PATH SQLite database.
    /// </summary>
    internal sealed class PathSqlDbRepository : IPathDataRepository, IStartupTask, IDisposable
    {
        private readonly Flags flags;
        private readonly IPathApiClient pathApiClient;
        private string latestChecksum;
        private SQLiteConnection sqliteConnection;
        private Timer updateTimer;
        private readonly AsyncReaderWriterLock readerWriterLock;
        private readonly TimeSpan refreshTimeSpan;
        private readonly Dictionary<string, string> specialHeadsignMappings;

        /// <summary>
        /// An event that is triggered when the PATH SQLite database is downloaded or updated.
        /// </summary>
        public event EventHandler<EventArgs> OnDataUpdate;

        /// <summary>
        /// Constructs a new instance of the <see cref="PathSqlDbRepository"/>.
        /// </summary>
        /// <param name="flags">The <see cref="Flags"/> instance containing the app configuration.</param>
        /// <param name="pathApiClient">The <see cref="PathApiClient"/> to use when retrieving the latest SQLite database.</param>
        public PathSqlDbRepository(Flags flags, IPathApiClient pathApiClient)
        {
            this.flags = flags;
            this.pathApiClient = pathApiClient;
            this.latestChecksum = flags.InitialPathDbChecksum;
            this.sqliteConnection = null;
            this.readerWriterLock = new AsyncReaderWriterLock();
            this.refreshTimeSpan = new TimeSpan(0, 0, this.flags.SqlUpdateCheckFrequencySecs);
            this.specialHeadsignMappings = new Dictionary<string, string>();

            foreach (var mapping in flags.SpecialHeadsignMappings)
            {
                string[] parts = mapping.Split('=');
                if (parts.Length != 2)
                {
                    throw new ArgumentException("Malformed special headsign mapping.");
                }
                this.specialHeadsignMappings.Add(parts[0], parts[1]);
            }
        }

        /// <summary>
        /// On app start, gets the latest PATH database checksum and downloads the SQLite database.
        /// </summary>
        /// <returns>A task which wait for the PATH database to be loaded.</returns>
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

        /// <inheritdoc />
        public Task<string> GetServiceBusKey() => this.GetConfigurationValue(this.flags.ServiceBusConfigurationKeyName);

        /// <inheritdoc />
        public Task<string> GetTokenBrokerUrl() => this.GetConfigurationValue(this.flags.TokenBrokerUrlKeyName);

        /// <inheritdoc />
        public Task<string> GetTokenValue() => this.GetConfigurationValue(this.flags.TokenValueKeyName);

        /// <summary>
        /// Gets information about the specified station.
        /// </summary>
        /// <returns>A task returning the station information for the specified station.</returns>
        public async Task<List<Stop>> GetStops(Station station)
        {
            if (station == Station.Unspecified)
            {
                throw new ArgumentException("Station must be specified.", nameof(station));
            }

            using (await this.readerWriterLock.ReaderLockAsync())
            {
                this.AssertConnected();
                SQLiteCommand command = this.sqliteConnection.CreateCommand();
                command.CommandText = "SELECT stop_id, stop_name, stop_lat, stop_lon, location_type, parent_station, stop_timezone FROM tblStops WHERE stop_id = @stop_id OR parent_station = @stop_id;";
                command.Parameters.AddWithValue("@stop_id", StationMappings.StationToDatabaseId[station]);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    List<Stop> stops = new List<Stop>();
                    while (await reader.ReadAsync())
                    {
                        stops.Add(new Stop()
                        {
                            Id = reader.GetString(0),
                            Name = reader.GetString(1),
                            Latitude = double.Parse(reader.GetString(2)),
                            Longitude = double.Parse(reader.GetString(3)),
                            LocationType = (LocationType)int.Parse(reader.GetString(4)),
                            ParentStopId = reader.GetString(5),
                            Timezone = reader.GetString(6)
                        });
                    }

                    if (stops.Count == 0)
                    {
                        throw new KeyNotFoundException("Could not find requested station in the PATH data.");
                    }

                    return stops;
                }
            }
        }

        /// <summary>
        /// Gets all the routes (specifically RouteLines).
        /// </summary>
        /// <returns>A task returning a collection of RouteLines.</returns>
        public async Task<List<RouteLine>> GetRoutes()
        {
            using (await this.readerWriterLock.ReaderLockAsync())
            {
                this.AssertConnected();
                SQLiteCommand command = this.sqliteConnection.CreateCommand();
                command.CommandText =
                    "SELECT r.route_id, r.route_long_name, rl.route_display_name,  t.trip_headsign, r.route_color, rl.direction " +
                    "FROM tblRoutes r " +
                    "JOIN tblRouteLine rl ON r.route_id = rl.route_id " +
                    "INNER JOIN tblTrips t ON t.route_id = r.route_id AND t.direction_id = rl.direction " +
                    "WHERE r.agency_id = 151 " +
                    "GROUP BY 1, 2, 3, 4, 5, 6 " +
                    "ORDER BY r.route_id, rl.direction;";

                List<RouteLine> routes = new List<RouteLine>();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        try
                        {
                            routes.Add(new RouteLine()
                            {
                                Route = RouteMappings.DatabaseIdToRoute[reader.GetString(0)],
                                Id = reader.GetString(0),
                                LongName = reader.GetString(1),
                                DisplayName = reader.GetString(2),
                                Headsign = reader.GetString(3),
                                Color = reader.GetString(4),
                                Direction = (RouteDirection)int.Parse(reader.GetString(5))
                            });
                        }
                        catch (Exception e)
                        {
                            Log.Logger.Here().Warning(e, "Unexpecting error when building RouteLine.");
                        }
                    }
                    return routes;
                }
            }
        }

        /// <summary>
        /// Gets a route from the specified headsign name and color pair.
        /// </summary>
        /// <returns>A task returning the route for the specified train.</returns>
        public async Task<RouteLine> GetRouteFromTrainHeadsign(string headsignName, IEnumerable<string> headsignColors)
        {
            if (string.IsNullOrWhiteSpace(headsignName))
            {
                throw new ArgumentException("Headsign must be specified.", nameof(headsignName));
            }
            else if (headsignColors == null || headsignColors.Count() <= 0)
            {
                throw new ArgumentException("At least one headsign color must be specified.", nameof(headsignColors));
            }

            // Input colors are likely to be prefixed with a #. They are not in the SQL database.
            headsignColors = headsignColors.Select((color) => color.Trim('#'));
            headsignName = this.NormalizeHeadsign(headsignName);

            using (await this.readerWriterLock.ReaderLockAsync())
            {
                this.AssertConnected();
                SQLiteCommand command = this.sqliteConnection.CreateCommand();
                int colorIndex = 0;
                string colorSql = "";
                foreach (var color in headsignColors)
                {
                    if (colorSql != string.Empty)
                    {
                        colorSql += ", ";
                    }

                    colorSql += $"@color{colorIndex}";
                    command.Parameters.AddWithValue($"@color{colorIndex}", color.ToLowerInvariant());
                    colorIndex++;
                }
                command.CommandText =
                    "SELECT route_id, route_long_name, route_display_name, trip_headsign, route_color, direction_id " +
                    "FROM Schedule " +
                    $"WHERE LOWER(trip_headsign) = @headsign AND LOWER(route_color) IN ({colorSql}) " +
                    "LIMIT 1;";
                command.Parameters.AddWithValue("@headsign", headsignName.ToLowerInvariant());
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new RouteLine()
                        {
                            Route = RouteMappings.DatabaseIdToRoute[reader.GetString(0)],
                            Id = reader.GetString(0),
                            LongName = reader.GetString(1),
                            DisplayName = reader.GetString(2),
                            Headsign = reader.GetString(3),
                            Color = reader.GetString(4),
                            Direction = (RouteDirection)int.Parse(reader.GetString(5))
                        };
                    }
                    else
                    {
                        throw new KeyNotFoundException($"Could not find route with headsign={headsignName} and color={string.Join(',', headsignColors)}.");
                    }
                }
            }
        }

        private string NormalizeHeadsign(string headsign)
        {
            if (headsign.Contains("via", StringComparison.InvariantCultureIgnoreCase))
            {
                headsign = headsign.Replace("Street", "", true, CultureInfo.InvariantCulture).Replace("  ", " ");
            }
            headsign = this.specialHeadsignMappings.GetValueOrDefault(headsign, headsign);
            return headsign;
        }

        private async void UpdateEvent(object ignored)
        {
            Log.Logger.Here().Information("Checking for a PATH SQLite DB update...");
            var previousChecksum = this.latestChecksum;
            bool updateNeeded = false;

            try
            {
                var newChecksum = await this.pathApiClient.GetLatestChecksum(this.latestChecksum);

                if (this.latestChecksum != newChecksum)
                {
                    using (await this.readerWriterLock.WriterLockAsync())
                    {
                        if (this.latestChecksum != newChecksum)
                        {
                            Log.Logger.Here().Information("PATH SQLite DB update needed.");
                            updateNeeded = true;
                            this.latestChecksum = newChecksum;
                            this.sqliteConnection.Close();
                            this.sqliteConnection.Dispose();
                            await this.DownloadDatabase();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Restore the checksum due to an error.
                this.latestChecksum = previousChecksum;
                updateNeeded = false;
                Log.Logger.Here().Error(ex, "Exception when checking for a PATH SQLite DB update.");
            }

            if (updateNeeded)
            {
                this.InvokeUpdateEvent();
            }
        }

        private void InvokeUpdateEvent()
        {
            var updateEvent = this.OnDataUpdate;
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
            this.sqliteConnection = new SQLiteConnection($"Data Source={this.GetSqliteFilename(this.latestChecksum)};Read Only=True", true);
            this.sqliteConnection.Open();
            this.AssertConnected();
            Log.Logger.Here().Information("Database downloaded, connection established to {dataSource}.", this.sqliteConnection.DataSource);
        }

        private string GetSqliteFilename(string checksum)
        {
            return $"{checksum}.db";
        }

        private void AssertConnected()
        {
            if (this.sqliteConnection == null || this.sqliteConnection.State != ConnectionState.Open)
            {
                throw new InvalidOperationException($"PATH SQL Database is not connected ({sqliteConnection?.State}). Are you making queries before startup has completed?");
            }
        }

        private async Task<string> GetConfigurationValue(string key)
        {
            using (await this.readerWriterLock.ReaderLockAsync())
            {
                this.AssertConnected();
                SQLiteCommand command = this.sqliteConnection.CreateCommand();
                command.CommandText = "SELECT configuration_value FROM tblConfigurationData WHERE configuration_key = @key;";
                command.Parameters.AddWithValue("@key", key);
                return (string)await command.ExecuteScalarAsync();
            }
        }

        public void Dispose()
        {
            this.updateTimer.Dispose();
            this.sqliteConnection.Dispose();
        }
    }
}