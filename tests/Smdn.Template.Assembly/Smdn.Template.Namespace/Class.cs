// SPDX-FileCopyrightText: 2021 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using NUnit.Framework;

namespace Smdn.Template.Namespace {
  [TestFixture]
  public class ClassTests {
    [Test]
    public void Method_Zero() => Assert.Throws<ArgumentOutOfRangeException>(() => new Class().Method(0));

    [TestCase(1)]
    [TestCase(-1)]
    public void Method_NonZero(int n) => Assert.AreEqual(n, new Class().Method(n));
  }
}
