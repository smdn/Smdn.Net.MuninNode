// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;

namespace Smdn.Net.MuninPlugin;

/// <summary>
/// Provides an interface that abstracts the plugin.
/// </summary>
/// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/plugin.html">Plugin reference</seealso>
public interface IPlugin {
  /// <summary>Gets a plugin name.</summary>
  /// <remarks>This value is used as the plugin name returned by the 'list' argument, or the plugin name specified by the 'fetch' argument.</remarks>
  string Name { get; }

  /// <summary>Gets a <see cref="IPluginGraphAttributes"/> that represents the graph attributes when the field values (<see cref="IPluginField"/>) are drawn as a graph.</summary>
  /// <seealso cref="IPluginGraphAttributes"/>
  /// <seealso cref="PluginGraphAttributes"/>
  IPluginGraphAttributes GraphAttributes { get; }

  /// <summary>Gets a <see cref="IPluginDataSource"/> that serves as the data source for the plugin.</summary>
  /// <seealso cref="IPluginDataSource"/>
  IPluginDataSource DataSource { get; }

  /// <summary>Gets a <see cref="INodeSessionCallback"/>, which defines the callbacks when a request session from the <c>munin-update</c> starts or ends, such as fetching data or getting configurations.</summary>
  /// <remarks>Callbacks of this interface can be used to initiate bulk collection of field values.</remarks>
  /// <seealso cref="INodeSessionCallback"/>
  /// <seealso cref="MuninNode.NodeBase"/>
#pragma warning disable CS0618
  [Obsolete(message: INodeSessionCallback.ObsoleteMessage.SessionCallbackProperty)]
#pragma warning restore CS0618
  INodeSessionCallback? SessionCallback { get; }
}
