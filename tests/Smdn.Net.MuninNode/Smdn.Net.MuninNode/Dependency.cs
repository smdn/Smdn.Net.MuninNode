using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Smdn.Net.MuninNode.AccessRules;
using Smdn.Net.MuninNode.SocketCreate;
using Smdn.Net.MuninPlugin;

namespace Smdn.Net.MuninNode;

public static class Dependency {
  public static IServiceCollection AddMunin(
    this IServiceCollection services, 
    string listen,
    int port,
    string hostname,
    string allowFrom)
  {
    var settings = new Dictionary<string, string>
    {
      {"MuninNode:Listen", $"{listen}"},
      {"MuninNode:Port", $"{port}"},
      {"MuninNode:Hostname", $"{hostname}"},
      {"MuninNode:AllowFrom", $"{allowFrom}"},
    };

    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(settings)
      .Build();

    services
      .AddLogging()
      .AddScoped<IMuninNode, MuninNode>()
      .AddScoped<ISocketCreator, SocketCreator>()
      .AddScoped<IPluginProvider, EmptyPluginProvider>()
      .AddScoped<IAccessRule, AccessRuleFromConfig>()
      .AddScoped<MuninConfiguration>()
      ;

    return services;
  }
}

public class EmptyPluginProvider : IPluginProvider {
  public IReadOnlyCollection<IPlugin> Plugins { get; } = [];
  public INodeSessionCallback? SessionCallback { get; }
}
