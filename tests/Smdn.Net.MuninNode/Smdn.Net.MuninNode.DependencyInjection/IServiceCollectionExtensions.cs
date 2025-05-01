// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

namespace Smdn.Net.MuninNode.DependencyInjection;

[TestFixture]
public class IServiceCollectionExtensionsTests {
  [Test]
  public void AddMunin_ArgumentNull()
  {
    var services = new ServiceCollection();

    Assert.That(
      () => services.AddMunin(configure: null!),
      Throws.ArgumentNullException
    );
  }

  [Test]
  public void AddMunin()
  {
    var services = new ServiceCollection();

    var ret = services.AddMunin(configure => { });

    Assert.That(ret, Is.SameAs(services));

    var muninServiceBuilder = services.BuildServiceProvider().GetService<IMuninServiceBuilder>();

    Assert.That(muninServiceBuilder, Is.Null);
  }
}
