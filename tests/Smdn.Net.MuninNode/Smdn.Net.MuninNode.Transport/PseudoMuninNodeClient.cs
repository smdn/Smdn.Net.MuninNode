// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Buffers;
using System.Net;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.MuninNode.Transport;

internal sealed class PseudoMuninNodeClient : IMuninNodeClient {
  public EndPoint? EndPoint { get; } = new IPEndPoint(IPAddress.Loopback, 0); // pseudo endpoint
  public bool IsConnected { get; private set; } = true;

  private readonly PipeWriter responseWriter;
  private readonly PipeReader requestReader;

  public PseudoMuninNodeClient(
    PipeWriter responseWriter,
    PipeReader requestReader
  )
  {
    this.responseWriter = responseWriter ?? throw new ArgumentNullException(nameof(responseWriter));
    this.requestReader = requestReader ?? throw new ArgumentNullException(nameof(requestReader));
  }

  public void Dispose()
  {
  }

  public ValueTask DisposeAsync()
    => default;

  public async ValueTask DisconnectAsync(CancellationToken cancellationToken)
  {
    await responseWriter.CompleteAsync().ConfigureAwait(false);
    await requestReader.CompleteAsync().ConfigureAwait(false);

    IsConnected = false;
  }

  public async ValueTask<int> ReceiveAsync(IBufferWriter<byte> buffer, CancellationToken cancellationToken)
  {
    var writtenByteCount = 0;
    var readResult = await requestReader.ReadAsync(cancellationToken).ConfigureAwait(false);

    foreach (var responseChunk in readResult.Buffer) {
      buffer.Write(responseChunk.Span);

      writtenByteCount += responseChunk.Length;
    }

    return writtenByteCount;
  }

  public async ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
  {
    responseWriter.Write(buffer.Span);

    _ = await responseWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
  }
}
