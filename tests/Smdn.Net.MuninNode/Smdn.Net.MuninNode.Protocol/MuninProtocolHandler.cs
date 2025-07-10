// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using Smdn.Net.MuninNode.Transport;
using Smdn.Net.MuninPlugin;

namespace Smdn.Net.MuninNode.Protocol;

[TestFixture]
public partial class MuninProtocolHandlerTests {
  private class PluginProvider(IReadOnlyCollection<IPlugin> plugins) : IPluginProvider {
    public IReadOnlyCollection<IPlugin> Plugins { get; } = plugins;
    [Obsolete] public INodeSessionCallback? SessionCallback => null;
  }

  private class EmptyPluginProvider() : PluginProvider(plugins: Array.Empty<IPlugin>()) { }

  public class MuninNodeProfile : IMuninNodeProfile {
    public string HostName { get; init; } = "munin-node.localhost";
    public string Version { get; init; } = "1.0.0";
    public IPluginProvider PluginProvider { get; init; } = new EmptyPluginProvider();
    public Encoding Encoding { get; init; } = Encoding.UTF8;
  }

  private class PseudoMuninNodeClient : IMuninNodeClient {
    private readonly List<string> responses = new(capacity: 2);

    public IReadOnlyList<string> Responses => responses;
    public bool Connected { get; set; }

    public EndPoint? EndPoint => null;

    public void Dispose()
    {
      // nothing to do
    }

    public ValueTask DisposeAsync()
    {
      // nothing to do
      return default;
    }

    public void ClearResponses()
    {
      responses.Clear();
    }

    public ValueTask DisconnectAsync(CancellationToken cancellationToken)
    {
      Connected = false;

      return default;
    }

    public ValueTask<int> ReceiveAsync(IBufferWriter<byte> buffer, CancellationToken cancellationToken)
      => throw new NotSupportedException();

    public ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
      responses.Add(Encoding.UTF8.GetString(buffer.Span));

