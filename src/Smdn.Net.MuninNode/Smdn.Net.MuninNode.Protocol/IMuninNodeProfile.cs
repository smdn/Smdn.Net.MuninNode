// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Text;

using Smdn.Net.MuninPlugin;

namespace Smdn.Net.MuninNode.Protocol;

/// <summary>
/// An interface for referencing properties that represent the profile of the constructed <c>Munin-Node</c>.
/// </summary>
public interface IMuninNodeProfile {
  /// <summary>
  /// Gets the string representing the hostname of the node.
  /// </summary>
  /// <remarks>
  /// This value is used as the response to the `node` command.
  /// </remarks>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/master/network-protocol.html">
  /// Data exchange between master and node - `node` command
  /// </seealso>
  public string HostName { get; }

  /// <summary>
  /// Gets the string representing the version information of the node.
  /// </summary>
  /// <remarks>
  /// This value is used as the response to the `version` command.
  /// </remarks>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/master/network-protocol.html">
  /// Data exchange between master and node - `version` command
  /// </seealso>
  public string Version { get; }

  /// <summary>
  /// Gets the <see cref="IPluginProvider"/> representing the munin plugins provided by the node.
  /// </summary>
  /// <remarks>
  /// This value is used as the response to the `list`, `config` and `fetch` command.
  /// </remarks>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/master/network-protocol.html">
  /// Data exchange between master and node - `list`, `config` and `fetch` command
  /// </seealso>
  public IPluginProvider PluginProvider { get; }

  /// <summary>
  /// Gets the <see cref="Encoding"/> used for data exchange with the <c>munin master</c>.
  /// </summary>
  public Encoding Encoding { get; }
}
