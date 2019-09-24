using System;
using System.Text;
using Grpc.Core;

namespace PathApi.Server.GrpcApi
{
    /// <summary>
    /// Collection of static methods to assist with pagination.
    /// </summary>
    internal static class PaginationHelper
    {
        /// <summary>
        /// Gets the encoded offset from the provided page token.
        /// </summary>
        public static int GetOffset(string pageToken)
        {
            if (string.IsNullOrWhiteSpace(pageToken))
            {
                return 0;
            }
            
            if (!int.TryParse(Encoding.UTF8.GetString(Convert.FromBase64String(pageToken)), out int offset))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Malformed page token."));
            }

            return offset;
        }

        /// <summary>
        /// Encodes the specified offset into a page token.
        /// </summary>
        public static string GetPageToken(int offset)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(offset.ToString()));
        }
    }
}