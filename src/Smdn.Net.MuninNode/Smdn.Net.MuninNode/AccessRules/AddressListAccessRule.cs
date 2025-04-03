// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Smdn.Net.MuninNode.AccessRules;

internal sealed class AddressListAccessRule : IAccessRule {
  private readonly IReadOnlyList<IPAddress> addressListAllowFrom;

  public AddressListAccessRule(IReadOnlyList<IPAddress> addressListAllowFrom)
  {
    this.addressListAllowFrom = addressListAllowFrom ?? throw new ArgumentNullException(nameof(addressListAllowFrom));
  }

  public bool IsAcceptable(IPEndPoint remoteEndPoint)
  {
    if (remoteEndPoint is null)
      throw new ArgumentNullException(nameof(remoteEndPoint));

    var remoteAddress = remoteEndPoint.Address;

    foreach (var addressAllowFrom in addressListAllowFrom) {
      if (addressAllowFrom.AddressFamily == AddressFamily.InterNetwork) {
        // test for client acceptability by IPv4 address
        if (remoteAddress.IsIPv4MappedToIPv6)
          remoteAddress = remoteAddress.MapToIPv4();
      }

      if (addressAllowFrom.Equals(remoteAddress))
        return true;
    }

    return false;
  }
}
