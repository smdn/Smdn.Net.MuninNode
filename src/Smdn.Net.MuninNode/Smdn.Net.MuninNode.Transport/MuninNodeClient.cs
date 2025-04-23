// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

// TODO: use LoggerMessage.Define
#pragma warning disable CA1848 // For improved performance, use the LoggerMessage delegates instead of calling 'LoggerExtensions.LogInformation(ILogger, string?, params object?[])'

using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Smdn.Net.MuninNode.Transport;

internal sealed class MuninNodeClient : IMuninNodeClient {
  public EndPoint EndPoint => client?.RemoteEndPoint ?? throw new ObjectDisposedException(GetType().FullName);

  private Socket client;
  private readonly ILogger? logger;

  internal MuninNodeClient(
    Socket client,
    ILogger? logger
  )
  {
    this.client = client ?? throw new ArgumentNullException(nameof(client));
    this.logger = logger;
  }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  public async ValueTask DisposeAsync()
  {
    await DisposeAsyncCore().ConfigureAwait(false);

    Dispose(disposing: false);
    GC.SuppressFinalize(this);
  }

  private ValueTask DisposeAsyncCore()
  {
    client?.Close();
    client?.Dispose();
    client = null!;

    return default;
  }

  // protected virtual
  private void Dispose(bool disposing)
  {
    if (!disposing)
      return;

    client?.Close();
    client?.Dispose();
    client = null!;
  }

  public ValueTask DisconnectAsync(CancellationToken cancellationToken)
  {
    if (client is null)
      throw new ObjectDisposedException(GetType().FullName);

    return DisposeAsync();
  }

  public async ValueTask<int> ReceiveAsync(IBufferWriter<byte> buffer, CancellationToken cancellationToken)
  {
    if (client is null)
      throw new ObjectDisposedException(GetType().FullName);

    // holds a reference to the endpoint before the client being disposed
    var remoteEndPoint = client.RemoteEndPoint;

    const int ReceiveChunkSize = 256;
    var totalByteCount = 0;

    for (; ; ) {
      try {
        cancellationToken.ThrowIfCancellationRequested();

        if (!client.Connected)
          return 0;

        var memory = buffer.GetMemory(ReceiveChunkSize);

        var byteCount = await client.ReceiveAsync(
          buffer: memory,
          socketFlags: SocketFlags.None,
          cancellationToken: cancellationToken
        ).ConfigureAwait(false);

        buffer.Advance(byteCount);

        totalByteCount += byteCount;

        if (byteCount < memory.Length)
          return totalByteCount;
      }
      catch (SocketException ex) when (
        ex.SocketErrorCode is
          SocketError.OperationAborted or // ECANCELED (125)
          SocketError.ConnectionReset // ECONNRESET (104)
      ) {
        logger?.LogDebug(
          "[{RemoteEndPoint}] expected socket exception ({NumericSocketErrorCode} {SocketErrorCode})",
          remoteEndPoint,
          (int)ex.SocketErrorCode,
          ex.SocketErrorCode
        );

        break; // expected exception
      }
      catch (ObjectDisposedException) {
        logger?.LogDebug(
          "[{RemoteEndPoint}] socket has been disposed",
          remoteEndPoint
        );

        break; // expected exception
      }
    }

    return totalByteCount;
  }

  public async ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
  {
    if (client is null)
      throw new ObjectDisposedException(GetType().FullName);

    try {
      _ = await client.SendAsync(
        buffer: buffer,
        socketFlags: SocketFlags.None,
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);
    }
    catch (SocketException ex) when (
      ex.SocketErrorCode is
        SocketError.Shutdown or // EPIPE (32)
        SocketError.ConnectionAborted or // WSAECONNABORTED (10053)
        SocketError.OperationAborted or // ECANCELED (125)
        SocketError.ConnectionReset // ECONNRESET (104)
    ) {
      // expected exception in case of disconnection
      throw new MuninNodeClientDisconnectedException(
        message: "client disconnected",
        innerException: ex
      );
    }
  }
}
