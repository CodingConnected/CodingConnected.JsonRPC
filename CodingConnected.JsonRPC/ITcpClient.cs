using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace CodingConnected.JsonRPC
{
    /// <summary>
    /// The ITcpClient interface is meant to ease exchanging data from within the JsonRpcClient class.
    /// One can easily write a small wrapper around a .NET TcpClient that sends the data when 
    /// SendDataAsync is called.
    /// </summary>
    public interface ITcpClient
    {
        Task SendDataAsync(string data, CancellationToken token);
    }
}
