// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Smdn.Net.MuninPlugin;

[TestFixture]
public class AggregatePluginProviderTests {
  private class PseudoPlugin(string name) : IPlugin {
    public string Name { get; } = name;
    public IPluginGraphAttributes GraphAttributes => throw new NotImplementedException();
    public IPluginDataSource DataSource => throw new NotImplementedException();
    public INodeSessionCallback? SessionCallback => null;
  }

  private class PseudoPluginProvider(IReadOnlyCollection<IPlugin> plugins) : IPluginProvider, INodeSessionCallback {
    public IReadOnlyCollection<IPlugin> Plugins => plugins;
    public INodeSessionCallback? SessionCallback => this;

    public int NumberOfInvocationOfReportSessionStartedAsync = 0;
    public int NumberOfInvocationOfReportSessionClosedAsync = 0;

    public ValueTask ReportSessionStartedAsync(string sessionId, CancellationToken cancellationToken)
    {
      NumberOfInvocationOfReportSessionStartedAsync++;

      return default;
    }

    public ValueTask ReportSessionClosedAsync(string sessionId, CancellationToken cancellationToken)
    {
      NumberOfInvocationOfReportSessionClosedAsync++;

      return default;
    }
  }

  [Test]
  public void Plugins_ZeroPluginProviders()
  {
    var pluginProviderCollections = new AggregatePluginProvider([]);

    Assert.That(pluginProviderCollections.Count, Is.Zero);
    Assert.That(pluginProviderCollections.Plugins.Count, Is.Zero);
  }

  [Test]
  public void Plugins_SinglePluginProvider_ZeroPlugins()
  {
    var pluginProviderCollections = new AggregatePluginProvider([
      new PseudoPluginProvider([])
    ]);

    Assert.That(pluginProviderCollections.Count, Is.EqualTo(1));
    Assert.That(pluginProviderCollections.Plugins.Count, Is.Zero);
  }

  [Test]
  public void Plugins_SinglePluginProvider_SinglePlugin()
  {
    var pluginProviderCollections = new AggregatePluginProvider([
      new PseudoPluginProvider([
        new PseudoPlugin("#1")
      ])
    ]);

    Assert.That(pluginProviderCollections.Count, Is.EqualTo(1));
    Assert.That(pluginProviderCollections.Plugins.Count, Is.EqualTo(1));
    Assert.That(pluginProviderCollections.Plugins.Select(static p => p.Name), Is.EquivalentTo(["#1"]));
  }

  [Test]
  public void Plugins_SinglePluginProvider_MultiplePlugins()
  {
    var pluginProviderCollections = new AggregatePluginProvider([
      new PseudoPluginProvider([
        new PseudoPlugin("#1"),
        new PseudoPlugin("#2"),
      ])
    ]);

    Assert.That(pluginProviderCollections.Count, Is.EqualTo(1));
    Assert.That(pluginProviderCollections.Plugins.Count, Is.EqualTo(2));
    Assert.That(pluginProviderCollections.Plugins.Select(static p => p.Name), Is.EquivalentTo(["#1", "#2"]));
  }

  [Test]
  public void Plugins_MultiplePluginProviders_ZeroPlugins()
  {
    var pluginProviderCollections = new AggregatePluginProvider([
      new PseudoPluginProvider([]),
      new PseudoPluginProvider([])
    ]);

    Assert.That(pluginProviderCollections.Count, Is.EqualTo(2));
    Assert.That(pluginProviderCollections.Plugins.Count, Is.Zero);
  }

  [Test]
  public void Plugins_MultiplePluginProviders_SinglePlugin()
  {
    var pluginProviderCollections = new AggregatePluginProvider([
      new PseudoPluginProvider([
        new PseudoPlugin("#1")
      ]),
      new PseudoPluginProvider([])
    ]);

    Assert.That(pluginProviderCollections.Count, Is.EqualTo(2));
    Assert.That(pluginProviderCollections.Plugins.Count, Is.EqualTo(1));
    Assert.That(pluginProviderCollections.Plugins.Select(static p => p.Name), Is.EquivalentTo(["#1"]));
  }

