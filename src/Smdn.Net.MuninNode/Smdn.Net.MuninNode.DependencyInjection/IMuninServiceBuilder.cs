// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#pragma warning disable CS0419

using Microsoft.Extensions.DependencyInjection;

namespace Smdn.Net.MuninNode.DependencyInjection;

/// <summary>
/// An interface for configuring the Munin service providers.
/// </summary>
/// <see cref="IMuninServiceBuilderExtensions.AddNode"/>
public interface IMuninServiceBuilder {
  /// <summary>
  /// Gets the <see cref="IServiceCollection"/> where the Munin services are configured.
  /// </summary>
  IServiceCollection Services { get; }
}
