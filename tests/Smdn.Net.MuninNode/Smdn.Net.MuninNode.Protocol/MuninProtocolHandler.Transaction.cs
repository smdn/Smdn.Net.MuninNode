// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using Smdn.Net.MuninPlugin;

namespace Smdn.Net.MuninNode.Protocol;

#pragma warning disable IDE0040
partial class MuninProtocolHandlerTests {
#pragma warning restore IDE0040
  // TODO: Plugin
  // TODO: AggregatePluginProvider
  private class TransactionCallbackPluginProvider : IPluginProvider, ITransactionCallback {
    public IReadOnlyCollection<IPlugin> Plugins => Array.Empty<IPlugin>();
    [Obsolete]  public INodeSessionCallback? SessionCallback => null;

    public Action<CancellationToken>? OnStartTransaction { get; init; }
    public Action<CancellationToken>? OnEndTransaction { get; init; }

    public ValueTask StartTransactionAsync(CancellationToken cancellationToken)
    {
      OnStartTransaction?.Invoke(cancellationToken);

      return default;
    }

    public ValueTask EndTransactionAsync(CancellationToken cancellationToken)
    {
      OnEndTransaction?.Invoke(cancellationToken);

      return default;
    }
  }

  [Test]
  [CancelAfter(1000)]
  public void HandleTransactionStartAsync_ITransactionCallback_IPluginProvider(CancellationToken cancellationToken)
  {
    const string HostName = "munin-node.localhost";

    var numberOfOnStartTransactionInvoked = 0;
    var numberOfOnEndTransactionInvoked = 0;
    var handler = new MuninProtocolHandler(
      profile: new MuninNodeProfile() {
        HostName = HostName,
        PluginProvider = new TransactionCallbackPluginProvider() {
          OnStartTransaction = ct => {
            Assert.That(ct, Is.EqualTo(cancellationToken));
            numberOfOnStartTransactionInvoked++;
          },
          OnEndTransaction = ct => {
            Assert.That(ct, Is.EqualTo(cancellationToken));
            numberOfOnEndTransactionInvoked++;
          }
        }
      }
    );
    var client = new PseudoMuninNodeClient();

    Assert.That(
      async () => await handler.HandleTransactionStartAsync(client, cancellationToken),
      Throws.Nothing
    );
    Assert.That(numberOfOnStartTransactionInvoked, Is.EqualTo(1));
    Assert.That(numberOfOnEndTransactionInvoked, Is.Zero);
  }

  [Test]
  [CancelAfter(1000)]
  public void HandleTransactionEndAsync_ITransactionCallback_IPluginProvider(CancellationToken cancellationToken)
  {
    const string HostName = "munin-node.localhost";

    var numberOfOnStartTransactionInvoked = 0;
    var numberOfOnEndTransactionInvoked = 0;
    var handler = new MuninProtocolHandler(
      profile: new MuninNodeProfile() {
        HostName = HostName,
        PluginProvider = new TransactionCallbackPluginProvider() {
          OnStartTransaction = ct => {
            Assert.That(ct, Is.EqualTo(cancellationToken));
            numberOfOnStartTransactionInvoked++;
          },
          OnEndTransaction = ct => {
            Assert.That(ct, Is.EqualTo(cancellationToken));
            numberOfOnEndTransactionInvoked++;
          }
        }
      }
    );
    var client = new PseudoMuninNodeClient();

    Assert.That(
      async () => await handler.HandleTransactionEndAsync(client, cancellationToken),
      Throws.Nothing
    );
    Assert.That(numberOfOnStartTransactionInvoked, Is.Zero);
    Assert.That(numberOfOnEndTransactionInvoked, Is.EqualTo(1));
  }

  private class TransactionCallbackPlugin(string name) : IPlugin, ITransactionCallback {
    public string Name => name;
    public IPluginGraphAttributes GraphAttributes => throw new NotImplementedException();
    public IPluginDataSource DataSource => throw new NotImplementedException();
    [Obsolete] public INodeSessionCallback? SessionCallback => throw new NotImplementedException();

    public Action<CancellationToken>? OnStartTransaction { get; init; }
    public Action<CancellationToken>? OnEndTransaction { get; init; }

    public ValueTask StartTransactionAsync(CancellationToken cancellationToken)
    {
      OnStartTransaction?.Invoke(cancellationToken);

      return default;
    }

