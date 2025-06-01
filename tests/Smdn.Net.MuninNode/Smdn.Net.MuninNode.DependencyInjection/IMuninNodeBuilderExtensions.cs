// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

using Smdn.Net.MuninNode.Transport;
using Smdn.Net.MuninPlugin;

namespace Smdn.Net.MuninNode.DependencyInjection;

[TestFixture]
public class IMuninNodeBuilderExtensionsTests {
  [Test]
  public void AddPlugin_IPlugin_ArgumentNull()
  {
    var services = new ServiceCollection();

    services.AddMunin(
      builder => {
        var nodeBuilder = builder.AddNode(option => { });

        Assert.That(
          () => nodeBuilder.AddPlugin(plugin: null!),
          Throws
            .ArgumentNullException
            .With
            .Property(nameof(ArgumentNullException.ParamName))
            .EqualTo("plugin")
        );
      }
    );

    var serviceProvider = services.BuildServiceProvider();

    Assert.That(
      () => serviceProvider.GetService<IMuninNode>(),
      Throws.Nothing
    );
  }

  private class PseudoPlugin(IServiceProvider? serviceProvider = null) : IPlugin {
    public IServiceProvider? ServiceProvider => serviceProvider;
    public string Name => "pseudo-plugin";
    public IPluginGraphAttributes GraphAttributes => throw new NotImplementedException();
    public IPluginDataSource DataSource => throw new NotImplementedException();
    public INodeSessionCallback? SessionCallback => null;
  }

  [Test]
  public void AddPlugin_IPlugin()
  {
    var services = new ServiceCollection();
    var plugin = new PseudoPlugin();

    services.AddMunin(
      builder =>
        builder
          .AddNode(option => { })
          .AddPlugin(plugin)
    );

    var serviceProvider = services.BuildServiceProvider();
    var node = (NodeBase)serviceProvider.GetRequiredService<IMuninNode>();

    Assert.That(node.PluginProvider.Plugins.Count, Is.EqualTo(1));
    Assert.That(node.PluginProvider.Plugins.First(), Is.SameAs(plugin));
  }

  [Test]
  public void AddPlugin_FuncBuildPlugin_ArgumentNull()
  {
    var services = new ServiceCollection();

    services.AddMunin(
      builder => {
        var nodeBuilder = builder.AddNode(option => { });

        Assert.That(
          () => nodeBuilder.AddPlugin(buildPlugin: null!),
          Throws
            .ArgumentNullException
            .With
            .Property(nameof(ArgumentNullException.ParamName))
            .EqualTo("buildPlugin")
        );
      }
    );

    var serviceProvider = services.BuildServiceProvider();

    Assert.That(
      () => serviceProvider.GetService<IMuninNode>(),
      Throws.Nothing
    );
  }

  [Test]
  public void AddPlugin_FuncBuildPlugin()
  {
    var services = new ServiceCollection();
    PseudoPlugin? plugin = null;

    services.AddMunin(
      builder =>
        builder
          .AddNode(option => { })
          .AddPlugin(serviceProvider => {
            plugin = new PseudoPlugin(serviceProvider);

            return plugin;
          })
    );

    var serviceProvider = services.BuildServiceProvider();
    var node = (NodeBase)serviceProvider.GetRequiredService<IMuninNode>();

    Assert.That(node.PluginProvider.Plugins.Count, Is.EqualTo(1));
    Assert.That(node.PluginProvider.Plugins.First(), Is.SameAs(plugin));

    Assert.That(plugin, Is.Not.Null);
    Assert.That(plugin.ServiceProvider, Is.Not.Null);
    Assert.That(plugin.ServiceProvider.GetRequiredService<IMuninNode>, Throws.Nothing);
  }

  [Test]
  public void UsePluginProvider_IPluginProvider_ArgumentNull()
  {
    var services = new ServiceCollection();

    services.AddMunin(
      builder => {
        var nodeBuilder = builder.AddNode(option => { });

        Assert.That(
          () => nodeBuilder.UsePluginProvider(pluginProvider: null!),
          Throws
            .ArgumentNullException
            .With
            .Property(nameof(ArgumentNullException.ParamName))
            .EqualTo("pluginProvider")
        );
      }
    );

    var serviceProvider = services.BuildServiceProvider();

    Assert.That(
      () => serviceProvider.GetService<IMuninNode>(),
      Throws.Nothing
    );
  }

  private class PseudoPluginProvider(IServiceProvider? serviceProvider = null) : IPluginProvider {
    public IServiceProvider? ServiceProvider => serviceProvider;
    public IReadOnlyCollection<IPlugin> Plugins => throw new NotImplementedException();
    public INodeSessionCallback? SessionCallback => throw new NotImplementedException();
  }

