using System.Net.Sockets;

namespace Smdn.Net.MuninNode.SocketCreate;

public interface ISocketCreator {
  Socket CreateServerSocket();
}