    public ValueTask EndTransactionAsync(CancellationToken cancellationToken)
    {
      OnEndTransaction?.Invoke(cancellationToken);

      return default;
    }
  }

  [Test]
  [CancelAfter(1000)]
  public void HandleTransactionStartAsync_ITransactionCallback_IPlugin(CancellationToken cancellationToken)
  {
    const string HostName = "munin-node.localhost";

    var numberOfOnStartTransactionInvoked = 0;
    var numberOfOnEndTransactionInvoked = 0;
    var handler = new MuninProtocolHandler(
      profile: new MuninNodeProfile() {
        HostName = HostName,
        PluginProvider = new PluginProvider([
          new TransactionCallbackPlugin("plugin") {
            OnStartTransaction = ct => {
              Assert.That(ct, Is.EqualTo(cancellationToken));
              numberOfOnStartTransactionInvoked++;
            },
            OnEndTransaction = ct => {
              Assert.That(ct, Is.EqualTo(cancellationToken));
              numberOfOnEndTransactionInvoked++;
            }
          }
        ])
      }
    );
    var client = new PseudoMuninNodeClient();

    Assert.That(
      async () => await handler.HandleTransactionStartAsync(client, cancellationToken),
      Throws.Nothing
    );
    Assert.That(numberOfOnStartTransactionInvoked, Is.EqualTo(1));
    Assert.That(numberOfOnEndTransactionInvoked, Is.Zero);
  }

  [Test]
  [CancelAfter(1000)]
  public void HandleTransactionEndAsync_ITransactionCallback_IPlugin(CancellationToken cancellationToken)
  {
    const string HostName = "munin-node.localhost";

    var numberOfOnStartTransactionInvoked = 0;
    var numberOfOnEndTransactionInvoked = 0;
    var handler = new MuninProtocolHandler(
      profile: new MuninNodeProfile() {
        HostName = HostName,
        PluginProvider = new PluginProvider([
          new TransactionCallbackPlugin("plugin") {
            OnStartTransaction = ct => {
              Assert.That(ct, Is.EqualTo(cancellationToken));
              numberOfOnStartTransactionInvoked++;
            },
            OnEndTransaction = ct => {
              Assert.That(ct, Is.EqualTo(cancellationToken));
              numberOfOnEndTransactionInvoked++;
            }
          }
        ])
      }
    );
    var client = new PseudoMuninNodeClient();

    Assert.That(
      async () => await handler.HandleTransactionEndAsync(client, cancellationToken),
      Throws.Nothing
    );
    Assert.That(numberOfOnStartTransactionInvoked, Is.Zero);
    Assert.That(numberOfOnEndTransactionInvoked, Is.EqualTo(1));
  }

  private class TransactionCallbackMultigraphPlugin(string name, IPlugin plugin) : IMultigraphPlugin, ITransactionCallback {
    public string Name => name;
    public IPluginGraphAttributes GraphAttributes => throw new NotImplementedException();
    public IPluginDataSource DataSource => throw new NotImplementedException();
    [Obsolete] public INodeSessionCallback? SessionCallback => throw new NotImplementedException();
    public IReadOnlyCollection<IPlugin> Plugins { get; } = [plugin];

    public Action<CancellationToken>? OnStartTransaction { get; init; }
    public Action<CancellationToken>? OnEndTransaction { get; init; }

    public ValueTask StartTransactionAsync(CancellationToken cancellationToken)
    {
      OnStartTransaction?.Invoke(cancellationToken);

      return default;
    }

    public ValueTask EndTransactionAsync(CancellationToken cancellationToken)
    {
      OnEndTransaction?.Invoke(cancellationToken);

      return default;
    }
  }

