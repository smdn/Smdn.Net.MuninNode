// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Buffers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Smdn.Net.MuninNode.Transport;

[TestFixture]
public class PseudoMuninNodeListenerTests {
  [Test]
  [CancelAfter(3000)]
  public async Task AcceptAsync(CancellationToken cancellationToken)
  {
    var listener = new PseudoMuninNodeListener(endPoint: null, node: null);

    await listener.StartAsync(cancellationToken);

    var client = await listener.AcceptAsync(cancellationToken);

    Assert.That(client, Is.Not.Null);
  }

  [Test]
  [CancelAfter(3000)]
  public async Task GetAcceptingClient(CancellationToken cancellationToken)
  {
    var listener = new PseudoMuninNodeListener(endPoint: null, node: null);

    await listener.StartAsync(cancellationToken);

    _ = await listener.AcceptAsync(cancellationToken);

    var (client, writer, reader) = listener.GetAcceptingClient(cancellationToken);

    Assert.That(client, Is.Not.Null);
    Assert.That(client.IsConnected, Is.True);

    Assert.That(
      async () => await client.DisconnectAsync(cancellationToken),
      Throws.Nothing
    );

    Assert.That(client.IsConnected, Is.False);
  }

  [Test]
  [CancelAfter(3000)]
  public async Task GetAcceptingClient_ResponseReader(CancellationToken cancellationToken)
  {
    var listener = new PseudoMuninNodeListener(endPoint: null, node: null);

    await listener.StartAsync(cancellationToken);

    _ = await listener.AcceptAsync(cancellationToken);
    var (client, _, reader) = listener.GetAcceptingClient(cancellationToken);

    Assert.That(client, Is.Not.Null);

    const string Response = ".";
    const string CRLF = "\r\n";

    await client.SendAsync(
      Encoding.UTF8.GetBytes(Response + CRLF),
      cancellationToken
    );

    Assert.That(
      await reader.ReadLineAsync(cancellationToken),
      Is.EqualTo(Response)
    );
  }

  [Test]
  [CancelAfter(3000)]
  public async Task GetAcceptingClient_RequestWriter(CancellationToken cancellationToken)
  {
    var listener = new PseudoMuninNodeListener(endPoint: null, node: null);

    await listener.StartAsync(cancellationToken);

    _ = await listener.AcceptAsync(cancellationToken);
    var (client, writer, _) = listener.GetAcceptingClient(cancellationToken);

    Assert.That(client, Is.Not.Null);

    const string Request = "quit";
    var expectedRequestLine = Request + "\r\n";

    await writer.WriteLineAsync(Request.AsMemory(), cancellationToken);

    var buffer = new ArrayBufferWriter<byte>();

    Assert.That(
      await client.ReceiveAsync(buffer, cancellationToken),
      Is.EqualTo(expectedRequestLine.Length)
    );
    Assert.That(
      Encoding.UTF8.GetString(buffer.WrittenSpan),
      Is.EqualTo(expectedRequestLine)
    );
  }
}
