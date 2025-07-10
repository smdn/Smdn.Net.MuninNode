// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using Smdn.Net.MuninNode.Protocol;
using Smdn.Net.MuninNode.Transport;
using Smdn.Net.MuninPlugin;

namespace Smdn.Net.MuninNode;

#pragma warning disable IDE0040
partial class NodeBaseTests {
#pragma warning restore IDE0040
  private class PseudoMuninNode : NodeBase {
    public override string HostName => nameof(PseudoMuninNode);
    public override IPluginProvider PluginProvider { get; }

    public PseudoMuninNode(IMuninProtocolHandlerFactory protocolHandlerFactory)
      : base(
        protocolHandlerFactory: protocolHandlerFactory,
        listenerFactory: new PseudoMuninNodeListenerFactory(),
        accessRule: null,
        logger: null
      )
    {
      PluginProvider = new ReadOnlyCollectionPluginProvider(Array.Empty<IPlugin>());
    }

    public PseudoMuninNode(IAccessRule? accessRule, IReadOnlyCollection<IPlugin>? plugins)
      : base(
        listenerFactory: new PseudoMuninNodeListenerFactory(),
        accessRule: accessRule,
        logger: null
      )
    {
      PluginProvider = new ReadOnlyCollectionPluginProvider(plugins ?? Array.Empty<IPlugin>());
    }

    private class ReadOnlyCollectionPluginProvider : IPluginProvider {
      public IReadOnlyCollection<IPlugin> Plugins { get; }
      [Obsolete] public INodeSessionCallback? SessionCallback => null;

      public ReadOnlyCollectionPluginProvider(IReadOnlyCollection<IPlugin> plugins)
      {
        Plugins = plugins;
      }
    }

    public (
      PseudoMuninNodeClient Client,
      TextWriter ClientRequestWriter,
      TextReader ServerResponseReader
    )
    GetAcceptingClient(CancellationToken cancellationToken)
    {
      if (Listener is null)
        throw new InvalidOperationException("not yet started");
      if (Listener is not PseudoMuninNodeListener pseudoListener)
        throw new InvalidOperationException("listener type mismatch");

      return pseudoListener.GetAcceptingClient(cancellationToken);
    }
  }

  private static Task RunSessionAsync(
    Func<PseudoMuninNode, PseudoMuninNodeClient, TextWriter, TextReader, CancellationToken, Task> action,
    CancellationToken cancellationToken = default
  )
    => RunSessionAsync(
      accessRule: null,
      plugins: null,
      action: action,
      cancellationToken: cancellationToken
    );

  private static async Task RunSessionAsync(
    IAccessRule? accessRule,
    IReadOnlyList<IPlugin>? plugins,
    Func<PseudoMuninNode, PseudoMuninNodeClient, TextWriter, TextReader, CancellationToken, Task> action,
    CancellationToken cancellationToken = default
  )
  {
    await using var node = new PseudoMuninNode(accessRule, plugins);

    await node.StartAsync(cancellationToken);

    var taskAccept = Task.Run(
      async () => await node.AcceptSingleSessionAsync(cancellationToken),
      cancellationToken
    );

    var (c, requestWriter, responseReader) = node.GetAcceptingClient(cancellationToken);
    using var client = c;

    try {
      Assert.That(
        async () => await responseReader.ReadLineAsync(cancellationToken), // receive banner
        Contains.Substring(node.HostName)
      );

      try {
        await action(node, client, requestWriter, responseReader, cancellationToken);
      }
      finally {
        await client.DisconnectAsync(cancellationToken);
      }
    }
    finally {
      await taskAccept;
    }
  }

  [TestCase("\r\n", "# Unknown command.")]
  [TestCase("\n", "# Unknown command.")]
  [TestCase("foo\r\n", "# Unknown command.")]
  [TestCase("foo\n", "# Unknown command.")]
  [TestCase(".\r\n", null)]
  [TestCase(".\n", null)]
  [CancelAfter(3000)]
  public async Task ProcessCommandAsync_EndOfLine(string commandLine, string? expectedResponseLinePrefix, CancellationToken cancellationToken)
  {
    PseudoMuninNodeClient? acceptedClient = null;

    await RunSessionAsync(
      accessRule: null,
      plugins: null,
      async (node, client, writer, reader, ct) => {
        Assert.That(client.IsConnected, Is.True);

        acceptedClient = client;

        await writer.WriteAsync(commandLine.AsMemory(), ct);
        await writer.FlushAsync(ct);

        Assert.That(
          await reader.ReadLineAsync(ct),
          expectedResponseLinePrefix is null
            ? Is.Null
            : Does.StartWith(expectedResponseLinePrefix)
        );
      },
      cancellationToken: cancellationToken
    );

    Assert.That(acceptedClient, Is.Not.Null);
    Assert.That(acceptedClient!.IsConnected, Is.False);
  }

  [TestCase("")]
  [TestCase(".quit")] // not to be confused with `.`
  [TestCase("ca")] // not to be confused with `cap`
  [TestCase("capa")] // not to be confused with `cap`
  [TestCase("unknown")]
  [CancelAfter(3000)]
  public async Task ProcessCommandAsync_UnknownCommand(string command, CancellationToken cancellationToken)
  {
    await RunSessionAsync(async (node, client, writer, reader, ct) => {
      await writer.WriteLineAsync(command, ct);
      await writer.FlushAsync(ct);

      Assert.That(
        await reader.ReadLineAsync(ct),
        Is.EqualTo("# Unknown command. Try cap, list, nodes, config, fetch, version or quit"),
        "line #1"
      );
    }, cancellationToken);
  }

  [TestCase(".")]
  [TestCase("quit")]
  [CancelAfter(3000)]
  public async Task ProcessCommandAsync_QuitCommand(string command, CancellationToken cancellationToken)
  {
    PseudoMuninNodeClient? acceptedClient = null;

    await RunSessionAsync(async (node, client, writer, reader, ct) => {
      acceptedClient = client;

      Assert.That(client.IsConnected, Is.True);

      await writer.WriteLineAsync(command, ct);
      await writer.FlushAsync(ct);

      Assert.That(await reader.ReadLineAsync(ct), Is.Null);
    }, cancellationToken);

    Assert.That(acceptedClient, Is.Not.Null);
    Assert.That(acceptedClient!.IsConnected, Is.False);
  }
}