  [Test]
  [CancelAfter(1000)]
  public void HandleTransactionStartAsync_ITransactionCallback_IMultigraphPlugin(
    [Values] bool multigraph,
    CancellationToken cancellationToken
  )
  {
    const string HostName = "munin-node.localhost";

    var numberOfMultigraphPluginOnStartTransactionInvoked = 0;
    var numberOfMultigraphPluginOnEndTransactionInvoked = 0;
    var numberOfPluginOnStartTransactionInvoked = 0;
    var numberOfPluginOnEndTransactionInvoked = 0;
    var handler = new MuninProtocolHandler(
      profile: new MuninNodeProfile() {
        HostName = HostName,
        PluginProvider = new PluginProvider([
          new TransactionCallbackMultigraphPlugin(
            "multigraph_plugin",
            new TransactionCallbackPlugin("plugin") {
              OnStartTransaction = ct => {
                Assert.That(ct, Is.EqualTo(cancellationToken));
                numberOfPluginOnStartTransactionInvoked++;
              },
              OnEndTransaction = ct => {
                Assert.That(ct, Is.EqualTo(cancellationToken));
                numberOfPluginOnEndTransactionInvoked++;
              }
            }
          ) {
            OnStartTransaction = ct => {
              Assert.That(ct, Is.EqualTo(cancellationToken));
              numberOfMultigraphPluginOnStartTransactionInvoked++;
            },
            OnEndTransaction = ct => {
              Assert.That(ct, Is.EqualTo(cancellationToken));
              numberOfMultigraphPluginOnEndTransactionInvoked++;
            }
          }
        ])
      }
    );
    var client = new PseudoMuninNodeClient();

    if (multigraph) {
      Assert.That(
        async () => await handler.HandleCommandAsync(
          client,
          commandLine: CreateCommandLineSequence("cap multigraph")
        ),
        Throws.Nothing
      );
    }

    Assert.That(
      async () => await handler.HandleTransactionStartAsync(client, cancellationToken),
      Throws.Nothing
    );
    Assert.That(numberOfMultigraphPluginOnStartTransactionInvoked, multigraph ? Is.EqualTo(1) : Is.Zero);
    Assert.That(numberOfMultigraphPluginOnEndTransactionInvoked, Is.Zero);
    Assert.That(numberOfPluginOnStartTransactionInvoked, multigraph ? Is.Zero : Is.EqualTo(1));
    Assert.That(numberOfPluginOnEndTransactionInvoked, Is.Zero);
  }

  [Test]
  [CancelAfter(1000)]
  public void HandleTransactionEndAsync_ITransactionCallback_IMultigraphPlugin(
    [Values] bool multigraph,
    CancellationToken cancellationToken
  )
  {
    const string HostName = "munin-node.localhost";

    var numberOfMultigraphPluginOnStartTransactionInvoked = 0;
    var numberOfMultigraphPluginOnEndTransactionInvoked = 0;
    var numberOfPluginOnStartTransactionInvoked = 0;
    var numberOfPluginOnEndTransactionInvoked = 0;
    var handler = new MuninProtocolHandler(
      profile: new MuninNodeProfile() {
        HostName = HostName,
        PluginProvider = new PluginProvider([
          new TransactionCallbackMultigraphPlugin(
            "multigraph_plugin",
            new TransactionCallbackPlugin("plugin") {
              OnStartTransaction = ct => {
                Assert.That(ct, Is.EqualTo(cancellationToken));
                numberOfPluginOnStartTransactionInvoked++;
              },
              OnEndTransaction = ct => {
                Assert.That(ct, Is.EqualTo(cancellationToken));
                numberOfPluginOnEndTransactionInvoked++;
              }
            }
          ) {
            OnStartTransaction = ct => {
              Assert.That(ct, Is.EqualTo(cancellationToken));
              numberOfMultigraphPluginOnStartTransactionInvoked++;
            },
            OnEndTransaction = ct => {
              Assert.That(ct, Is.EqualTo(cancellationToken));
              numberOfMultigraphPluginOnEndTransactionInvoked++;
            }
          }
        ])
      }
    );
    var client = new PseudoMuninNodeClient();

    if (multigraph) {
      Assert.That(
        async () => await handler.HandleCommandAsync(
          client,
          commandLine: CreateCommandLineSequence("cap multigraph")
        ),
        Throws.Nothing
      );
    }

    Assert.That(
      async () => await handler.HandleTransactionEndAsync(client, cancellationToken),
      Throws.Nothing
    );
    Assert.That(numberOfMultigraphPluginOnStartTransactionInvoked, Is.Zero);
    Assert.That(numberOfMultigraphPluginOnEndTransactionInvoked, multigraph ? Is.EqualTo(1) : Is.Zero);
    Assert.That(numberOfPluginOnStartTransactionInvoked, Is.Zero);
    Assert.That(numberOfPluginOnEndTransactionInvoked, multigraph ? Is.Zero : Is.EqualTo(1));
  }
}
