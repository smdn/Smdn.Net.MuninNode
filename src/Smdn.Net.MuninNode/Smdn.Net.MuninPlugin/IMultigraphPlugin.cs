// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Collections.Generic;

namespace Smdn.Net.MuninPlugin;

/// <summary>
/// Provides an interface that abstracts the multigraph plugin.
/// </summary>
/// <remarks>
/// For plugins implementing this interface, <see cref="IPlugin.GraphAttributes"/> and <see cref="IPlugin.DataSource"/>
/// are not referenced and are ignored.
/// </remarks>
/// <seealso cref="IPlugin"/>
/// <seealso href="https://guide.munin-monitoring.org/en/latest/plugin/multigraphing.html">Multigraph plugins</seealso>
/// <seealso href="https://guide.munin-monitoring.org/en/latest/plugin/protocol-multigraph.html">Protocol extension: multiple graphs from one plugin</seealso>
public interface IMultigraphPlugin : IPlugin {
  /// <summary>
  /// Gets a collection of <see cref="IPlugin"/> that constitutes the multiple graphs generated from this plugin.
  /// </summary>
  /// <remarks>
  /// If <c>munin master</c> supports <see href="https://guide.munin-monitoring.org/en/latest/plugin/protocol-multigraph.html">the <c>multigraph</c> capability</see>,
  /// each <see cref="IPlugin"/> is handled as a set of names, graph attributes and data sources that
  /// constitute each graph, rather than as individual plugins.
  /// Otherwise, each <see cref="IPlugin"/> is handled as an individual plugin.
  /// </remarks>
  IReadOnlyCollection<IPlugin> Plugins { get; }
}
