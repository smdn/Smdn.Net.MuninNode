// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using NUnit.Framework;

namespace Smdn.Net.MuninNode.DependencyInjection;

[TestFixture]
public class IMuninServiceBuilderExtensionsTests {
  [Test]
  public void AddNode_ArgumentNull()
  {
    var services = new ServiceCollection();

    services.AddMunin(configure: builder => {
      Assert.That(
        () => builder.AddNode(configure: null!),
        Throws
          .ArgumentNullException
          .With
          .Property(nameof(ArgumentNullException.ParamName))
          .EqualTo("configure")
      );
    });
  }

  [TestCase("_")]
  [TestCase("munin-node.localhost")]
  public void AddNode_IMuninNodeBuilderServiceKey(string hostname)
  {
    var services = new ServiceCollection();

    services.AddMunin(configure: builder => {
      var nodeBuilder = builder.AddNode(configure: options => options.HostName = hostname);

      Assert.That(nodeBuilder.ServiceKey, Is.EqualTo(hostname));
    });
  }

  [Test]
  public void AddNode_PostConfigure_MuninNodeOptions()
  {
    const string HostNameForServiceKeyAndOptionName = "munin-node.localhost";
    const string HostNameForPostConfigure = "postconfigure.munin-node.localhost";

    var services = new ServiceCollection();

    services.AddMunin(configure: builder => {
      builder.AddNode(configure: options => options.HostName = HostNameForServiceKeyAndOptionName);
    });

    services.PostConfigure<MuninNodeOptions>(
      name: HostNameForServiceKeyAndOptionName,
      configureOptions: options => options.HostName = HostNameForPostConfigure
    );

    var serviceProvider = services.BuildServiceProvider();
    var node = serviceProvider.GetRequiredService<IMuninNode>();

    Assert.That(node.HostName, Is.EqualTo(HostNameForPostConfigure));
  }

  [Test]
  public void AddNode_GetService()
  {
    var services = new ServiceCollection();

    services.AddMunin(configure: builder => {
      builder.AddNode();
    });

    var serviceProvider = services.BuildServiceProvider();
    var firstNode = serviceProvider.GetService<IMuninNode>();
    var secondNode = serviceProvider.GetService<IMuninNode>();

    Assert.That(firstNode, Is.Not.Null);
    Assert.That(secondNode, Is.Not.SameAs(firstNode));
  }

  [Test]
  public void AddNode_GetKeyedService()
  {
    const string HostName = "munin-node.localhost";
    var services = new ServiceCollection();

    services.AddMunin(configure: builder => {
      builder.AddNode(options => options.HostName = HostName);
    });

    var serviceProvider = services.BuildServiceProvider();
    var firstNode = serviceProvider.GetKeyedService<IMuninNode>(HostName);
    var secondNode = serviceProvider.GetKeyedService<IMuninNode>(HostName);

    Assert.That(firstNode, Is.Not.Null);
    Assert.That(firstNode.HostName, Is.EqualTo(HostName));
    Assert.That(secondNode, Is.SameAs(firstNode));
  }

  [Test]
  public void AddNode_DuplicateHostName()
  {
    const string HostName = "munin-node.localhost";
    var services = new ServiceCollection();

    services.AddMunin(configure: builder => {
      builder.AddNode(options => options.HostName = HostName);
      builder.AddNode(options => options.HostName = HostName); // should throw exception?
    });

    var serviceProvider = services.BuildServiceProvider();
    var firstNode = serviceProvider.GetKeyedService<IMuninNode>(HostName);
    var secondNode = serviceProvider.GetKeyedService<IMuninNode>(HostName);

    Assert.That(firstNode, Is.Not.Null);
    Assert.That(firstNode.HostName, Is.EqualTo(HostName));
    Assert.That(secondNode, Is.SameAs(firstNode));
  }

  [Test]
  public void AddNode_Multiple_GetServices()
  {
    var services = new ServiceCollection();

    const string HostName0 = "0.munin-node.localhost";
    const string HostName1 = "1.munin-node.localhost";

    services.AddMunin(configure: builder => {
      builder.AddNode(options => options.HostName = HostName0);
      builder.AddNode(options => options.HostName = HostName1);
    });

    var nodes = services.BuildServiceProvider().GetServices<IMuninNode>().ToList();

    Assert.That(nodes, Is.Not.Null);
    Assert.That(nodes.Count, Is.EqualTo(2));
    Assert.That(() => nodes.First(node => node.HostName == HostName0), Throws.Nothing);
    Assert.That(() => nodes.First(node => node.HostName == HostName1), Throws.Nothing);
  }

  [TestCase("_")]
  [TestCase("munin-node.localhost")]
  public void AddNode_Multiple_GetKeyedService(string hostName)
  {
    const string AnotherHostName = "another.munin-node.localhost";
    var services = new ServiceCollection();

    services.AddMunin(configure: builder => {
      builder.AddNode(options => options.HostName = hostName);
      builder.AddNode(options => options.HostName = AnotherHostName);
    });

    var serviceProvider = services.BuildServiceProvider();
    var node = serviceProvider.GetKeyedService<IMuninNode>(serviceKey: hostName);
    var anotherNode = serviceProvider.GetKeyedService<IMuninNode>(serviceKey: AnotherHostName);

    Assert.That(node, Is.Not.Null);
    Assert.That(node.HostName, Is.EqualTo(hostName));
    Assert.That(node, Is.Not.SameAs(anotherNode));
  }

  private class CustomMuninNodeOptions : MuninNodeOptions {
    public string? ExtraOption { get; set; }

    protected override void Configure(MuninNodeOptions baseOptions)
    {
      base.Configure(baseOptions ?? throw new ArgumentNullException(nameof(baseOptions)));

      if (baseOptions is CustomMuninNodeOptions options)
        ExtraOption = options.ExtraOption;
    }
  }

  [Test]
  public void AddNode_CustomOptionsType()
  {
    const string HostName = "munin-node.localhost";
    const string ExtraOptionValue = "foo";

    var services = new ServiceCollection();

    services.AddMunin(configure: builder => {
      builder.AddNode<CustomMuninNodeOptions>(
        configure: options => {
          options.HostName = HostName;
          options.ExtraOption = ExtraOptionValue;
        }
      );
    });

    var serviceProvider = services.BuildServiceProvider();

    var options = serviceProvider
      .GetRequiredService<IOptionsMonitor<CustomMuninNodeOptions>>()
      .Get(name: HostName);

    Assert.That(options.HostName, Is.EqualTo(HostName));
    Assert.That(options.ExtraOption, Is.EqualTo(ExtraOptionValue));
  }
}
