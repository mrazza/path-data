namespace PathApi.Server
{
    using System.Threading.Tasks;

    internal interface IStartupTask
    {
        Task OnStartup();
    }
}