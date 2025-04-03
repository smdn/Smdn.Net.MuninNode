// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Net;

using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

using Smdn.Net.MuninNode.AccessRules;

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
}
