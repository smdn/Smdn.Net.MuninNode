// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Smdn.Net.MuninNode;

internal sealed class AddressListAccessRule : IAccessRule {
  private readonly IReadOnlyList<IPAddress> addressListAllowFrom;
  private readonly bool shouldConsiderIPv4MappedIPv6Address;

  public AddressListAccessRule(
    IReadOnlyList<IPAddress> addressListAllowFrom,
    bool shouldConsiderIPv4MappedIPv6Address
  )
  {
    this.addressListAllowFrom = addressListAllowFrom ?? throw new ArgumentNullException(nameof(addressListAllowFrom));
    this.shouldConsiderIPv4MappedIPv6Address = shouldConsiderIPv4MappedIPv6Address;
  }

  public bool IsAcceptable(IPEndPoint remoteEndPoint)
  {
    if (remoteEndPoint is null)
      throw new ArgumentNullException(nameof(remoteEndPoint));

    var remoteAddress = remoteEndPoint.Address;

    foreach (var addressAllowFrom in addressListAllowFrom) {
      if (shouldConsiderIPv4MappedIPv6Address) {
        if (
          remoteAddress.IsIPv4MappedToIPv6 &&
          addressAllowFrom.AddressFamily == AddressFamily.InterNetwork
        ) {
          // test for client acceptability by IPv4 address
          remoteAddress = remoteAddress.MapToIPv4();
        }

        if (
          remoteAddress.AddressFamily == AddressFamily.InterNetwork &&
          addressAllowFrom.AddressFamily == AddressFamily.InterNetworkV6
        ) {
          // test for client acceptability by IPv6 address
          remoteAddress = remoteAddress.MapToIPv6();
        }
      }

      if (addressAllowFrom.Equals(remoteAddress))
        return true;
    }

    return false;
  }
}
