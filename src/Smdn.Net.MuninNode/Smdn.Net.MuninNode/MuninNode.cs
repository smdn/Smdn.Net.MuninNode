using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Smdn.Net.MuninNode.AccessRules;
using Smdn.Net.MuninNode.SocketCreate;
using Smdn.Net.MuninPlugin;

namespace Smdn.Net.MuninNode;

public sealed class MuninNode (
  ILogger<MuninNode> logger,
  IAccessRule accessRule,
  MuninConfiguration config,
  IPluginProvider plugins,
  ISocketCreator socketServer)
  : NodeBase(accessRule, logger), IMuninNode {
  private ISocketCreator SocketServer { get; set; } = socketServer;
  public override IPluginProvider PluginProvider { get; } = plugins;
  public override string HostName { get; } = config.Hostname;

  protected override Socket CreateServerSocket()
  {
    return SocketServer.CreateServerSocket();
  }

  public async Task RunAsync(bool throwIfCancellationRequested, CancellationToken stoppingToken)
  {
    Start();
    await AcceptAsync(throwIfCancellationRequested, stoppingToken);
  }
}
