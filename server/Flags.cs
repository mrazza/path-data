namespace PathApi.Server
{
    using System.Collections.Generic;
    using CommandLine;

    /// <summary>
    /// Class representing the commandline flags supported by this binary.
    /// </summary>
    internal class Flags
    {
        #region gRPC Flags
        [Option("server_host", Default = "0.0.0.0", HelpText = "The host (IP address) to bind the gRPC server to.")]
        public string ServerHost { get; set; }

        [Option("server_port", Required = true, HelpText = "The port to bind the gRPC server to.")]
        public int ServerPort { get; set; }
        #endregion

        #region PATH API Flags
        [Option("path_db_update_url", Default = "https://path-mppprod-app.azurewebsites.net/api/v1/Config/Fetch", HelpText = "Format URL for the PATH API database update check. The first argument ({0}) will be replaced by the checksum.")]
        public string PathCheckDbUpdateUrl { get; set; }

        [Option("path_initial_checksum", Default = "3672A87A4D8E9104E736C3F61023F013", HelpText = "The initial checksum to use when calling the PATH database update API.")]
        public string InitialPathDbChecksum { get; set; }

        [Option("path_db_download_url", Default = "https://path-mppprod-app.azurewebsites.net/api/v1/file/clientdb?checksum={0}", HelpText = "Format URL used to download the latest PATH SQLite database. THe first argument ({0} will be the checksum of the database.")]
        public string PathDbDownloadUrl { get; set; }

        [Option("path_api_key", Default = "3CE6A27D-6A58-4CA5-A3ED-CE2EBAEFA166", HelpText = "API key to pass to the PATH HTTP API.")]
        public string PathApiKey { get; set; }

        [Option("path_app_name", Default = "RidePATH", HelpText = "Name of the PATH App for use in HTTP calls.")]
        public string PathAppName { get; set; }

        [Option("path_app_version", Default = "4.3.0", HelpText = "Version of the PATH App for use in HTTP calls.")]
        public string PathAppVersion { get; set; }

        [Option("path_user_agent", Default = "okhttp/3.12.6", HelpText = "User agent for use in HTTP calls.")]
        public string PathUserAgent { get; set; }
        #endregion

        #region PATH SQL Flags
        [Option("service_bus_configuration_key_name", Default = "rt_ServiceBusEndpoint_Prod", HelpText = "The name of the key in the tblConfigurationData table to read for the Azure Service Bus key.")]
        public string ServiceBusConfigurationKeyName { get; set; }

        [Option("token_broker_url_key_name", Default = "rt_TokenBrokerUrl_Prod", HelpText = "The name of the key in the tblConfigurationData table to read for the JWT token broker URL.")]
        public string TokenBrokerUrlKeyName { get; set; }

        [Option("token_value_key_name", Default = "rt_TokenValue_Prod", HelpText = "The name of the key in the tblConfigurationData table to read for the token broker authorization header value.")]
        public string TokenValueKeyName { get; set; }

        [Option("sql_update_check_frequency_secs", Default = 3600, HelpText = "The delay, in seconds, between checks for a new SQL database.")]
        public int SqlUpdateCheckFrequencySecs { get; set; }
        #endregion

        #region PATH Service Bus Flags
        [Option("service_bus_subscription_id", HelpText = "Optional service bus subscription ID, if left unspecified one is generated automatically.")]
        public string ServiceBusSubscriptionId { get; set; }

        [Option("special_headsign_mapping", HelpText = "Optional mappings from one headsign to another (of format 'Exchange Place=World Trade Center').")]
        public IEnumerable<string> SpecialHeadsignMappings { get; set; }
        #endregion
    }
}