// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.MuninPlugin;

/// <summary>
/// Provides a read-only collection of <see cref="IPluginProvider"/>s to aggregate zero or more <see cref="IPluginProvider"/>s and
/// wrap them as a single <see cref="IPluginProvider"/>.
/// </summary>
#pragma warning disable IDE0055
public sealed class ReadOnlyPluginProviderCollection :
  ReadOnlyCollection<IPluginProvider>,
  INodeSessionCallback,
  IPluginProvider
{
#pragma warning restore IDE0055
  /*
   * IPluginProvider
   */
  public IReadOnlyCollection<IPlugin> Plugins { get; }

  INodeSessionCallback? IPluginProvider.SessionCallback => this;

  /*
   * ReadOnlyPluginProviderCollection members
   */
  public ReadOnlyPluginProviderCollection(IList<IPluginProvider> pluginProviders)
    : base(pluginProviders ?? throw new ArgumentNullException(nameof(pluginProviders)))
  {
    Plugins = Items.SelectMany(static provider => provider.Plugins).ToList();
  }

  /*
   * INodeSessionCallback
   */
  async ValueTask INodeSessionCallback.ReportSessionStartedAsync(string sessionId, CancellationToken cancellationToken)
  {
    foreach (var provider in Items) {
      cancellationToken.ThrowIfCancellationRequested();

      if (provider.SessionCallback is INodeSessionCallback sessionCallback)
        await sessionCallback.ReportSessionStartedAsync(sessionId, cancellationToken).ConfigureAwait(false);
    }
  }

  async ValueTask INodeSessionCallback.ReportSessionClosedAsync(string sessionId, CancellationToken cancellationToken)
  {
    foreach (var provider in Items) {
      cancellationToken.ThrowIfCancellationRequested();

      if (provider.SessionCallback is INodeSessionCallback sessionCallback)
        await sessionCallback.ReportSessionClosedAsync(sessionId, cancellationToken).ConfigureAwait(false);
    }
  }
}
