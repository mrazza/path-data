using Microsoft.AspNetCore.SignalR.Client;
using Serilog;
using System;
using PathApi.Server.PathServices.Models;
using PathApi.V1;

namespace PathApi.Server.PathServices
{
    internal sealed class RetryPolicy : IRetryPolicy
    {
        private readonly Station station;
        private readonly RouteDirection direction;

        public RetryPolicy(Station station, RouteDirection direction)
        {
            this.station = station;
            this.direction = direction;
        }

        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            Log.Logger.Here().Warning("SignalR connection for S:{station} D:{direction} retrying because of {retryReason}, total retry count {previousRetryCount}", this.station, this.direction, retryContext.RetryReason, retryContext.PreviousRetryCount);
            return TimeSpan.FromSeconds(new Random().Next(1, 4) * Math.Min(retryContext.PreviousRetryCount + 1, 5));
        }
    }
}
