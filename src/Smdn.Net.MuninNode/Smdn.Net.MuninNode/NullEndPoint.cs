// SPDX-FileCopyrightText: 2026 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;
using System.Net.Sockets;

namespace Smdn.Net.MuninNode;

internal sealed class NullEndPoint : EndPoint {
  public static readonly NullEndPoint Instance = new();

  public override AddressFamily AddressFamily => AddressFamily.Unknown;

  public override EndPoint Create(SocketAddress socketAddress)
    => throw new NotSupportedException();

  public override SocketAddress Serialize()
    => throw new NotImplementedException();

  public override string ToString() => nameof(NullEndPoint);
}