  [Test]
  public void Plugins_MultiplePluginProviders_MultiplePlugins()
  {
    var pluginProviderCollections = new AggregatePluginProvider([
      new PseudoPluginProvider([
        new PseudoPlugin("#1"),
      ]),
      new PseudoPluginProvider([
        new PseudoPlugin("#2"),
        new PseudoPlugin("#3"),
      ])
    ]);

    Assert.That(pluginProviderCollections.Count, Is.EqualTo(2));
    Assert.That(pluginProviderCollections.Plugins.Count, Is.EqualTo(3));
    Assert.That(pluginProviderCollections.Plugins.Select(static p => p.Name), Is.EquivalentTo(["#1", "#2", "#3"]));
  }

  [Test]
  public void INodeSessionCallback_ReportSessionStartedAsync()
  {
    var provider1 = new PseudoPluginProvider([]);
    var provider2 = new PseudoPluginProvider([]);
    var pluginProviderCollections = new AggregatePluginProvider([
      provider1,
      provider2,
    ]);

    INodeSessionCallback sessionCallback = pluginProviderCollections;

    Assert.That(
      async () => await sessionCallback.ReportSessionStartedAsync(string.Empty, default),
      Throws.Nothing
    );

    Assert.That(provider1.NumberOfInvocationOfReportSessionStartedAsync, Is.EqualTo(1));
    Assert.That(provider1.NumberOfInvocationOfReportSessionClosedAsync, Is.Zero);

    Assert.That(provider2.NumberOfInvocationOfReportSessionStartedAsync, Is.EqualTo(1));
    Assert.That(provider2.NumberOfInvocationOfReportSessionClosedAsync, Is.Zero);
  }

  [Test]
  public void INodeSessionCallback_ReportSessionClosedAsync()
  {
    var provider1 = new PseudoPluginProvider([]);
    var provider2 = new PseudoPluginProvider([]);
    var pluginProviderCollections = new AggregatePluginProvider([
      provider1,
      provider2,
    ]);

    INodeSessionCallback sessionCallback = pluginProviderCollections;

    Assert.That(
      async () => await sessionCallback.ReportSessionClosedAsync(string.Empty, default),
      Throws.Nothing
    );

    Assert.That(provider1.NumberOfInvocationOfReportSessionStartedAsync, Is.Zero);
    Assert.That(provider1.NumberOfInvocationOfReportSessionClosedAsync, Is.EqualTo(1));

    Assert.That(provider2.NumberOfInvocationOfReportSessionStartedAsync, Is.Zero);
    Assert.That(provider2.NumberOfInvocationOfReportSessionClosedAsync, Is.EqualTo(1));
  }

  [Test]
  public void INodeSessionCallback_ReportSessionStartedAsync_CancellationRequested()
  {
    var provider1 = new PseudoPluginProvider([]);
    var provider2 = new PseudoPluginProvider([]);
    var pluginProviderCollections = new AggregatePluginProvider([
      provider1,
      provider2,
    ]);

    INodeSessionCallback sessionCallback = pluginProviderCollections;

    using var cts = new CancellationTokenSource(0);

    Assert.That(
      async () => await sessionCallback.ReportSessionStartedAsync(string.Empty, cts.Token),
      Throws.InstanceOf<OperationCanceledException>()
    );

    Assert.That(provider1.NumberOfInvocationOfReportSessionStartedAsync, Is.Zero);
    Assert.That(provider1.NumberOfInvocationOfReportSessionClosedAsync, Is.Zero);

    Assert.That(provider2.NumberOfInvocationOfReportSessionStartedAsync, Is.Zero);
    Assert.That(provider2.NumberOfInvocationOfReportSessionClosedAsync, Is.Zero);
  }

  [Test]
  public void INodeSessionCallback_ReportSessionClosedAsync_CancellationRequested()
  {
    var provider1 = new PseudoPluginProvider([]);
    var provider2 = new PseudoPluginProvider([]);
    var pluginProviderCollections = new AggregatePluginProvider([
      provider1,
      provider2,
    ]);

    INodeSessionCallback sessionCallback = pluginProviderCollections;

    using var cts = new CancellationTokenSource(0);

    Assert.That(
      async () => await sessionCallback.ReportSessionClosedAsync(string.Empty, cts.Token),
      Throws.InstanceOf<OperationCanceledException>()
    );

    Assert.That(provider1.NumberOfInvocationOfReportSessionStartedAsync, Is.Zero);
    Assert.That(provider1.NumberOfInvocationOfReportSessionClosedAsync, Is.Zero);

    Assert.That(provider2.NumberOfInvocationOfReportSessionStartedAsync, Is.Zero);
    Assert.That(provider2.NumberOfInvocationOfReportSessionClosedAsync, Is.Zero);
  }
}
