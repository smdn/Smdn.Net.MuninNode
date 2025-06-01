// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_DYNAMICALLYACCESSEDMEMBERSATTRIBUTE
using System.Diagnostics.CodeAnalysis;
#endif

using Microsoft.Extensions.DependencyInjection;

using Smdn.Net.MuninNode.DependencyInjection;

namespace Smdn.Net.MuninNode.Hosting;

public static class IServiceCollectionExtensions {
  /// <summary>
  /// Add <see cref="MuninNodeBackgroundService"/>, which runs <c>Munin-Node</c> as an
  /// <see cref="Microsoft.Extensions.Hosting.IHostedService"/>, to <see cref="IServiceCollection"/>.
  /// </summary>
  /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
  /// <param name="configureNode">
  /// An <see cref="Action{MuninNodeOptions}"/> to setup <see cref="MuninNodeOptions"/> to
  /// configure the <c>Munin-Node</c> to be built.
  /// </param>
  /// <param name="buildNode">
  /// An <see cref="Action{IMuninNodeBuilder}"/> to build <c>Munin-Node</c> using with
  /// the <see cref="IMuninNodeBuilder"/>.
  /// </param>
  /// <returns>The current <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="services"/> is <see langword="null"/>, or
  /// <paramref name="configureNode"/> is <see langword="null"/>, or
  /// <paramref name="buildNode"/> is <see langword="null"/>.
  /// </exception>
#pragma warning disable CS0618 // accept MuninNodeBuilder instead of IMuninNodeBuilder
  public static IServiceCollection AddHostedMuninNodeService(
    this IServiceCollection services,
    Action<MuninNodeOptions> configureNode,
    Action<IMuninNodeBuilder> buildNode
  )
#pragma warning restore CS0618
    => AddHostedMuninNodeService<
      MuninNodeBackgroundService,
      IMuninNode,
      IMuninNode,
      MuninNodeOptions,
      DefaultMuninNodeBuilder
    >(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      configureNode: configureNode ?? throw new ArgumentNullException(nameof(configureNode)),
      createNodeBuilder: static (serviceBuilder, serviceKey) => new(serviceBuilder, serviceKey),
      buildNode: builder => (buildNode ?? throw new ArgumentNullException(nameof(buildNode)))(builder)
    );

  private class DefaultMuninNodeBuilder(IMuninServiceBuilder serviceBuilder, string serviceKey)
    : MuninNodeBuilder(serviceBuilder, serviceKey) {
  }

  /// <summary>
  /// Add <typeparamref name="TMuninNodeBackgroundService"/>, which runs <typeparamref name="TMuninNode"/> as an
  /// <see cref="Microsoft.Extensions.Hosting.IHostedService"/>, to <see cref="IServiceCollection"/>.
  /// </summary>
  /// <typeparam name="TMuninNodeBackgroundService">
  /// The type of <see cref="Microsoft.Extensions.Hosting.IHostedService"/> service to add to the <seealso cref="IServiceCollection"/>.
  /// </typeparam>
  /// <typeparam name="TMuninNode">
  /// The type of <see cref="IMuninNode"/> service to add to the <seealso cref="IServiceCollection"/>.
  /// </typeparam>
  /// <typeparam name="TMuninNodeOptions">
  /// The extended type of <see cref="MuninNodeOptions"/> to configure the <typeparamref name="TMuninNode"/>.
  /// </typeparam>
  /// <typeparam name="TMuninNodeBuilder">
  /// The extended type of <see cref="MuninNodeBuilder"/> to build the <typeparamref name="TMuninNode"/>.
  /// </typeparam>
  /// <param name="services">
  /// An <see cref="IServiceCollection"/> that the built <typeparamref name="TMuninNodeBackgroundService"/> and
  /// <typeparamref name="TMuninNode"/> will be added to.
  /// </param>
  /// <param name="configureNode">
  /// An <see cref="Action{TMuninNodeOptions}"/> to setup <typeparamref name="TMuninNodeOptions"/> to
  /// configure the <typeparamref name="TMuninNode"/> to be built.
  /// </param>
  /// <param name="createNodeBuilder">
  /// An <see cref="Func{TMuninNodeBuilder}"/> to create <typeparamref name="TMuninNodeBuilder"/> to build
  /// the <typeparamref name="TMuninNode"/>.
  /// </param>
  /// <param name="buildNode">
  /// An <see cref="Action{TMuninNodeBuilder}"/> to build <typeparamref name="TMuninNode"/> using with
  /// the <typeparamref name="TMuninNodeBuilder"/>.
  /// </param>
  /// <returns>The current <see cref="IMuninNodeBuilder"/> so that additional calls can be chained.</returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="services"/> is <see langword="null"/>, or
  /// <paramref name="configureNode"/> is <see langword="null"/>, or
  /// <paramref name="createNodeBuilder"/> is <see langword="null"/>, or
  /// <paramref name="buildNode"/> is <see langword="null"/>.
  /// </exception>
#pragma warning disable IDE0055
  public static
  IServiceCollection AddHostedMuninNodeService<
#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_DYNAMICALLYACCESSEDMEMBERSATTRIBUTE
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
    TMuninNodeBackgroundService,
    TMuninNode,
    TMuninNodeOptions,
    TMuninNodeBuilder
  >(
    this IServiceCollection services,
    Action<TMuninNodeOptions> configureNode,
    Func<IMuninServiceBuilder, string, TMuninNodeBuilder> createNodeBuilder,
    Action<TMuninNodeBuilder> buildNode
  )
    where TMuninNodeBackgroundService : MuninNodeBackgroundService
    where TMuninNode : class, IMuninNode
    where TMuninNodeOptions : MuninNodeOptions, new()
    where TMuninNodeBuilder : MuninNodeBuilder
#pragma warning restore IDE0055
    => AddHostedMuninNodeService<
      TMuninNodeBackgroundService,
      TMuninNode,
      TMuninNode,
      TMuninNodeOptions,
      TMuninNodeBuilder
    >(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      configureNode: configureNode ?? throw new ArgumentNullException(nameof(configureNode)),
      createNodeBuilder: createNodeBuilder ?? throw new ArgumentNullException(nameof(configureNode)),
      buildNode: buildNode ?? throw new ArgumentNullException(nameof(buildNode))
    );

