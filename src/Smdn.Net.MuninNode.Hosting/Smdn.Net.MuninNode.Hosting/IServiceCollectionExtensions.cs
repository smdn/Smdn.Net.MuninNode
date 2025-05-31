// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;

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
  /// An <see cref="Action{IMuninServiceBuilder}"/> to build <c>Munin-Node</c> using with
  /// the <see cref="IMuninServiceBuilder"/>.
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
  {
    if (services is null)
      throw new ArgumentNullException(nameof(services));
    if (configureNode is null)
      throw new ArgumentNullException(nameof(configureNode));
    if (buildNode is null)
      throw new ArgumentNullException(nameof(buildNode));

    return services.AddMunin(
      muninBuilder => {
        var muninNodeBuilder = muninBuilder.AddNode(configureNode);

        buildNode(muninNodeBuilder);

        muninNodeBuilder.Services.AddHostedService<MuninNodeBackgroundService>();

        // TODO: support keyed service
#if false
        var muninNodeBuilder = muninBuilder.AddKeyedNode(configureNode);

        buildNode(muninNodeBuilder);

        // these code does not work currently
        // https://github.com/dotnet/runtime/issues/99085
        muninNodeBuilder.Services.AddHostedService<MuninNodeBackgroundService>(
          serviceKey: muninNodeBuilder.ServiceKey
        );

        muninNodeBuilder.Services.TryAddEnumerable(
          ServiceDescriptor.KeyedSingleton<IHostedService, MuninNodeBackgroundService>(
            serviceKey: muninNodeBuilder.ServiceKey
          )
        );
#endif
      }
    );
  }
}
