// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Net;

using NUnit.Framework;

namespace Smdn.Net.MuninNode.AccessRules;

[TestFixture]
public class AddressListAccessRuleTests {
  [Test]
  public new void ToString()
  {
    const string AllowFromAddressString = "192.0.2.1";

    var options = new MuninNodeOptions().AllowFrom([IPAddress.Parse(AllowFromAddressString)]);

    Assert.That(options.AccessRule!.ToString(), Is.Not.Empty);
    Assert.That(options.AccessRule!.ToString(), Does.Contain("AddressListAccessRule"));
    Assert.That(options.AccessRule!.ToString(), Does.Contain(AllowFromAddressString));
  }
}
