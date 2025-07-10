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

/// <summary>
/// Provides builder pattern for configuring and building the <c>Munin-Node</c>.
/// </summary>
/// <seealso cref="MuninNodeBuilderExtensions"/>
#pragma warning disable CS0618 // TODO: remove IMuninNodeBuilder
public class MuninNodeBuilder : IMuninNodeBuilder {
#pragma warning restore CS0618
  private readonly List<Func<IServiceProvider, IPlugin>> pluginFactories = new(capacity: 4);
  private Func<IServiceProvider, IPluginProvider>? buildPluginProvider;
  [Obsolete] private Func<IServiceProvider, INodeSessionCallback>? buildSessionCallback;
  private Func<IServiceProvider, IMuninNodeListenerFactory>? buildListenerFactory;

  /// <summary>
  /// Gets the <see cref="IServiceCollection"/> where the <c>Munin-Node</c> services are configured.
  /// </summary>
  public IServiceCollection Services { get; }

  /// <summary>
  /// Gets the <see cref="string"/> key of <c>Munin-Node</c> service.
  /// </summary>
  /// <remarks>
  /// The value set as the hostname of the <c>Munin-Node</c> (see <see cref="MuninNodeOptions.HostName"/>) is used as the service key.
  /// </remarks>
  /// <see cref="IMuninServiceBuilderExtensions.AddNode(IMuninServiceBuilder, Action{MuninNodeOptions})"/>
  public string ServiceKey { get; }

  protected internal MuninNodeBuilder(IMuninServiceBuilder serviceBuilder, string serviceKey)
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

  [Obsolete]
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

  /// <summary>
  /// Builds the <c>Munin-Node</c> with current configurations.
  /// </summary>
  /// <param name="serviceProvider">
  /// An <see cref="IServiceProvider"/> that provides the services to be used by the <see cref="IMuninNode"/> being built.
  /// </param>
  /// <returns>An initialized <see cref="IMuninNode"/>.</returns>
  public IMuninNode Build(IServiceProvider serviceProvider)
  {
    if (serviceProvider is null)
      throw new ArgumentNullException(nameof(serviceProvider));

    return Build(
      pluginProvider: buildPluginProvider is null
        ? new PluginProvider(
            plugins: pluginFactories.Select(factory => factory(serviceProvider)).ToList(),
#pragma warning disable CS0612
            sessionCallback: buildSessionCallback?.Invoke(serviceProvider)
#pragma warning restore CS0612
          )
        : buildPluginProvider.Invoke(serviceProvider),
      listenerFactory: buildListenerFactory?.Invoke(serviceProvider),
      serviceProvider: serviceProvider
    );
  }

  private sealed class PluginProvider : IPluginProvider {
    public IReadOnlyCollection<IPlugin> Plugins { get; }

    [Obsolete]
    public INodeSessionCallback? SessionCallback { get; }

    public PluginProvider(
      IReadOnlyCollection<IPlugin> plugins,
#pragma warning disable CS0618
      INodeSessionCallback? sessionCallback
#pragma warning restore CS0618
    )
    {
      Plugins = plugins ?? throw new ArgumentNullException(nameof(plugins));

#pragma warning disable CS0612
      SessionCallback = sessionCallback;
#pragma warning restore CS0612
    }
  }

  protected virtual IMuninNode Build(
    IPluginProvider pluginProvider,
    IMuninNodeListenerFactory? listenerFactory,
    IServiceProvider serviceProvider
  )
  {
    if (serviceProvider is null)
      throw new ArgumentNullException(nameof(serviceProvider));

    return new DefaultMuninNode(
      options: GetConfiguredOptions<MuninNodeOptions>(serviceProvider),
      pluginProvider: pluginProvider,
      listenerFactory: listenerFactory,
      logger: serviceProvider.GetService<ILoggerFactory>()?.CreateLogger<DefaultMuninNode>()
    );
  }

  protected TMuninNodeOptions GetConfiguredOptions<TMuninNodeOptions>(IServiceProvider serviceProvider)
    where TMuninNodeOptions : MuninNodeOptions
  {
    if (serviceProvider is null)
      throw new ArgumentNullException(nameof(serviceProvider));

    return serviceProvider
      .GetRequiredService<IOptionsMonitor<TMuninNodeOptions>>()
      .Get(name: ServiceKey);
  }
}
