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
        [Option("path_db_update_url", Default = "https://path-mppprod-app.azurewebsites.net/api/v1/checkdbupdate?appVersion=3.0.0&checkSum={0}", HelpText = "Format URL for the PATH API database update check. The first argument ({0}) will be replaced by the checksum.")]
        public string PathCheckDbUpdateUrl { get; set; }

        [Option("path_initial_checksum", Default = "C405CEFB9887554D47AD92334385E808", HelpText = "The initial checksum to use when calling the PATH database update API.")]
        public string InitialPathDbChecksum { get; set; }

        [Option("path_db_download_url", Default = "https://path-mppprod-app.azurewebsites.net/api/v1/file/clientdb?checksum={0}", HelpText = "Format URL used to download the latest PATH SQLite database. THe first argument ({0} will be the checksum of the database.")]
        public string PathDbDownloadUrl { get; set; }

        [Option("path_api_key", Default = "44F41F65-7E93-4075-9BAE-BAAE2DFC90F3", HelpText = "API key to pass to the PATH HTTP API.")]
        public string PathApiKey { get; set; }
        #endregion

        #region PATH SQL Flags
        [Option("service_bus_configuration_key_name", Default = "rt_ServiceBusEndpoint_Prod", HelpText = "The name of the key in the tblConfigurationData table to read for the Azure Service Bus key.")]
        public string ServiceBusConfigurationKeyName { get; set; }

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