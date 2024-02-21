using Microsoft.AspNetCore.SignalR.Client;
using Serilog;
using System;

namespace PathApi.Server.PathServices
{
    internal sealed class RetryPolicy : IRetryPolicy
    {
        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            Log.Logger.Here().Warning("SignalR connection retrying because of {retryReason}, total retry count {previousRetryCount}", retryContext.RetryReason, retryContext.PreviousRetryCount);
            return TimeSpan.FromSeconds(new Random().Next(1, 4) * Math.Min(retryContext.PreviousRetryCount + 1, 5));
        }
    }
}
