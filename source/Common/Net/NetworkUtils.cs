using System.Net;
using System.Net.Sockets;

namespace Common.Net;

public static class NetworkUtils
{
    /// <summary>
    /// Returns a random TCP port that is free although there is a race condition in the implementation.
    /// The method is mainly suitable for testing purposes and not production environments.
    /// </summary>
    public static int GetNextFreeTcpPort()
    {
        using Socket socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        // specifying value of 0 makes the OS to choose the next free port
        socket.Bind(new IPEndPoint(IPAddress.Loopback, port: 0));
        int port = ((IPEndPoint)socket.LocalEndPoint!).Port;

        return port;
    }
}
