// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;

using Microsoft.Extensions.DependencyInjection;

namespace Smdn.Net.MuninNode.DependencyInjection;

public static class IMuninServiceBuilderExtensions {
  /// <summary>
  /// Adds a <c>Munin-Node</c> to the <see cref="IMuninServiceBuilder"/> with default configurations.
  /// </summary>
  /// <param name="builder">
  /// An <see cref="IMuninServiceBuilder"/> that the built <c>Munin-Node</c> will be added to.
  /// </param>
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
  /// An <see cref="IMuninServiceBuilder"/> that the built <c>Munin-Node</c> will be added to.
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
    => AddNode<
      MuninNodeOptions,
      MuninNodeBuilder
    >(
      builder: builder ?? throw new ArgumentNullException(nameof(builder)),
      configure: configure ?? throw new ArgumentNullException(nameof(configure)),
      createBuilder: static (serviceBuilder, serviceKey) => new(serviceBuilder, serviceKey)
    );

  /// <summary>
  /// Adds a <c>Munin-Node</c> to the <see cref="IMuninServiceBuilder"/> with specified configurations.
  /// </summary>
  /// <typeparam name="TMuninNodeOptions">
  /// The extended type of <see cref="MuninNodeOptions"/> to configure the <c>Munin-Node</c>.
  /// </typeparam>
  /// <typeparam name="TMuninNodeBuilder">
  /// The extended type of <see cref="MuninNodeBuilder"/> to build the <c>Munin-Node</c>.
  /// </typeparam>
  /// <param name="builder">
  /// An <see cref="IMuninServiceBuilder"/> that the built <c>Munin-Node</c> will be added to.
  /// </param>
  /// <param name="configure">
  /// An <see cref="Action{TMuninNodeOptions}"/> to setup <typeparamref name="TMuninNodeOptions"/> to
  /// configure the <c>Munin-Node</c> to be built.
  /// </param>
  /// <param name="createBuilder">
  /// An <see cref="Func{TMuninNodeBuilder}"/> to create <typeparamref name="TMuninNodeBuilder"/> to build
  /// the <c>Munin-Node</c>.
  /// </param>
  /// <returns>The current <see cref="IMuninNodeBuilder"/> so that additional calls can be chained.</returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="builder"/> is <see langword="null"/>, or
  /// <paramref name="configure"/> is <see langword="null"/>, or
  /// <paramref name="createBuilder"/> is <see langword="null"/>.
  /// </exception>
  public static
  IMuninNodeBuilder AddNode<
    TMuninNodeOptions,
    TMuninNodeBuilder
  >(
    this IMuninServiceBuilder builder,
    Action<TMuninNodeOptions> configure,
    Func<IMuninServiceBuilder, string, TMuninNodeBuilder> createBuilder
  )
    where TMuninNodeOptions : MuninNodeOptions, new()
    where TMuninNodeBuilder : MuninNodeBuilder
  {
    if (builder is null)
      throw new ArgumentNullException(nameof(builder));
    if (configure is null)
      throw new ArgumentNullException(nameof(configure));
    if (createBuilder is null)
      throw new ArgumentNullException(nameof(createBuilder));

    var configuredOptions = new TMuninNodeOptions();

    configure(configuredOptions);

    var nodeBuilder = createBuilder(
      /* serviceBuilder: */ builder,
      /* serviceKey: */ configuredOptions.HostName // use configured hostname as a service key and option name
    );

    _ = builder.Services.Configure<TMuninNodeOptions>(
      name: nodeBuilder.ServiceKey, // configure MuninNodeOptions for this builder
      options => options.Configure(configuredOptions)
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
