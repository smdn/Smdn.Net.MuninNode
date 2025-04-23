// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
namespace Smdn.Net.MuninNode;

/// <summary>
/// An interface that abstracts an arbitrary <c>Munin-Node</c> implementation.
/// </summary>
public interface IMuninNode {
  /// <summary>
  /// Gets a <see cref="string"/> to be used as the hostname of the <c>Munin-Node</c>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The value of this property is used as the banner response to the
  /// <c>munin master</c> process and the response to the <c>nodes</c> command.
  /// This can be any name different from the host on which the <c>Munin-Node</c> process runs.
  /// </para>
  /// <para>
  /// The value of this property may not be a name that can be resolved by DNS.
  /// It is not guaranteed to be used to determine the endpoint for the <c>Munin-Node</c>.
  /// </para>
  /// </remarks>
  string HostName { get; }
}