  [Test]
  public void UsePluginProvider_IPluginProvider()
  {
    var services = new ServiceCollection();

    services.AddMunin(
      builder =>
        builder
          .AddNode(option => { })
          .UsePluginProvider(new PseudoPluginProvider())
    );

    var serviceProvider = services.BuildServiceProvider();
    var node = (NodeBase)serviceProvider.GetRequiredService<IMuninNode>();

    Assert.That(node.PluginProvider, Is.Not.Null);
    Assert.That(
      () => _ = node.PluginProvider.Plugins,
      Throws.TypeOf<NotImplementedException>()
    );
    Assert.That(
      () => _ = node.PluginProvider.SessionCallback,
      Throws.TypeOf<NotImplementedException>()
    );
  }

  [Test]
  public void UsePluginProvider_FuncBuildPluginProvider_ArgumentNull()
  {
    var services = new ServiceCollection();

    services.AddMunin(
      builder => {
        var nodeBuilder = builder.AddNode(option => { });

        Assert.That(
          () => nodeBuilder.UsePluginProvider(buildPluginProvider: null!),
          Throws
            .ArgumentNullException
            .With
            .Property(nameof(ArgumentNullException.ParamName))
            .EqualTo("buildPluginProvider")
        );
      }
    );

    var serviceProvider = services.BuildServiceProvider();

    Assert.That(
      () => serviceProvider.GetService<IMuninNode>(),
      Throws.Nothing
    );
  }

  [Test]
  public void UsePluginProvider_FuncBuildPluginProvider_IPluginProvider()
  {
    var services = new ServiceCollection();
    PseudoPluginProvider? pluginProvider = null;

    services.AddMunin(
      builder =>
        builder
          .AddNode(option => { })
          .UsePluginProvider(serviceProvider => {
            pluginProvider = new PseudoPluginProvider(serviceProvider);

            return pluginProvider;
          })
    );

    var serviceProvider = services.BuildServiceProvider();
    var node = (NodeBase)serviceProvider.GetRequiredService<IMuninNode>();

    Assert.That(node.PluginProvider, Is.Not.Null);
    Assert.That(node.PluginProvider, Is.SameAs(pluginProvider));

    Assert.That(pluginProvider!.ServiceProvider, Is.Not.Null);
    Assert.That(pluginProvider.ServiceProvider.GetRequiredService<IMuninNode>, Throws.Nothing);
  }

  [Test]
  public void UseSessionCallback_INodeSessionCallback_ArgumentNull()
  {
    var services = new ServiceCollection();

    services.AddMunin(
      builder => {
        var nodeBuilder = builder.AddNode(option => { });

        Assert.That(
          () => nodeBuilder.UseSessionCallback(sessionCallback: null!),
          Throws
            .ArgumentNullException
            .With
            .Property(nameof(ArgumentNullException.ParamName))
            .EqualTo("sessionCallback")
        );
      }
    );

    var serviceProvider = services.BuildServiceProvider();

    Assert.That(
      () => serviceProvider.GetService<IMuninNode>(),
      Throws.Nothing
    );
  }

  private class SessionCallback(IServiceProvider? serviceProvider = null) : INodeSessionCallback {
    public IServiceProvider? ServiceProvider => serviceProvider;

    public ValueTask ReportSessionStartedAsync(string sessionId, CancellationToken cancellationToken)
      => throw new NotImplementedException();

    public ValueTask ReportSessionClosedAsync(string sessionId, CancellationToken cancellationToken)
      => throw new NotImplementedException();
  }

  [Test]
  public void UseSessionCallback_INodeSessionCallback()
  {
    var services = new ServiceCollection();
    var sessionCallback = new SessionCallback();

    services.AddMunin(
      builder =>
        builder
          .AddNode(option => { })
          .UseSessionCallback(sessionCallback)
    );

    var serviceProvider = services.BuildServiceProvider();
    var node = (NodeBase)serviceProvider.GetRequiredService<IMuninNode>();

    Assert.That(node.PluginProvider.SessionCallback, Is.Not.Null);
    Assert.That(node.PluginProvider.SessionCallback, Is.SameAs(sessionCallback));
  }

