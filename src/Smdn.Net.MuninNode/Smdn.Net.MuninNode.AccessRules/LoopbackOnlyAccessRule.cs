// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;

namespace Smdn.Net.MuninNode.AccessRules;

internal sealed class LoopbackOnlyAccessRule : IAccessRule {
  public static LoopbackOnlyAccessRule Instance { get; } = new();

  public bool IsAcceptable(IPEndPoint remoteEndPoint)
    => IPAddress.IsLoopback(
      (remoteEndPoint ?? throw new ArgumentNullException(nameof(remoteEndPoint))).Address
    );
}
