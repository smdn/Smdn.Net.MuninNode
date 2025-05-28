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
      IMuninNode,
      IMuninNode,
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
  /// <returns>The current <typeparamref name="TMuninNodeBuilder"/> so that additional calls can be chained.</returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="builder"/> is <see langword="null"/>, or
  /// <paramref name="configure"/> is <see langword="null"/>, or
  /// <paramref name="createBuilder"/> is <see langword="null"/>.
  /// </exception>
  public static
  TMuninNodeBuilder AddNode<
    TMuninNodeOptions,
    TMuninNodeBuilder
  >(
    this IMuninServiceBuilder builder,
    Action<TMuninNodeOptions> configure,
    Func<IMuninServiceBuilder, string, TMuninNodeBuilder> createBuilder
  )
    where TMuninNodeOptions : MuninNodeOptions, new()
    where TMuninNodeBuilder : MuninNodeBuilder
    => AddNode<
      IMuninNode,
      IMuninNode,
      TMuninNodeOptions,
      TMuninNodeBuilder
    >(
      builder: builder ?? throw new ArgumentNullException(nameof(builder)),
      configure: configure ?? throw new ArgumentNullException(nameof(configure)),
      createBuilder: createBuilder ?? throw new ArgumentNullException(nameof(createBuilder))
    );

  /// <summary>
  /// Adds a <typeparamref name="TMuninNode"/> to the <see cref="IMuninServiceBuilder"/> with specified configurations.
  /// </summary>
  /// <typeparam name="TMuninNode">
  /// The type of <see cref="IMuninNode"/> service to add to the <seealso cref="IServiceCollection"/>.
  /// </typeparam>
  /// <typeparam name="TMuninNodeOptions">
  /// The extended type of <see cref="MuninNodeOptions"/> to configure the <typeparamref name="TMuninNode"/>.
  /// </typeparam>
  /// <typeparam name="TMuninNodeBuilder">
  /// The extended type of <see cref="MuninNodeBuilder"/> to build the <typeparamref name="TMuninNode"/>.
  /// </typeparam>
  /// <param name="builder">
  /// An <see cref="IMuninServiceBuilder"/> that the built <typeparamref name="TMuninNode"/> will be added to.
  /// </param>
  /// <param name="configure">
  /// An <see cref="Action{TMuninNodeOptions}"/> to setup <typeparamref name="TMuninNodeOptions"/> to
  /// configure the <typeparamref name="TMuninNode"/> to be built.
  /// </param>
  /// <param name="createBuilder">
  /// An <see cref="Func{TMuninNodeBuilder}"/> to create <typeparamref name="TMuninNodeBuilder"/> to build
  /// the <typeparamref name="TMuninNode"/>.
  /// </param>
  /// <returns>The current <typeparamref name="TMuninNodeBuilder"/> so that additional calls can be chained.</returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="builder"/> is <see langword="null"/>, or
  /// <paramref name="configure"/> is <see langword="null"/>, or
  /// <paramref name="createBuilder"/> is <see langword="null"/>.
  /// </exception>
  public static
  TMuninNodeBuilder AddNode<
    TMuninNode,
    TMuninNodeOptions,
    TMuninNodeBuilder
  >(
    this IMuninServiceBuilder builder,
    Action<TMuninNodeOptions> configure,
    Func<IMuninServiceBuilder, string, TMuninNodeBuilder> createBuilder
  )
    where TMuninNode : class, IMuninNode
    where TMuninNodeOptions : MuninNodeOptions, new()
    where TMuninNodeBuilder : MuninNodeBuilder
    => AddNode<
      TMuninNode,
      TMuninNode,
      TMuninNodeOptions,
      TMuninNodeBuilder
    >(
      builder: builder ?? throw new ArgumentNullException(nameof(builder)),
      configure: configure ?? throw new ArgumentNullException(nameof(configure)),
      createBuilder: createBuilder ?? throw new ArgumentNullException(nameof(createBuilder))
    );

  /// <summary>
  /// Adds a <typeparamref name="TMuninNodeImplementation"/> to the <see cref="IMuninServiceBuilder"/> with specified configurations.
  /// </summary>
  /// <typeparam name="TMuninNodeService">
  /// The type of <see cref="IMuninNode"/> service to add to the <seealso cref="IServiceCollection"/>.
  /// </typeparam>
  /// <typeparam name="TMuninNodeImplementation">
  /// The type of <typeparamref name="TMuninNodeService"/> implementation.
  /// </typeparam>
  /// <typeparam name="TMuninNodeOptions">
  /// The extended type of <see cref="MuninNodeOptions"/> to configure the <typeparamref name="TMuninNodeImplementation"/>.
  /// </typeparam>
  /// <typeparam name="TMuninNodeBuilder">
  /// The extended type of <see cref="MuninNodeBuilder"/> to build the <typeparamref name="TMuninNodeImplementation"/>.
  /// </typeparam>
  /// <param name="builder">
  /// An <see cref="IMuninServiceBuilder"/> that the built <typeparamref name="TMuninNodeImplementation"/> will be added to.
  /// </param>
  /// <param name="configure">
  /// An <see cref="Action{TMuninNodeOptions}"/> to setup <typeparamref name="TMuninNodeOptions"/> to
  /// configure the <typeparamref name="TMuninNodeImplementation"/> to be built.
  /// </param>
  /// <param name="createBuilder">
  /// An <see cref="Func{TMuninNodeBuilder}"/> to create <typeparamref name="TMuninNodeBuilder"/> to build
  /// the <typeparamref name="TMuninNodeImplementation"/>.
  /// </param>
  /// <returns>The current <see cref="IMuninNodeBuilder"/> so that additional calls can be chained.</returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="builder"/> is <see langword="null"/>, or
  /// <paramref name="configure"/> is <see langword="null"/>, or
  /// <paramref name="createBuilder"/> is <see langword="null"/>.
  /// </exception>
  public static
  TMuninNodeBuilder AddNode<
    TMuninNodeService,
    TMuninNodeImplementation,
    TMuninNodeOptions,
    TMuninNodeBuilder
  >(
    this IMuninServiceBuilder builder,
    Action<TMuninNodeOptions> configure,
    Func<IMuninServiceBuilder, string, TMuninNodeBuilder> createBuilder
  )
    where TMuninNodeService : class, IMuninNode
    where TMuninNodeImplementation : class, TMuninNodeService
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
      ServiceDescriptor.KeyedSingleton<TMuninNodeBuilder>(
        serviceKey: nodeBuilder.ServiceKey,
        implementationFactory: (_, _) => nodeBuilder
      )
    );

    // add keyed/singleton IMuninNode
    builder.Services.Add(
      ServiceDescriptor.KeyedSingleton<TMuninNodeService, TMuninNodeImplementation>(
        serviceKey: nodeBuilder.ServiceKey,
        static (serviceProvider, serviceKey)
          => serviceProvider
            .GetRequiredKeyedService<TMuninNodeBuilder>(serviceKey)
            .Build<TMuninNodeImplementation>(serviceProvider)
      )
    );

    // add keyless/multiple IMuninNode
#pragma warning disable IDE0200
    builder.Services.Add(
      ServiceDescriptor.Transient<TMuninNodeService, TMuninNodeImplementation>(
        serviceProvider => nodeBuilder.Build<TMuninNodeImplementation>(serviceProvider)
      )
    );
#pragma warning restore IDE0200

    return nodeBuilder;
  }
}
