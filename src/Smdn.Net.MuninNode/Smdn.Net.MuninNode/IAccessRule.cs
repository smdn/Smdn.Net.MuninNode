// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Net;

namespace Smdn.Net.MuninNode;

/// <summary>
/// Provides an interface for defining rules to determine whether a client connecting to
/// <c>Munin-Node</c> is acceptable for access, based on the endpoint of the client.
/// </summary>
/// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/munin-node.conf.html#cmdoption-arg-allow">munin-node.conf - DIRECTIVES - Inherited - allow</seealso>
/// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/munin-node.conf.html#cmdoption-arg-cidr-allow">munin-node.conf - DIRECTIVES - Inherited - cidr_allow</seealso>
/// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/munin-node.conf.html#cmdoption-arg-cidr-deny">munin-node.conf - DIRECTIVES - Inherited - cidr_deny</seealso>
public interface IAccessRule {
  /// <summary>
  /// Returns a value indicating whether the <see cref="IPEndPoint"/> is acceptable in the
  /// access rules defined by this instance.
  /// </summary>
  /// <param name="remoteEndPoint"><see cref="IPEndPoint"/> of the remote host requesting access.</param>
  /// <returns><see langword="true"/> if acceptable, <see langword="false"/> otherwise.</returns>
  bool IsAcceptable(IPEndPoint remoteEndPoint);
}
