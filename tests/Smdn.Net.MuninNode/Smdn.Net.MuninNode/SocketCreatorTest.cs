using System.Net;

using Microsoft.Extensions.Configuration;
using Smdn.Net.MuninNode.AccessRules;
using Smdn.Net.MuninNode.SocketCreate;

namespace Smdn.Net.MuninNode;

[TestFixture]
public class SocketCreatorTest {
  
  [TestCase("127.0.0.1", 14949)]
  [TestCase("0.0.0.0", 14945)]
  public void Test(string listen, int port)
  {
    
    // Prepare

    var appsettings = new Dictionary<string, string>
    {
      {"MuninNode:Listen", listen},
      {"MuninNode:Port", $"{port}"},
    };
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(appsettings)
      .Build();

    var config = new MuninConfiguration(configuration);
    ISocketCreator scoketCreator = new SocketCreator(config);
    
    // Act
    
    using var socket = scoketCreator.CreateServerSocket();
    
    // Assert

    Assert.That(socket.Connected, Is.False);
    Assert.That(socket.LocalEndPoint, Is.TypeOf<IPEndPoint>());
    Assert.That(((IPEndPoint)socket.LocalEndPoint).Port, Is.EqualTo(port));
    Assert.That(((IPEndPoint)socket.LocalEndPoint).Address.ToString(), Is.EqualTo(listen));
  }
}
