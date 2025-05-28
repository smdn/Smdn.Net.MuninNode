// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Smdn.Net.MuninNode.Transport;
using Smdn.Net.MuninPlugin;

namespace Smdn.Net.MuninNode.DependencyInjection;

public class MuninNodeBuilder : IMuninNodeBuilder {
  private readonly List<Func<IServiceProvider, IPlugin>> pluginFactories = new(capacity: 4);
  private Func<IServiceProvider, IPluginProvider>? buildPluginProvider;
  private Func<IServiceProvider, INodeSessionCallback>? buildSessionCallback;
  private Func<IServiceProvider, IMuninNodeListenerFactory>? buildListenerFactory;

  public IServiceCollection Services { get; }
  public string ServiceKey { get; }

  internal MuninNodeBuilder(IMuninServiceBuilder serviceBuilder, string serviceKey)
  {
    Services = (serviceBuilder ?? throw new ArgumentNullException(nameof(serviceBuilder))).Services;
    ServiceKey = serviceKey ?? throw new ArgumentNullException(nameof(serviceKey));
  }

  internal void AddPluginFactory(Func<IServiceProvider, IPlugin> buildPlugin)
  {
    if (buildPlugin is null)
      throw new ArgumentNullException(nameof(buildPlugin));

    pluginFactories.Add(serviceProvider => buildPlugin(serviceProvider));
  }

  internal void SetPluginProviderFactory(
    Func<IServiceProvider, IPluginProvider> buildPluginProvider
  )
  {
    if (buildPluginProvider is null)
      throw new ArgumentNullException(nameof(buildPluginProvider));

    this.buildPluginProvider = buildPluginProvider;
  }

  internal void SetSessionCallbackFactory(
    Func<IServiceProvider, INodeSessionCallback> buildSessionCallback
  )
  {
    if (buildSessionCallback is null)
      throw new ArgumentNullException(nameof(buildSessionCallback));

    this.buildSessionCallback = buildSessionCallback;
  }

  internal void SetListenerFactory(
    Func<IServiceProvider, IMuninNodeListenerFactory> buildListenerFactory
  )
  {
    if (buildListenerFactory is null)
      throw new ArgumentNullException(nameof(buildListenerFactory));

    this.buildListenerFactory = buildListenerFactory;
  }

  public IMuninNode Build(IServiceProvider serviceProvider)
  {
    if (serviceProvider is null)
      throw new ArgumentNullException(nameof(serviceProvider));

    return new DefaultMuninNode(
      options: serviceProvider.GetRequiredService<IOptionsMonitor<MuninNodeOptions>>().Get(name: ServiceKey),
      pluginProvider: buildPluginProvider is null
        ? new PluginProvider(
            plugins: pluginFactories.Select(factory => factory(serviceProvider)).ToList(),
            sessionCallback: buildSessionCallback?.Invoke(serviceProvider)
          )
        : buildPluginProvider.Invoke(serviceProvider),
      listenerFactory: buildListenerFactory?.Invoke(serviceProvider),
      logger: serviceProvider.GetService<ILoggerFactory>()?.CreateLogger<DefaultMuninNode>()
    );
  }

  private sealed class PluginProvider : IPluginProvider {
    public IReadOnlyCollection<IPlugin> Plugins { get; }
    public INodeSessionCallback? SessionCallback { get; }

    public PluginProvider(
      IReadOnlyCollection<IPlugin> plugins,
      INodeSessionCallback? sessionCallback
    )
    {
      Plugins = plugins ?? throw new ArgumentNullException(nameof(plugins));
      SessionCallback = sessionCallback;
    }
  }
}
