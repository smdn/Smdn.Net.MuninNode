using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.MuninNode;

public interface IMuninNode {
  Task RunAsync(bool throwIfCancellationRequested, CancellationToken stoppingToken);
}
