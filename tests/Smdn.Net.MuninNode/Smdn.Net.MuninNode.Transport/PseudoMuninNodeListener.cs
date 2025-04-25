// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.MuninNode.Transport;

internal sealed class PseudoMuninNodeListener : IMuninNodeListener {
  public EndPoint? EndPoint { get; }

  private readonly IMuninNode? node;

  private readonly Pipe pipeFromClientToServer;
  private readonly Pipe pipeFromServerToClient;

  private ManualResetEventSlim acceptingEvent = new(false);
  private PseudoMuninNodeClient? acceptingClient;

  public PseudoMuninNodeListener(EndPoint? endPoint, IMuninNode? node)
  {
    EndPoint = endPoint;

    this.node = node;

    pipeFromClientToServer = new Pipe(
      new PipeOptions(
        useSynchronizationContext: false
      )
    );
    pipeFromServerToClient = new Pipe(
      new PipeOptions(
        useSynchronizationContext: false
      )
    );
  }

  public void Dispose()
  {
    acceptingEvent?.Dispose();
    acceptingEvent = null!;
  }

  public ValueTask DisposeAsync()
  {
    acceptingEvent?.Dispose();
    acceptingEvent = null!;

    return default;
  }

  public ValueTask StartAsync(CancellationToken cancellationToken)
    => default;

  public ValueTask<IMuninNodeClient> AcceptAsync(CancellationToken cancellationToken)
  {
    if (acceptingClient is not null)
      throw new InvalidOperationException("only a single IMuninNodeClient can be accepted.");

    acceptingClient = new PseudoMuninNodeClient(
      responseWriter: pipeFromServerToClient.Writer,
      requestReader: pipeFromClientToServer.Reader
    );

    acceptingEvent.Set();

    return new(acceptingClient);
  }

  public (
    PseudoMuninNodeClient Client,
    TextWriter ClientRequestWriter,
    TextReader ServerResponseReader
  )
  GetAcceptingClient(CancellationToken cancellationToken)
  {
    acceptingEvent.Wait(cancellationToken);

    if (acceptingClient is null)
      throw new InvalidOperationException("not accepting");

    const string CRLF = "\r\n";

    var clientRequestWriter = new StreamWriter(
      pipeFromClientToServer.Writer.AsStream(),
      (node as NodeBase)?.Encoding ?? Encoding.ASCII
    ) {
      NewLine = CRLF,
      AutoFlush = true,
    };
    var serverResponseReader = new StreamReader(
      pipeFromServerToClient.Reader.AsStream(),
      (node as NodeBase)?.Encoding ?? Encoding.ASCII
    ) {
    };

    return (acceptingClient, clientRequestWriter, serverResponseReader);
  }
}