      return default;
    }
  }

  private static ReadOnlySequence<byte> CreateCommandLineSequence(string commandLine)
    => new(Encoding.UTF8.GetBytes(commandLine));

  [TestCase("munin-node.localhost")]
  [TestCase("_")]
  public void HandleTransactionStartAsync(string hostName)
  {
    var handler = new MuninProtocolHandler(
      profile: new MuninNodeProfile() {
        HostName = hostName
      }
    );
    var client = new PseudoMuninNodeClient();

    Assert.That(
      async () => await handler.HandleTransactionStartAsync(client),
      Throws.Nothing
    );
    Assert.That(client.Responses.Count, Is.EqualTo(1), "must receive banner response");
    Assert.That(client.Responses[0], Does.StartWith("#"), "banner response must be a comment line");
    Assert.That(client.Responses[0], Does.Contain(hostName), "banner response should contain host name");
  }

  [Test]
  public void HandleTransactionStartAsync_ArgumentNull()
  {
    var handler = new MuninProtocolHandler(
      profile: new MuninNodeProfile()
    );
    var client = new PseudoMuninNodeClient();

    Assert.That(
      async () => await handler.HandleTransactionStartAsync(client: null!),
      Throws
        .ArgumentNullException
        .With
        .Property(nameof(ArgumentNullException.ParamName))
        .EqualTo("client")
    );

    Assert.That(client.Responses.Count, Is.Zero);
  }

  [Test]
  public void HandleTransactionStartAsync_CancellationRequested()
  {
    var handler = new MuninProtocolHandler(
      profile: new MuninNodeProfile()
    );
    var client = new PseudoMuninNodeClient();
    var cancellationToken = new CancellationToken(canceled: true);

    Assert.That(
      async () => await handler.HandleTransactionStartAsync(client, cancellationToken),
      Throws
        .InstanceOf<OperationCanceledException>()
        .With
        .Property(nameof(OperationCanceledException.CancellationToken))
        .EqualTo(cancellationToken)
    );

    Assert.That(client.Responses.Count, Is.Zero);
  }

  [Test]
  public void HandleTransactionEndAsync()
  {
    var handler = new MuninProtocolHandler(
      profile: new MuninNodeProfile() {
      }
    );
    var client = new PseudoMuninNodeClient();

    Assert.That(
      async () => await handler.HandleTransactionEndAsync(client),
      Throws.Nothing
    );
    Assert.That(client.Responses.Count, Is.Zero, "must receive nothing");
  }

  [Test]
  public void HandleTransactionEndAsync_ArgumentNull()
  {
    var handler = new MuninProtocolHandler(
      profile: new MuninNodeProfile()
    );
    var client = new PseudoMuninNodeClient();

    Assert.That(
      async () => await handler.HandleTransactionEndAsync(client: null!),
      Throws
        .ArgumentNullException
        .With
        .Property(nameof(ArgumentNullException.ParamName))
        .EqualTo("client")
    );

    Assert.That(client.Responses.Count, Is.Zero);
  }

  [Test]
  public void HandleTransactionEndAsync_CancellationRequested()
  {
    var handler = new MuninProtocolHandler(
      profile: new MuninNodeProfile()
    );
    var client = new PseudoMuninNodeClient();
    var cancellationToken = new CancellationToken(canceled: true);

    Assert.That(
      async () => await handler.HandleTransactionEndAsync(client, cancellationToken),
      Throws
        .InstanceOf<OperationCanceledException>()
        .With
        .Property(nameof(OperationCanceledException.CancellationToken))
        .EqualTo(cancellationToken)
    );

    Assert.That(client.Responses.Count, Is.Zero);
  }

  [Test]
  public void HandleCommandAsync_ArgumentNull()
  {
    var handler = new MuninProtocolHandler(
      profile: new MuninNodeProfile()
    );
    var client = new PseudoMuninNodeClient();

    Assert.That(
      async () => await handler.HandleCommandAsync(client: null!, commandLine: CreateCommandLineSequence(".")),
      Throws
        .ArgumentNullException
        .With
        .Property(nameof(ArgumentNullException.ParamName))
        .EqualTo("client")
    );

    Assert.That(client.Responses.Count, Is.Zero);
  }

  [Test]
  public void HandleCommandAsync_CancellationRequested()
  {
    var handler = new MuninProtocolHandler(
      profile: new MuninNodeProfile()
    );
    var client = new PseudoMuninNodeClient();
    var cancellationToken = new CancellationToken(canceled: true);

    Assert.That(
      async () => await handler.HandleCommandAsync(client, CreateCommandLineSequence("."), cancellationToken),
      Throws
        .InstanceOf<OperationCanceledException>()
        .With
        .Property(nameof(OperationCanceledException.CancellationToken))
        .EqualTo(cancellationToken)
    );

    Assert.That(client.Responses.Count, Is.Zero);
  }

  [Test]
  [CancelAfter(10_000)]
  public async Task HandleCommandAsync_RaceConditionDuringResponse(CancellationToken cancellationToken)
  {
    var numberOfParallelism = Environment.ProcessorCount * 3;
    const int NumberOfRepeat = 100;

    var handler = new MuninProtocolHandler(
      profile: new MuninNodeProfile()
    );
    var clients = Enumerable
      .Range(0, numberOfParallelism)
      .Select(static _ => new PseudoMuninNodeClient())
      .ToList();

    await Parallel.ForEachAsync(
      source: clients,
      parallelOptions: new() {
        MaxDegreeOfParallelism = -1,
        CancellationToken = cancellationToken,
      },
      body: (client, ct) => {
        for (var n = 0; n < NumberOfRepeat; n++) {
          Assert.That(
            async () => await handler.HandleCommandAsync(client, CreateCommandLineSequence("unknown"), ct),
            Throws.Nothing
          );

          Assert.That(client.Responses.Count, Is.EqualTo(1));
          Assert.That(
            client.Responses[0],
            Is.EqualTo("# Unknown command. Try cap, list, nodes, config, fetch, version or quit\n")
          );

          client.ClearResponses();
        }

        return default;
      }
    );
  }
}
