// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using NUnit.Framework;

using Smdn.Net.MuninNode.DependencyInjection;
using Smdn.Net.MuninNode.Transport;
using Smdn.Net.MuninPlugin;

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
    public CustomMuninNodeOptions Options { get; }
    public override string HostName => Options.HostName;
    public override IPluginProvider PluginProvider { get; }

    public string? ExtraOption => Options.ExtraOption;

    public CustomMuninNode(
      CustomMuninNodeOptions options,
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

  private class CustomMuninNodeOptions : MuninNodeOptions {
    public string? ExtraOption { get; set; }

    protected override void Configure(MuninNodeOptions baseOptions)
    {
      base.Configure(baseOptions ?? throw new ArgumentNullException(nameof(baseOptions)));

      if (baseOptions is CustomMuninNodeOptions options)
        ExtraOption = options.ExtraOption;
    }
  }

  private class CustomMuninNodeBackgroundService(CustomMuninNode node) : MuninNodeBackgroundService(node) {
    public string? ExtraOption => node.ExtraOption;
  }

  [Test]
  public void AddHostedMuninNodeService_CustomBackgroundServiceType()
  {
    const string HostName = "munin-node.localhost";
    const string ExtraOptionValue = "foo";

    var services = new ServiceCollection();

    services.AddHostedMuninNodeService<
      CustomMuninNodeBackgroundService,
      CustomMuninNode,
      CustomMuninNode,
      CustomMuninNodeOptions,
      CustomMuninNodeBuilder<CustomMuninNodeOptions>
    >(
      configureNode: options => {
        options.HostName = HostName;
        options.ExtraOption = ExtraOptionValue;
      },
      createNodeBuilder: static (serviceBuilder, serviceKey) => new CustomMuninNodeBuilder<CustomMuninNodeOptions>(
        serviceBuilder: serviceBuilder,
        serviceKey: serviceKey,
        nodeFactory: static (options, pluginProvider, listenerFactory, serviceProvider) => new CustomMuninNode(
          options,
          pluginProvider,
          listenerFactory
        )
      ),
      buildNode: nodeBuilder => { }
    );

    var serviceProvider = services.BuildServiceProvider();

    var options = serviceProvider
      .GetRequiredService<IOptionsMonitor<CustomMuninNodeOptions>>()
      .Get(name: HostName);

    Assert.That(options.HostName, Is.EqualTo(HostName));
    Assert.That(options.ExtraOption, Is.EqualTo(ExtraOptionValue));

    var muninNodeBackgroundService = serviceProvider.GetRequiredService<IHostedService>();

    Assert.That(muninNodeBackgroundService, Is.TypeOf<CustomMuninNodeBackgroundService>());
    Assert.That(
      (muninNodeBackgroundService as CustomMuninNodeBackgroundService)!.ExtraOption,
      Is.EqualTo(ExtraOptionValue)
    );
  }

  [Test]
  public void AddHostedMuninNodeService_CustomBackgroundServiceType_WithIMuninServiceBuilder()
  {
    const string HostName = "munin-node.localhost";
    const string ExtraOptionValue = "foo";

    var services = new ServiceCollection();

    services.AddHostedMuninNodeService<
      CustomMuninNodeBackgroundService,
      CustomMuninNodeBuilder<CustomMuninNodeOptions>
    >(
      buildMunin: muninServiceBuilder => muninServiceBuilder.AddNode<
        CustomMuninNode,
        CustomMuninNodeOptions,
        CustomMuninNodeBuilder<CustomMuninNodeOptions>
      >(
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
      )
    );

    var serviceProvider = services.BuildServiceProvider();

    var options = serviceProvider
      .GetRequiredService<IOptionsMonitor<CustomMuninNodeOptions>>()
      .Get(name: HostName);

    Assert.That(options.HostName, Is.EqualTo(HostName));
    Assert.That(options.ExtraOption, Is.EqualTo(ExtraOptionValue));

    var muninNodeBackgroundService = serviceProvider.GetRequiredService<IHostedService>();

    Assert.That(muninNodeBackgroundService, Is.TypeOf<CustomMuninNodeBackgroundService>());
    Assert.That(
      (muninNodeBackgroundService as CustomMuninNodeBackgroundService)!.ExtraOption,
      Is.EqualTo(ExtraOptionValue)
    );
  }
}
