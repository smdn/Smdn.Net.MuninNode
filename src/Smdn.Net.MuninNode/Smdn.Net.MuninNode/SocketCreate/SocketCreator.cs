using System.Net;
using System.Net.Sockets;

namespace Smdn.Net.MuninNode.SocketCreate;

public sealed class SocketCreator(MuninConfiguration config) : ISocketCreator {
  IPEndPoint GetLocalEndPointToBind()
    => new(
      address: config.Listen,
      port: config.Port
    );

  public Socket CreateServerSocket()
  {
    const int MaxClients = 1;
    Socket? server = null;

    try {
      var endPoint = GetLocalEndPointToBind();

      server = new Socket(
        endPoint.AddressFamily,
        SocketType.Stream,
        ProtocolType.Tcp
      );

      if (endPoint.AddressFamily == AddressFamily.InterNetworkV6 && Socket.OSSupportsIPv4)
        server.DualMode = true;

      server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
      server.Bind(endPoint);
      server.Listen(MaxClients);

      return server;
    }
    catch {
      server?.Dispose();
      throw;
    }
  }
}
