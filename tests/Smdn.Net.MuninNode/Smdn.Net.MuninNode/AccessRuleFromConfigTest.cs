using System.Net;

using Microsoft.Extensions.Configuration;
using Smdn.Net.MuninNode.AccessRules;

namespace Smdn.Net.MuninNode;

[TestFixture]
public class AccessRuleFromConfigTest {
  
  [TestCase("127.0.0.1, 1.2.3.4, 5.6.7.8, 10.10.10.10", "127.0.0.1:1233", true)]
  [TestCase("127.0.0.1, 1.2.3.4, 5.6.7.8, 10.10.10.10", "1.2.3.4:1233", true)]
  [TestCase("127.0.0.1, 1.2.3.4, 5.6.7.8, 10.10.10.10", "1.2.3.5:1233", false)]
  [TestCase("127.0.0.1, 1.2.3.4, 5.6.7.8, 10.10.10.10", "10.10.10.10:0", true)]
  [TestCase("127.0.0.1, 1.2.3.4, 5.6.7.8, 10.10.10.10", "5.6.7.8:9011", true)]
  [TestCase("127.0.0.1, 1.2.3.4, 5.6.7.8, 10.10.10.10", "0.0.0.0:9011", false)]
  [TestCase("127.0.0.1, 1.2.3.4, 5.6.7.8, 10.10.10.10", "0.0.0.1:9011", false)]
  [TestCase("127.0.0.1, 1.2.3.4, 5.6.7.8, 10.10.10.10", "0.0.1.0:9011", false)]
  [TestCase("127.0.0.1, 1.2.3.4, 5.6.7.8, 10.10.10.10", "0.1.1.0:9011", false)]
  [TestCase("127.0.0.1, 1.2.3.4, 5.6.7.8, 10.10.10.10", "1.1.1.0:9011", false)]
  public void Test(string allowFrom, string endpoint, bool result)
  {
    
    // Prepare

    var ipendpoint = IPEndPoint.Parse(endpoint);
    var appsettings = new Dictionary<string, string>
    {
      {"MuninNode:AllowFrom", allowFrom},
    };
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(appsettings)
      .Build();

    var config = new MuninConfiguration(configuration);
    var access = new AccessRuleFromConfig(config);
    
    // Act
    
    var acceptable = access.IsAcceptable(ipendpoint);
    
    // Assert

    Assert.That(acceptable, result ? Is.True : Is.False);
  }
}
