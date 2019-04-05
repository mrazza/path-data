namespace PathApi.Server.PathServices
{
    using System;
    using System.Threading.Tasks;

    internal interface IPathSqlDbRepository
    {
        event EventHandler<EventArgs> OnDatabaseUpdate;

        Task<string> GetServiceBusKey();
    }
}