using System.Net;

using Microsoft.Extensions.Configuration;

namespace Smdn.Net.MuninNode;

[TestFixture]
public class MuninConfigurationTest {
  
  [Test]
  public void TestDefaultValues()
  {
    
    // Prepare
    
    // Act
    
    var config = new MuninConfiguration();
    
    // Assert

    Assert.That(config.Port, Is.EqualTo(4949) );
    Assert.That(config.Listen, Is.EqualTo(IPAddress.Loopback));
    Assert.That(config.Hostname, Is.EqualTo("localhost"));
    Assert.That(config.AllowFrom.Count, Is.EqualTo(2));
  }
  
  [Test]
  public void TestValues()
  {
    
    // Prepare
    // Act

    var config = new MuninConfiguration {
      Hostname = "hostname",
      Port = 1234,
      Listen = IPAddress.Any,
      AllowFrom = [IPAddress.Any]
    };
    
    // Assert

    Assert.That(config.Port, Is.EqualTo(1234) );
    Assert.That(config.Listen, Is.EqualTo(IPAddress.Any));
    Assert.That(config.Hostname, Is.EqualTo("hostname"));
    Assert.That(config.AllowFrom.Count, Is.EqualTo(1));
  }
  
  [Test]
  public void TestFromConfiguration()
  {
    
    // Prepare

    var appsettings = new Dictionary<string, string>
    {
      {"MuninNode:Port", "9876"},
      {"MuninNode:Hostname", "appsettings"},
      {"MuninNode:Listen", "::1"},
      {"MuninNode:AllowFrom", "127.0.0.1, 1.2.3.4, 5.6.7.8, 10.10.10.10"},
    };

    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(appsettings)
      .Build();

    // Act

    var config = new MuninConfiguration(configuration);
    
    // Assert

    Assert.That(config.Port, Is.EqualTo(9876));
    Assert.That(config.Listen, Is.EqualTo(IPAddress.Parse("::1")));
    Assert.That(config.Hostname, Is.EqualTo("appsettings"));
    Assert.That(config.AllowFrom.Count, Is.EqualTo(4));
  }
}