  /// <summary>
  /// Add <typeparamref name="TMuninNodeBackgroundService"/>, which runs <typeparamref name="TMuninNodeImplementation"/> as an
  /// <see cref="Microsoft.Extensions.Hosting.IHostedService"/>, to <see cref="IServiceCollection"/>.
  /// </summary>
  /// <typeparam name="TMuninNodeBackgroundService">
  /// The type of <see cref="Microsoft.Extensions.Hosting.IHostedService"/> service to add to the <seealso cref="IServiceCollection"/>.
  /// </typeparam>
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
  /// <param name="services">
  /// An <see cref="IServiceCollection"/> that the built <typeparamref name="TMuninNodeBackgroundService"/> and
  /// <typeparamref name="TMuninNodeImplementation"/> will be added to.
  /// </param>
  /// <param name="configureNode">
  /// An <see cref="Action{TMuninNodeOptions}"/> to setup <typeparamref name="TMuninNodeOptions"/> to
  /// configure the <typeparamref name="TMuninNodeImplementation"/> to be built.
  /// </param>
  /// <param name="createNodeBuilder">
  /// An <see cref="Func{TMuninNodeBuilder}"/> to create <typeparamref name="TMuninNodeBuilder"/> to build
  /// the <typeparamref name="TMuninNodeImplementation"/>.
  /// </param>
  /// <param name="buildNode">
  /// An <see cref="Action{TMuninNodeBuilder}"/> to build <typeparamref name="TMuninNodeImplementation"/> using with
  /// the <typeparamref name="TMuninNodeBuilder"/>.
  /// </param>
  /// <returns>The current <see cref="IMuninNodeBuilder"/> so that additional calls can be chained.</returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="services"/> is <see langword="null"/>, or
  /// <paramref name="configureNode"/> is <see langword="null"/>, or
  /// <paramref name="createNodeBuilder"/> is <see langword="null"/>, or
  /// <paramref name="buildNode"/> is <see langword="null"/>.
  /// </exception>
#pragma warning disable IDE0055
  public static
  IServiceCollection AddHostedMuninNodeService<
#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_DYNAMICALLYACCESSEDMEMBERSATTRIBUTE
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
    TMuninNodeBackgroundService,
    TMuninNodeService,
    TMuninNodeImplementation,
    TMuninNodeOptions,
    TMuninNodeBuilder
  >(
    this IServiceCollection services,
    Action<TMuninNodeOptions> configureNode,
    Func<IMuninServiceBuilder, string, TMuninNodeBuilder> createNodeBuilder,
    Action<TMuninNodeBuilder> buildNode
  )
    where TMuninNodeBackgroundService : MuninNodeBackgroundService
    where TMuninNodeService : class, IMuninNode
    where TMuninNodeImplementation : class, TMuninNodeService
    where TMuninNodeOptions : MuninNodeOptions, new()
    where TMuninNodeBuilder : MuninNodeBuilder
#pragma warning restore IDE0055
  {
    if (services is null)
      throw new ArgumentNullException(nameof(services));
    if (configureNode is null)
      throw new ArgumentNullException(nameof(configureNode));
    if (createNodeBuilder is null)
      throw new ArgumentNullException(nameof(createNodeBuilder));
    if (buildNode is null)
      throw new ArgumentNullException(nameof(buildNode));

    return AddHostedMuninNodeService<TMuninNodeBackgroundService, TMuninNodeBuilder>(
      services: services,
      buildMunin: muninBuilder => {
        var muninNodeBuilder = muninBuilder.AddNode<
          TMuninNodeService,
          TMuninNodeImplementation,
          TMuninNodeOptions,
          TMuninNodeBuilder
        >(
          configureNode,
          createNodeBuilder
        );

        buildNode(muninNodeBuilder);

        return muninNodeBuilder;
      }
    );
  }

