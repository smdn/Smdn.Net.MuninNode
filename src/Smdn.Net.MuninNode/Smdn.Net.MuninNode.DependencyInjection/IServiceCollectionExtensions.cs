// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;

using Microsoft.Extensions.DependencyInjection;

namespace Smdn.Net.MuninNode.DependencyInjection;

public static class IServiceCollectionExtensions {
  /// <summary>
  /// Adds an <see cref="IMuninServiceBuilder"/> to the <see cref="IServiceCollection"/>.
  /// </summary>
  /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
  /// <param name="configure">
  /// An <see cref="Action{IMuninServiceBuilder}"/> to add munin services to the <see cref="IMuninServiceBuilder"/>.
  /// </param>
  /// <returns>The current <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
  public static IServiceCollection AddMunin(
    this IServiceCollection services,
    Action<IMuninServiceBuilder> configure
  )
  {
    if (services is null)
      throw new ArgumentNullException(nameof(services));
    if (configure is null)
      throw new ArgumentNullException(nameof(configure));

    configure(new MuninServiceBuilder(services));

    return services;
  }
}