  private static System.Collections.IEnumerable YieldTestCases_UseSessionCallback_FuncSessionCallback()
  {
    yield return new object[] {
      (MuninNodeBuilder builder) => {
        builder.UseSessionCallback(
          reportSessionStartedAsyncFunc: null,
          reportSessionClosedAsyncFunc: null
        );
      },
      (NodeBase node) => {
        Assert.That(node.PluginProvider.SessionCallback, Is.Not.Null);
        Assert.That(
          async () => await node.PluginProvider.SessionCallback.ReportSessionStartedAsync("started", default),
          Throws.Nothing
        );
        Assert.That(
          async () => await node.PluginProvider.SessionCallback.ReportSessionClosedAsync("closed", default),
          Throws.Nothing
        );
      }
    };

    yield return new object[] {
      (MuninNodeBuilder builder) => {
        builder.UseSessionCallback(
          reportSessionStartedAsyncFunc: (sessionId, ct) => throw new NotImplementedException($"sessionId={sessionId}"),
          reportSessionClosedAsyncFunc: null
        );
      },
      (NodeBase node) => {
        Assert.That(node.PluginProvider.SessionCallback, Is.Not.Null);
        Assert.That(
          async () => await node.PluginProvider.SessionCallback.ReportSessionStartedAsync("session-started", default),
          Throws
            .TypeOf<NotImplementedException>()
            .With
            .Property(nameof(NotImplementedException.Message))
            .EqualTo("sessionId=session-started")
        );
        Assert.That(
          async () => await node.PluginProvider.SessionCallback.ReportSessionClosedAsync("session-closed", default),
          Throws.Nothing
        );
      }
    };

    yield return new object[] {
      (MuninNodeBuilder builder) => {
        builder.UseSessionCallback(
          reportSessionStartedAsyncFunc: null,
          reportSessionClosedAsyncFunc: (sessionId, ct) => throw new NotImplementedException($"sessionId={sessionId}")
        );
      },
      (NodeBase node) => {
        Assert.That(node.PluginProvider.SessionCallback, Is.Not.Null);
        Assert.That(
          async () => await node.PluginProvider.SessionCallback.ReportSessionStartedAsync("session-started", default),
          Throws.Nothing
        );
        Assert.That(
          async () => await node.PluginProvider.SessionCallback.ReportSessionClosedAsync("session-closed", default),
          Throws
            .TypeOf<NotImplementedException>()
            .With
            .Property(nameof(NotImplementedException.Message))
            .EqualTo("sessionId=session-closed")
        );
      }
    };
  }

  [TestCaseSource(nameof(YieldTestCases_UseSessionCallback_FuncSessionCallback))]
  public void UseSessionCallback_FuncSessionCallback(
    Action<MuninNodeBuilder> callUseSessionCallback,
    Action<NodeBase> assertBuiltNode
  )
  {
    var services = new ServiceCollection();

    services.AddMunin(
      builder => callUseSessionCallback((MuninNodeBuilder)builder.AddNode(option => { }))
    );

    var serviceProvider = services.BuildServiceProvider();
    var node = (NodeBase)serviceProvider.GetRequiredService<IMuninNode>();

    assertBuiltNode(node);
  }

  [Test]
  public void UseSessionCallback_FuncBuildSessionCallback_ArgumentNull()
  {
    var services = new ServiceCollection();

    services.AddMunin(
      builder => {
        var nodeBuilder = builder.AddNode(option => { });

        Assert.That(
          () => nodeBuilder.UseSessionCallback(buildSessionCallback: null!),
          Throws
            .ArgumentNullException
            .With
            .Property(nameof(ArgumentNullException.ParamName))
            .EqualTo("buildSessionCallback")
        );
      }
    );

    var serviceProvider = services.BuildServiceProvider();

    Assert.That(
      () => serviceProvider.GetService<IMuninNode>(),
      Throws.Nothing
    );
  }

  [Test]
  public void UseSessionCallback_FuncBuildSessionCallback()
  {
    var services = new ServiceCollection();
    SessionCallback? sessionCallback = null;

    services.AddMunin(
      builder =>
        builder
          .AddNode(option => { })
          .UseSessionCallback(buildSessionCallback: serviceProvider => {
            sessionCallback = new SessionCallback(serviceProvider);

            return sessionCallback;
          })
    );

    var serviceProvider = services.BuildServiceProvider();
    var node = (NodeBase)serviceProvider.GetRequiredService<IMuninNode>();

    Assert.That(node.PluginProvider.SessionCallback, Is.Not.Null);
    Assert.That(node.PluginProvider.SessionCallback, Is.SameAs(sessionCallback));
    Assert.That(sessionCallback!.ServiceProvider, Is.Not.Null);
    Assert.That(sessionCallback!.ServiceProvider!.GetRequiredService<IMuninNode>, Throws.Nothing);
  }

  [Test]
  public void UseListenerFactory_IMuninNodeListenerFactory_ArgumentNull()
  {
    var services = new ServiceCollection();

    services.AddMunin(
      builder => {
        var nodeBuilder = builder.AddNode(option => { });

        Assert.That(
          () => nodeBuilder.UseListenerFactory(listenerFactory: null!),
          Throws
            .ArgumentNullException
            .With
            .Property(nameof(ArgumentNullException.ParamName))
            .EqualTo("listenerFactory")
        );
      }
    );

    var serviceProvider = services.BuildServiceProvider();

    Assert.That(
      () => serviceProvider.GetService<IMuninNode>(),
      Throws.Nothing
    );
  }

