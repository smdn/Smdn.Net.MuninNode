// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Net;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Smdn.Net.MuninNode.AccessRules;

namespace Smdn.Net.MuninNode;

public static class IAccessRuleServiceCollectionExtensions {
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
