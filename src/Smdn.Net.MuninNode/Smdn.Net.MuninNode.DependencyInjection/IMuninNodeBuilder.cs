// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#pragma warning disable CS0419

using System;

using Microsoft.Extensions.DependencyInjection;

namespace Smdn.Net.MuninNode.DependencyInjection;

/// <summary>
/// An interface for configuring the <c>Munin-Node</c> service providers.
/// </summary>
/// <see cref="IMuninNodeBuilderExtensions.AddPlugin"/>
/// <see cref="IMuninNodeBuilderExtensions.UseListenerFactory"/>
/// <see cref="IMuninNodeBuilderExtensions.UseSessionCallback"/>
public interface IMuninNodeBuilder {
  /// <summary>
  /// Gets the <see cref="IServiceCollection"/> where the <c>Munin-Node</c> services are configured.
  /// </summary>
  IServiceCollection Services { get; }

  /// <summary>
  /// Gets the <see cref="string"/> key of <c>Munin-Node</c> service.
  /// </summary>
  /// <remarks>
  /// The value set as the hostname of the <c>Munin-Node</c> (see <see cref="MuninNodeOptions.HostName"/>) is used as the service key.
  /// </remarks>
  /// <see cref="IMuninServiceBuilderExtensions.AddNode(IMuninServiceBuilder, Action{MuninNodeOptions})"/>
  string ServiceKey { get; }

  /// <summary>
  /// Builds the <c>Munin-Node</c> with current configurations.
  /// </summary>
  /// <param name="serviceProvider">
  /// An <see cref="IServiceProvider"/> that provides the services to be used by the <see cref="IMuninNode"/> being built.
  /// </param>
  /// <returns>An initialized <see cref="IMuninNode"/>.</returns>
  IMuninNode Build(IServiceProvider serviceProvider);
}
