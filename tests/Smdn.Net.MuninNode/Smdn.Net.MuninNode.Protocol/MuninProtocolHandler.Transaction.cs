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
  private void HandleTransactionStartAsync_ITransactionCallback(
    Func<Action<CancellationToken>, Action<CancellationToken>, IPluginProvider> createPluginProvider,
    CancellationToken cancellationToken
  )
    => HandleTransactionAsync_ITransactionCallback(
      createPluginProvider,
      testStartTransaction: true,
      cancellationToken
    );

  private void HandleTransactionEndAsync_ITransactionCallback(
    Func<Action<CancellationToken>, Action<CancellationToken>, IPluginProvider> createPluginProvider,
    CancellationToken cancellationToken
  )
    => HandleTransactionAsync_ITransactionCallback(
      createPluginProvider,
      testStartTransaction: false,
      cancellationToken
    );

  private void HandleTransactionAsync_ITransactionCallback(
    Func<Action<CancellationToken>, Action<CancellationToken>, IPluginProvider> createPluginProvider,
    bool testStartTransaction,
    CancellationToken cancellationToken
  )
  {
    const string HostName = "munin-node.localhost";

    var numberOfOnStartTransactionInvoked = 0;
    var numberOfOnEndTransactionInvoked = 0;

    void OnStartTransaction(CancellationToken ct)
    {
      Assert.That(ct, Is.EqualTo(cancellationToken));
      numberOfOnStartTransactionInvoked++;
    }

    void OnEndTransaction(CancellationToken ct)
    {
      Assert.That(ct, Is.EqualTo(cancellationToken));
      numberOfOnEndTransactionInvoked++;
    }

    var handler = new MuninProtocolHandler(
      profile: new MuninNodeProfile() {
        HostName = HostName,
        PluginProvider = createPluginProvider(
          OnStartTransaction,
          OnEndTransaction
        )
      }
    );
    var client = new PseudoMuninNodeClient();

    Assert.That(
      async () => {
        if (testStartTransaction)
          await handler.HandleTransactionStartAsync(client, cancellationToken);
        else
          await handler.HandleTransactionEndAsync(client, cancellationToken);
      },
      Throws.Nothing
    );
    Assert.That(numberOfOnStartTransactionInvoked, testStartTransaction ? Is.EqualTo(1) : Is.Zero);
    Assert.That(numberOfOnEndTransactionInvoked, testStartTransaction ? Is.Zero : Is.EqualTo(1));
  }

  private class TransactionCallbackPluginProvider : IPluginProvider, ITransactionCallback {
    public IReadOnlyCollection<IPlugin> Plugins => Array.Empty<IPlugin>();
    [Obsolete] public INodeSessionCallback? SessionCallback => null;

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
    => HandleTransactionStartAsync_ITransactionCallback(
      createPluginProvider: static (onStartTransaction, _) => new TransactionCallbackPluginProvider() {
        OnStartTransaction = onStartTransaction,
        OnEndTransaction = static _ => throw new InvalidOperationException("callback was called unexpectedly")
      },
      cancellationToken
    );

  [Test]
  [CancelAfter(1000)]
  public void HandleTransactionEndAsync_ITransactionCallback_IPluginProvider(CancellationToken cancellationToken)
    => HandleTransactionEndAsync_ITransactionCallback(
      createPluginProvider: static (_, onEndTransaction) => new TransactionCallbackPluginProvider() {
        OnStartTransaction = static _ => throw new InvalidOperationException("callback was called unexpectedly"),
        OnEndTransaction = onEndTransaction
      },
      cancellationToken
    );

  [Test]
  [CancelAfter(1000)]
  public void HandleTransactionStartAsync_ITransactionCallback_AggregatePluginProvider(CancellationToken cancellationToken)
    => HandleTransactionStartAsync_ITransactionCallback(
      createPluginProvider: static (onStartTransaction, _) => new AggregatePluginProvider([
        new TransactionCallbackPluginProvider() {
          OnStartTransaction = onStartTransaction,
          OnEndTransaction = static _ => throw new InvalidOperationException("callback was called unexpectedly")
        }
      ]),
      cancellationToken
    );

  [Test]
  [CancelAfter(1000)]
  public void HandleTransactionEndAsync_ITransactionCallback_AggregatePluginProvider(CancellationToken cancellationToken)
    => HandleTransactionEndAsync_ITransactionCallback(
      createPluginProvider: static (_, onEndTransaction) => new AggregatePluginProvider([
        new TransactionCallbackPluginProvider() {
          OnStartTransaction = static _ => throw new InvalidOperationException("callback was called unexpectedly"),
          OnEndTransaction = onEndTransaction
        }
      ]),
      cancellationToken
    );

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
    => HandleTransactionStartAsync_ITransactionCallback(
      createPluginProvider: static (onStartTransaction, _) => new PluginProvider([
        new TransactionCallbackPlugin("plugin") {
          OnStartTransaction = onStartTransaction,
          OnEndTransaction = static _ => throw new InvalidOperationException("callback was called unexpectedly")
        }
      ]),
      cancellationToken
    );

  [Test]
  [CancelAfter(1000)]
  public void HandleTransactionEndAsync_ITransactionCallback_IPlugin(CancellationToken cancellationToken)
    => HandleTransactionEndAsync_ITransactionCallback(
      createPluginProvider: static (_, onEndTransaction) => new PluginProvider([
        new TransactionCallbackPlugin("plugin") {
          OnStartTransaction = static _ => throw new InvalidOperationException("callback was called unexpectedly"),
          OnEndTransaction = onEndTransaction
        }
      ]),
      cancellationToken
    );

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

  private class TransactionCallbackExtendedPlugin : Plugin {
    public Action<CancellationToken>? OnStartTransaction { get; init; }
    public Action<CancellationToken>? OnEndTransaction { get; init; }

    public TransactionCallbackExtendedPlugin()
      : base(
        name: "plugin",
        graphAttributes: new PluginGraphAttributes(
          title: "title",
          category: "test",
          verticalLabel: "test",
          scale: false,
          arguments: "--args"
        ),
        fields: Array.Empty<IPluginField>()
      )
    {
    }

    protected override ValueTask StartTransactionAsync(CancellationToken cancellationToken)
    {
      OnStartTransaction?.Invoke(cancellationToken);

      return default;
    }

    protected override ValueTask EndTransactionAsync(CancellationToken cancellationToken)
    {
      OnEndTransaction?.Invoke(cancellationToken);

      return default;
    }
  }

  [Test]
  [CancelAfter(1000)]
  public void HandleTransactionStartAsync_ITransactionCallback_Plugin(CancellationToken cancellationToken)
    => HandleTransactionStartAsync_ITransactionCallback(
      createPluginProvider: static (onStartTransaction, _) => new PluginProvider([
        new TransactionCallbackExtendedPlugin() {
          OnStartTransaction = onStartTransaction,
          OnEndTransaction = static _ => throw new InvalidOperationException("callback was called unexpectedly")
        }
      ]),
      cancellationToken
    );

  [Test]
  [CancelAfter(1000)]
  public void HandleTransactionEndAsync_ITransactionCallback_Plugin(CancellationToken cancellationToken)
    => HandleTransactionEndAsync_ITransactionCallback(
      createPluginProvider: static (_, onEndTransaction) => new PluginProvider([
        new TransactionCallbackExtendedPlugin() {
          OnStartTransaction = static _ => throw new InvalidOperationException("callback was called unexpectedly"),
          OnEndTransaction = onEndTransaction
        }
      ]),
      cancellationToken
    );
}
