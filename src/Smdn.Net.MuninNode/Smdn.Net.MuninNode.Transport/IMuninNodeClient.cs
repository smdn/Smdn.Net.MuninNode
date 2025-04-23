// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Buffers;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.MuninNode.Transport;

/// <summary>
/// Provides an interface that abstracts the client implementation of
/// the transport layer that connects to the <c>Munin-Node</c>.
/// </summary>
/// <seealso cref="IMuninNodeServer"/>
public interface IMuninNodeClient : IDisposable, IAsyncDisposable {
  /// <summary>
  /// Gets the <see cref="EndPoint"/> that is bound with this instance.
  /// </summary>
  /// <exception cref="ObjectDisposedException">The client has been disposed.</exception>
  /// <value>
  /// <see langword="null"/> if this client does not have <see cref="EndPoint"/>.
  /// </value>
  /// <remarks>
  /// The value of this property will be used by the <see cref="IAccessRule"/> interface
  /// to determine if the client is accessible.
  /// </remarks>
  /// <seealso cref="IAccessRule.IsAcceptable(IPEndPoint)"/>
  EndPoint? EndPoint { get; }

  /// <summary>
  /// Disconnects the active connection.
  /// </summary>
  /// <param name="cancellationToken">
  /// The <see cref="CancellationToken"/> to monitor for cancellation requests.
  /// </param>
  /// <exception cref="ObjectDisposedException">The client has been disposed.</exception>
  /// <returns>
  /// The <see cref="ValueTask"/> that represents the asynchronous operation,
  /// disconnecting the active connection.
  /// </returns>
  ValueTask DisconnectAsync(CancellationToken cancellationToken);

  /// <summary>
  /// Waits to receive a response from the server and writes the received data into a buffer.
  /// </summary>
  /// <param name="buffer">
  /// The <see cref="IBufferWriter{Byte}"/> for writing received data.
  /// </param>
  /// <param name="cancellationToken">
  /// The <see cref="CancellationToken"/> to monitor for cancellation requests.
  /// </param>
  /// <exception cref="ObjectDisposedException">The client has been disposed.</exception>
  /// <returns>
  /// The <see cref="ValueTask"/> that represents the asynchronous operation,
  /// returning the number of bytes received.
  /// </returns>
  ValueTask<int> ReceiveAsync(IBufferWriter<byte> buffer, CancellationToken cancellationToken);

  /// <summary>
  /// Sends a request to the server.
  /// </summary>
  /// <param name="buffer">
  /// The <see cref="ReadOnlyMemory{Byte}"/> that contains the data to be sent.
  /// </param>
  /// <param name="cancellationToken">
  /// The <see cref="CancellationToken"/> to monitor for cancellation requests.
  /// </param>
  /// <exception cref="ObjectDisposedException">The client has been disposed.</exception>
  /// <returns>
  /// The <see cref="ValueTask"/> that represents the asynchronous operation,
  /// sending the requested data.
  /// </returns>
  ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);
}
