// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using NUnit.Framework;

namespace Smdn.Net.MuninNode.AccessRules;

[TestFixture]
public class LoopbackOnlyAccessRuleTests {
  [Test]
  public new void ToString()
  {
    var options = new MuninNodeOptions().AllowFromLoopbackOnly();

    Assert.That(options.AccessRule!.ToString(), Is.Not.Empty);
    Assert.That(options.AccessRule!.ToString(), Does.Contain("LoopbackOnlyAccessRule"));
  }
}
