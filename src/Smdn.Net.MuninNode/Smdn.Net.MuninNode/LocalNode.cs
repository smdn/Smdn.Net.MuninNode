// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
namespace Smdn.Net.MuninNode;

/// <summary>
/// Implement a <c>Munin-Node</c> that acts as a node on the localhost and only accepts connections from the local loopback address (127.0.0.1, ::1).
/// </summary>
public abstract partial class LocalNode : NodeBase {
  private const string DefaultHostName = "munin-node.localhost";

  /// <summary>
  /// Initializes a new instance of the <see cref="LocalNode"/> class.
  /// </summary>
  private LocalNode()
    : base(
      accessRule: null,
      logger: null
    )
  {
  }
}