  /// <summary>
  /// Add <typeparamref name="TMuninNodeBackgroundService"/>, which runs <c>Munin-Node</c> as an
  /// <see cref="Microsoft.Extensions.Hosting.IHostedService"/>, to <see cref="IServiceCollection"/>.
  /// </summary>
  /// <typeparam name="TMuninNodeBackgroundService">
  /// The type of <see cref="Microsoft.Extensions.Hosting.IHostedService"/> service to add to the <seealso cref="IServiceCollection"/>.
  /// </typeparam>
  /// <typeparam name="TMuninNodeBuilder">
  /// The extended type of <see cref="MuninNodeBuilder"/> to build the <c>Munin-Node</c>.
  /// </typeparam>
  /// <param name="services">
  /// An <see cref="IServiceCollection"/> that the built <typeparamref name="TMuninNodeBackgroundService"/> and
  /// <c>Munin-Node</c> will be added to.
  /// </param>
  /// <param name="buildMunin">
  /// A <see cref="Func{IMuninServiceBuilder, TMuninNodeBuilder}"/> that registers at least one <see cref="IMuninNode"/> to
  /// <paramref name="services"/> and returns <typeparamref name="TMuninNodeBuilder"/>, which builds the <see cref="IMuninNode"/>
  /// to be registered.
  /// </param>
  /// <returns>The current <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="services"/> is <see langword="null"/>, or
  /// <paramref name="buildMunin"/> is <see langword="null"/>.
  /// </exception>
  /// <remarks>
  /// In future implementations, <typeparamref name="TMuninNodeBackgroundService"/> to be registered by
  /// this method will use the same key as the <see cref="MuninNodeBuilder.ServiceKey"/> of the
  /// <typeparamref name="TMuninNodeBuilder"/> returned by the <paramref name="buildMunin"/>.
  /// </remarks>
#pragma warning disable IDE0055
  public static
  IServiceCollection AddHostedMuninNodeService<
#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_DYNAMICALLYACCESSEDMEMBERSATTRIBUTE
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
    TMuninNodeBackgroundService,
    TMuninNodeBuilder
  >(
    this IServiceCollection services,
    Func<IMuninServiceBuilder, TMuninNodeBuilder> buildMunin
  )
    where TMuninNodeBackgroundService : MuninNodeBackgroundService
    where TMuninNodeBuilder : MuninNodeBuilder
#pragma warning restore IDE0055
  {
    if (services is null)
      throw new ArgumentNullException(nameof(services));
    if (buildMunin is null)
      throw new ArgumentNullException(nameof(buildMunin));

    return services.AddMunin(
      muninBuilder => {
        var muninNodeBuilder = buildMunin(muninBuilder);

        muninNodeBuilder.Services.AddHostedService<TMuninNodeBackgroundService>();

        // TODO: support keyed service
#if false
        // these code does not work currently
        // https://github.com/dotnet/runtime/issues/99085
        muninNodeBuilder.Services.AddHostedService<TMuninNodeBackgroundService>(
          serviceKey: muninNodeBuilder.ServiceKey
        );

        muninNodeBuilder.Services.TryAddEnumerable(
          ServiceDescriptor.KeyedSingleton<IHostedService, TMuninNodeBackgroundService>(
            serviceKey: muninNodeBuilder.ServiceKey
          )
        );
#endif
      }
    );
  }
}
