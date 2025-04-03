using System.Net;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Smdn.Net.MuninNode.AccessRules;
using Smdn.Net.MuninNode.SocketCreate;

namespace Smdn.Net.MuninNode;

[TestFixture]
public class MuninNodeTest {
  
  [TestCase]
  public async Task Test()
  {
    
    // Prepare

    var services = new ServiceCollection();
    services.AddMunin(
      listen: "127.0.0.1",
      port: 14949,
      hostname: "localhost",
      allowFrom: "127.0.0.1"
    );

    var provider = services.BuildServiceProvider();
    var muninNode = provider.GetRequiredService<IMuninNode>();
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
    
    // Act

    await muninNode.RunAsync(throwIfCancellationRequested: false, cts.Token);
    
    // Assert

    Assert.Pass();
  }
}
