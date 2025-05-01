// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NUnit.Framework;

namespace Smdn.Net.MuninNode.Hosting;

[TestFixture]
public class IServiceCollectionExtensionsTests {
  [Test]
  public void AddHostedMuninNodeService_ArgumentNull()
  {
    var services = new ServiceCollection();

    Assert.That(
      () => services.AddHostedMuninNodeService(
        configureNode: null!,
        buildNode: builder => { }
      ),
      Throws
        .ArgumentNullException
        .With
        .Property(nameof(ArgumentNullException.ParamName))
        .EqualTo("configureNode")
    );

    Assert.That(
      () => services.AddHostedMuninNodeService(
        configureNode: options => { },
        buildNode: null!
      ),
      Throws
        .ArgumentNullException
        .With
        .Property(nameof(ArgumentNullException.ParamName))
        .EqualTo("buildNode")
    );
  }

  [Test]
  public void AddHostedMuninNodeService()
  {
    var services = new ServiceCollection();

    services.AddHostedMuninNodeService(
      configureNode: options => { },
      buildNode: builder => { }
    );

    var serviceProvider = services.BuildServiceProvider();
    var muninNodeService = serviceProvider.GetRequiredService<IHostedService>();

    Assert.That(muninNodeService, Is.TypeOf<MuninNodeBackgroundService>());
  }
}
