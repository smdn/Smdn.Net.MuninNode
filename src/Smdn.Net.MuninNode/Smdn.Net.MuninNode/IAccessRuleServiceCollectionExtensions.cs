// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Smdn.Net.MuninNode;

public static class IAccessRuleServiceCollectionExtensions {
  internal class AddressListAccessRule : IAccessRule {
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

  /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
  /// <param name="addressListAllowFrom">The <see cref="IReadOnlyList{IPAddress}"/> indicates the read-only list of addresses allowed to access <see cref="NodeBase"/>.</param>
  public static IServiceCollection AddMuninNodeAccessRule(
    this IServiceCollection services,
    IReadOnlyList<IPAddress> addressListAllowFrom
  )
    => AddMuninNodeAccessRule(
      services: services ?? throw new ArgumentNullException(nameof(services)),
      accessRule: new AddressListAccessRule(
        addressListAllowFrom: addressListAllowFrom ?? throw new ArgumentNullException(nameof(addressListAllowFrom))
      )
    );

  /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
  /// <param name="accessRule">The <see cref="IAccessRule"/> which defines access rules to <see cref="NodeBase"/>.</param>
  public static IServiceCollection AddMuninNodeAccessRule(
    this IServiceCollection services,
    IAccessRule accessRule
  )
  {
#pragma warning disable CA1510
    if (services is null)
      throw new ArgumentNullException(nameof(services));
    if (accessRule is null)
      throw new ArgumentNullException(nameof(accessRule));
#pragma warning restore CA1510

    services.TryAdd(
      ServiceDescriptor.Singleton(typeof(IAccessRule), accessRule)
    );

    return services;
  }
}
