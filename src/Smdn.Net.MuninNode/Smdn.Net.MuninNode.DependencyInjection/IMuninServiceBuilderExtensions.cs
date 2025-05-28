// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;

using Microsoft.Extensions.DependencyInjection;

namespace Smdn.Net.MuninNode.DependencyInjection;

public static class IMuninServiceBuilderExtensions {
  /// <summary>
  /// Adds a <c>Munin-Node</c> to the <see cref="IMuninServiceBuilder"/> with default configurations.
  /// </summary>
  /// <param name="builder">An <see cref="IMuninNodeBuilder"/> to build the <c>Munin-Node</c> to be added.</param>
  /// <returns>The current <see cref="IMuninNodeBuilder"/> so that additional calls can be chained.</returns>
  public static IMuninNodeBuilder AddNode(
    this IMuninServiceBuilder builder
  )
    => AddNode(
      builder: builder,
      configure: _ => { }
    );

  /// <summary>
  /// Adds a <c>Munin-Node</c> to the <see cref="IMuninServiceBuilder"/> with specified configurations.
  /// </summary>
  /// <param name="builder">
  /// An <see cref="IMuninNodeBuilder"/> to build the <c>Munin-Node</c> to be added.
  /// </param>
  /// <param name="configure">
  /// An <see cref="Action{MuninNodeOptions}"/> to setup <see cref="MuninNodeOptions"/> to
  /// configure the <c>Munin-Node</c> to be built.
  /// </param>
  /// <returns>The current <see cref="IMuninNodeBuilder"/> so that additional calls can be chained.</returns>
  public static IMuninNodeBuilder AddNode(
    this IMuninServiceBuilder builder,
    Action<MuninNodeOptions> configure
  )
  {
    if (builder is null)
      throw new ArgumentNullException(nameof(builder));
    if (configure is null)
      throw new ArgumentNullException(nameof(configure));

    var options = new MuninNodeOptions();

    configure(options);

    var nodeBuilder = new MuninNodeBuilder(
      serviceBuilder: builder,
      serviceKey: options.HostName // use configured hostname as a service key and option name
    );

    _ = builder.Services.Configure<MuninNodeOptions>(
      name: nodeBuilder.ServiceKey, // configure MuninNodeOptions for this builder
      opts => {
        opts.Address = options.Address;
        opts.Port = options.Port;
        opts.HostName = options.HostName;
        opts.AccessRule = options.AccessRule;
      }
    );

    builder.Services.Add(
      ServiceDescriptor.KeyedSingleton<IMuninNodeBuilder>(
        serviceKey: nodeBuilder.ServiceKey,
        implementationFactory: (_, _) => nodeBuilder
      )
    );

    // add keyed/singleton IMuninNode
    builder.Services.Add(
      ServiceDescriptor.KeyedSingleton<IMuninNode>(
        serviceKey: nodeBuilder.ServiceKey,
        static (serviceProvider, serviceKey)
            => serviceProvider.GetRequiredKeyedService<IMuninNodeBuilder>(serviceKey).Build(serviceProvider)
      )
    );

    // add keyless/multiple IMuninNode
    builder.Services.Add(
      ServiceDescriptor.Transient<IMuninNode>(
        nodeBuilder.Build
      )
    );

    return nodeBuilder;
  }
}
