// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Net;

using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

namespace Smdn.Net.MuninNode;

[TestFixture]
public class IAccessRuleServiceCollectionExtensionsTests {
  [Test]
  public void AddMuninNodeAccessRule_TryAddMultiple()
  {
    var services = new ServiceCollection();

    services.AddMuninNodeAccessRule([IPAddress.Any]);

    var firstAccessRule = services.BuildServiceProvider().GetRequiredService<IAccessRule>();

    Assert.That(firstAccessRule, Is.Not.Null, nameof(firstAccessRule));

    services.AddMuninNodeAccessRule([IPAddress.Any]);

    var secondAccessRule = services.BuildServiceProvider().GetRequiredService<IAccessRule>();

    Assert.That(secondAccessRule, Is.SameAs(firstAccessRule), nameof(secondAccessRule));
  }

  [Test]
  public void AddMuninNodeAccessRule_IReadOnlyListOfIPAddress_ArgumentNull()
  {
    var services = new ServiceCollection();

    Assert.Throws<ArgumentNullException>(
      () => services.AddMuninNodeAccessRule((IReadOnlyList<IPAddress>)null!)
    );
  }

  [Test]
  public void AddMuninNodeAccessRule_IAccessRule_ArgumentNull()
  {
    var services = new ServiceCollection();

    Assert.Throws<ArgumentNullException>(
      () => services.AddMuninNodeAccessRule((IAccessRule)null!)
    );
  }

  private static System.Collections.IEnumerable YieldTestCases_AddMuninNodeAccessRule_IReadOnlyListOfIPAddress_ShouldConsiderIPv4MappedIPv6Address()
  {
    var addressIPv4 = IPAddress.Parse("192.0.2.255");
    var addressIPv6MappedIPv4 = IPAddress.Parse("::ffff:192.0.2.255");

    foreach (var shouldConsiderIPv4MappedIPv6Address in new[] { true, false }) {
      yield return new object[] {
        new[] { addressIPv4 },
        shouldConsiderIPv4MappedIPv6Address,
        addressIPv6MappedIPv4,
        shouldConsiderIPv4MappedIPv6Address
      };

      yield return new object[] {
        new[] { addressIPv6MappedIPv4 },
        shouldConsiderIPv4MappedIPv6Address,
        addressIPv4,
        shouldConsiderIPv4MappedIPv6Address
      };

      yield return new object[] {
        new[] { addressIPv4, addressIPv6MappedIPv4 },
        shouldConsiderIPv4MappedIPv6Address,
        addressIPv6MappedIPv4,
        true
      };

      yield return new object[] {
        new[] { addressIPv4, addressIPv6MappedIPv4 },
        shouldConsiderIPv4MappedIPv6Address,
        addressIPv4,
        true
      };

      yield return new object[] {
        new[] { addressIPv4, addressIPv6MappedIPv4 },
        shouldConsiderIPv4MappedIPv6Address,
        IPAddress.Loopback,
        false
      };
    }
  }

  [TestCaseSource(nameof(YieldTestCases_AddMuninNodeAccessRule_IReadOnlyListOfIPAddress_ShouldConsiderIPv4MappedIPv6Address))]
  public void AddMuninNodeAccessRule_IReadOnlyListOfIPAddress_ShouldConsiderIPv4MappedIPv6Address(
    IReadOnlyList<IPAddress> addressListAllowFrom,
    bool shouldConsiderIPv4MappedIPv6Address,
    IPAddress remoteAddress,
    bool expected
  )
  {
    var services = new ServiceCollection();

    services.AddMuninNodeAccessRule(
      addressListAllowFrom: addressListAllowFrom,
      shouldConsiderIPv4MappedIPv6Address: shouldConsiderIPv4MappedIPv6Address
    );

    var accessRule = services.BuildServiceProvider().GetRequiredService<IAccessRule>();

    Assert.That(
      accessRule.IsAcceptable(new(remoteAddress, port: 0)),
      Is.EqualTo(expected)
    );
  }
}