  private class PseudoListenerFactoryNotImplementedException : NotImplementedException {
  }

  private class PseudoListenerFactory(IServiceProvider? serviceProvider = null) : IMuninNodeListenerFactory {
    public IServiceProvider? ServiceProvider => serviceProvider;

    public ValueTask<IMuninNodeListener> CreateAsync(
      EndPoint endPoint,
      IMuninNode node,
      CancellationToken cancellationToken
    )
      => throw new PseudoListenerFactoryNotImplementedException();
  }

  [Test]
  [CancelAfter(1000)]
  public void UseListenerFactory_IMuninNodeListenerFactory(CancellationToken cancellationToken)
  {
    var services = new ServiceCollection();

    services.AddMunin(
      builder =>
        builder
          .AddNode(option => { })
          .UseListenerFactory(new PseudoListenerFactory())
    );

    var serviceProvider = services.BuildServiceProvider();
    var node = serviceProvider.GetRequiredService<IMuninNode>();

    Assert.That(
      async () => await node.RunAsync(cancellationToken),
      Throws.TypeOf<PseudoListenerFactoryNotImplementedException>()
    );
  }

  [Test]
  public void UseListenerFactory_FuncBuildListenerFactory_ArgumentNull()
  {
    var services = new ServiceCollection();

    services.AddMunin(
      builder => {
        var nodeBuilder = builder.AddNode(option => { });

        Assert.That(
          () => nodeBuilder.UseListenerFactory(buildListenerFactory: null!),
          Throws
            .ArgumentNullException
            .With
            .Property(nameof(ArgumentNullException.ParamName))
            .EqualTo("buildListenerFactory")
        );
      }
    );

    var serviceProvider = services.BuildServiceProvider();

    Assert.That(
      () => serviceProvider.GetService<IMuninNode>(),
      Throws.Nothing
    );
  }

  [Test]
  [CancelAfter(1000)]
  public void UseListenerFactory_FuncBuildListenerFactory(CancellationToken cancellationToken)
  {
    var services = new ServiceCollection();
    PseudoListenerFactory? listenerFactory = null;

    services.AddMunin(
      builder =>
        builder
          .AddNode(option => { })
          .UseListenerFactory(serviceProvider => {
            listenerFactory = new PseudoListenerFactory(serviceProvider);

            return listenerFactory;
          })
    );

    var serviceProvider = services.BuildServiceProvider();
    var node = serviceProvider.GetRequiredService<IMuninNode>();

    Assert.That(
      async () => await node.RunAsync(cancellationToken),
      Throws.TypeOf<PseudoListenerFactoryNotImplementedException>()
    );

    Assert.That(listenerFactory!.ServiceProvider, Is.Not.Null);
    Assert.That(listenerFactory!.ServiceProvider!.GetRequiredService<IMuninNode>, Throws.Nothing);
  }

  [Test]
  public void UseListenerFactory_FuncCreateListenerAsync_ArgumentNull()
  {
    var services = new ServiceCollection();

    services.AddMunin(
      builder => {
        var nodeBuilder = builder.AddNode(option => { });

        Assert.That(
          () => nodeBuilder.UseListenerFactory(createListenerAsyncFunc: null!),
          Throws
            .ArgumentNullException
            .With
            .Property(nameof(ArgumentNullException.ParamName))
            .EqualTo("createListenerAsyncFunc")
        );
      }
    );

    var serviceProvider = services.BuildServiceProvider();

    Assert.That(
      () => serviceProvider.GetService<IMuninNode>(),
      Throws.Nothing
    );
  }

  [Test]
  [CancelAfter(1000)]
  public void UseListenerFactory_FuncCreateListenerAsync(CancellationToken cancellationToken)
  {
    var services = new ServiceCollection();
    const string Message = nameof(UseListenerFactory_FuncCreateListenerAsync);

    services.AddMunin(
      builder =>
        builder
          .AddNode(option => { })
          .UseListenerFactory(createListenerAsyncFunc: (_, _, _, _) => throw new NotSupportedException(Message))
    );

    var serviceProvider = services.BuildServiceProvider();
    var node = serviceProvider.GetRequiredService<IMuninNode>();

    Assert.That(
      async () => await node.RunAsync(cancellationToken),
      Throws
        .TypeOf<NotSupportedException>()
        .With
        .Property(nameof(NotSupportedException.Message))
        .EqualTo(Message)
    );
  }
}
