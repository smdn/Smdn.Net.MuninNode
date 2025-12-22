// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using NUnit.Framework;

using Smdn.Net.MuninNode.Transport;
using Smdn.Net.MuninPlugin;

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

  private class CustomMuninNodeBuilder<TMuninNodeOptions> : MuninNodeBuilder
    where TMuninNodeOptions : MuninNodeOptions, new() {
    private readonly Func<TMuninNodeOptions, IPluginProvider, IMuninNodeListenerFactory?, IServiceProvider, IMuninNode> nodeFactory;

    public CustomMuninNodeBuilder(
      IMuninServiceBuilder serviceBuilder,
      string serviceKey,
      Func<TMuninNodeOptions, IPluginProvider, IMuninNodeListenerFactory?, IServiceProvider, IMuninNode> nodeFactory
    )
      : base(serviceBuilder, serviceKey)
    {
      this.nodeFactory = nodeFactory;
    }

    protected override IMuninNode Build(
      IPluginProvider pluginProvider,
      IMuninNodeListenerFactory? listenerFactory,
      IServiceProvider serviceProvider
    )
      => nodeFactory(
        GetConfiguredOptions<TMuninNodeOptions>(serviceProvider),
        pluginProvider,
        listenerFactory,
        serviceProvider
      );
  }

  private class CustomMuninNode : LocalNode {
    public MuninNodeOptions Options { get; }
    public override string HostName => Options.HostName;
    public override IPluginProvider PluginProvider { get; }

    public CustomMuninNode(
      MuninNodeOptions options,
      IPluginProvider pluginProvider,
      IMuninNodeListenerFactory? listenerFactory
    )
      : base(
        listenerFactory: listenerFactory,
        accessRule: null,
        logger: null
      )
    {
      Options = options;
      PluginProvider = pluginProvider;
    }
  }

  [Test]
  public void AddNode_CustomBuilderType()
  {
    const string HostName = "munin-node.localhost";
    var services = new ServiceCollection();

    services.AddMunin(configure: builder => {
      builder.AddNode<MuninNodeOptions, CustomMuninNodeBuilder<MuninNodeOptions>>(
        configure: options => options.HostName = HostName,
        createBuilder: static (serviceBuilder, serviceKey) => new CustomMuninNodeBuilder<MuninNodeOptions>(
          serviceBuilder: serviceBuilder,
          serviceKey: serviceKey,
          nodeFactory: static (options, pluginProvider, listenerFactory, serviceProvider) => new CustomMuninNode(
            options,
            pluginProvider,
            listenerFactory
          )
        )
      );
    });

    var serviceProvider = services.BuildServiceProvider();

    var node = serviceProvider.GetService<IMuninNode>();

    Assert.That(node, Is.Not.Null);
    Assert.That(node, Is.TypeOf<CustomMuninNode>());
    Assert.That(node.HostName, Is.EqualTo(HostName));

    var keyedNode = serviceProvider.GetKeyedService<IMuninNode>(HostName);

    Assert.That(keyedNode, Is.Not.Null);
    Assert.That(keyedNode, Is.TypeOf<CustomMuninNode>());
    Assert.That(keyedNode.HostName, Is.EqualTo(HostName));
  }

  [Test]
  public void AddNode_CustomBuilderType_AbstractServiceType()
  {
    const string HostName = "munin-node.localhost";
    var services = new ServiceCollection();

    services.AddMunin(configure: builder => {
      builder.AddNode<IMuninNode, CustomMuninNode, MuninNodeOptions, CustomMuninNodeBuilder<MuninNodeOptions>>(
        configure: options => options.HostName = HostName,
        createBuilder: static (serviceBuilder, serviceKey) => new CustomMuninNodeBuilder<MuninNodeOptions>(
          serviceBuilder: serviceBuilder,
          serviceKey: serviceKey,
          nodeFactory: static (options, pluginProvider, listenerFactory, serviceProvider) => new CustomMuninNode(
            options,
            pluginProvider,
            listenerFactory
          )
        )
      );
    });

    var serviceProvider = services.BuildServiceProvider();

    var node = serviceProvider.GetService<IMuninNode>();

    Assert.That(node, Is.Not.Null);
    Assert.That(node, Is.TypeOf<CustomMuninNode>());
    Assert.That(node.HostName, Is.EqualTo(HostName));

    var keyedNode = serviceProvider.GetKeyedService<IMuninNode>(HostName);

    Assert.That(keyedNode, Is.Not.Null);
    Assert.That(keyedNode, Is.TypeOf<CustomMuninNode>());
    Assert.That(keyedNode.HostName, Is.EqualTo(HostName));
  }

  [Test]
  public void AddNode_CustomBuilderType_ConcreteServiceType()
  {
    const string HostName = "munin-node.localhost";
    var services = new ServiceCollection();

    services.AddMunin(configure: builder => {
      builder.AddNode<CustomMuninNode, MuninNodeOptions, CustomMuninNodeBuilder<MuninNodeOptions>>(
        configure: options => options.HostName = HostName,
        createBuilder: static (serviceBuilder, serviceKey) => new CustomMuninNodeBuilder<MuninNodeOptions>(
          serviceBuilder: serviceBuilder,
          serviceKey: serviceKey,
          nodeFactory: static (options, pluginProvider, listenerFactory, serviceProvider) => new CustomMuninNode(
            options,
            pluginProvider,
            listenerFactory
          )
        )
      );
    });

    var serviceProvider = services.BuildServiceProvider();

    var node = serviceProvider.GetService<CustomMuninNode>();

    Assert.That(node, Is.Not.Null);
    Assert.That(node.HostName, Is.EqualTo(HostName));

    var keyedNode = serviceProvider.GetKeyedService<CustomMuninNode>(HostName);

    Assert.That(keyedNode, Is.Not.Null);
    Assert.That(keyedNode.HostName, Is.EqualTo(HostName));
  }

  private class ExtendedCustomMuninNode : CustomMuninNode {
    public ExtendedCustomMuninNode(
      MuninNodeOptions options,
      IPluginProvider pluginProvider,
      IMuninNodeListenerFactory? listenerFactory
    )
      : base(
        options: options,
        pluginProvider: pluginProvider,
        listenerFactory: listenerFactory
      )
    {
    }
  }

  [Test]
  public void AddNode_CustomBuilderType_ConcreteServiceType_ImplementationTypeMismatch()
  {
    const string HostName = "munin-node.localhost";
    var services = new ServiceCollection();

    services.AddMunin(configure: builder => {
      builder.AddNode<ExtendedCustomMuninNode, MuninNodeOptions, CustomMuninNodeBuilder<MuninNodeOptions>>(
        configure: options => options.HostName = HostName,
        createBuilder: static (serviceBuilder, serviceKey) => new CustomMuninNodeBuilder<MuninNodeOptions>(
          serviceBuilder: serviceBuilder,
          serviceKey: serviceKey,
          nodeFactory: static (options, pluginProvider, listenerFactory, serviceProvider) => new CustomMuninNode(
            options,
            pluginProvider,
            listenerFactory
          )
        )
      );
    });

    var serviceProvider = services.BuildServiceProvider();

    Assert.That(
      () => serviceProvider.GetService<ExtendedCustomMuninNode>(),
      Throws.InvalidOperationException
    );
    Assert.That(
      () => serviceProvider.GetService<CustomMuninNode>(),
      Is.Null
    );

    Assert.That(
      () => serviceProvider.GetKeyedService<ExtendedCustomMuninNode>(HostName),
      Throws.InvalidOperationException
    );
    Assert.That(
      () => serviceProvider.GetKeyedService<CustomMuninNode>(HostName),
      Is.Null
    );
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
      builder.AddNode<CustomMuninNodeOptions, CustomMuninNodeBuilder<CustomMuninNodeOptions>>(
        configure: options => {
          options.HostName = HostName;
          options.ExtraOption = ExtraOptionValue;
        },
        createBuilder: static (serviceBuilder, serviceKey) => new CustomMuninNodeBuilder<CustomMuninNodeOptions>(
          serviceBuilder: serviceBuilder,
          serviceKey: serviceKey,
          nodeFactory: static (options, pluginProvider, listenerFactory, serviceProvider) => new CustomMuninNode(
            options,
            pluginProvider,
            listenerFactory
          )
        )
      );
    });

    var serviceProvider = services.BuildServiceProvider();

    var options = serviceProvider
      .GetRequiredService<IOptionsMonitor<CustomMuninNodeOptions>>()
      .Get(name: HostName);

    Assert.That(options.HostName, Is.EqualTo(HostName));
    Assert.That(options.ExtraOption, Is.EqualTo(ExtraOptionValue));

    var node = serviceProvider.GetService<IMuninNode>();

    Assert.That(node, Is.Not.Null);
    Assert.That(node, Is.TypeOf<CustomMuninNode>());

    var customNode = (CustomMuninNode)node;

    Assert.That(customNode.Options, Is.TypeOf<CustomMuninNodeOptions>());
    Assert.That((customNode.Options as CustomMuninNodeOptions)!.ExtraOption, Is.EqualTo(ExtraOptionValue));
    Assert.That(customNode.HostName, Is.EqualTo(HostName));

    var keyedNode = serviceProvider.GetKeyedService<IMuninNode>(HostName);

    Assert.That(keyedNode, Is.Not.Null);
    Assert.That(keyedNode, Is.TypeOf<CustomMuninNode>());

    var customKeyedNode = (CustomMuninNode)keyedNode;

    Assert.That(customKeyedNode.Options, Is.TypeOf<CustomMuninNodeOptions>());
    Assert.That((customKeyedNode.Options as CustomMuninNodeOptions)!.ExtraOption, Is.EqualTo(ExtraOptionValue));
    Assert.That(customKeyedNode.HostName, Is.EqualTo(HostName));
  }
}
