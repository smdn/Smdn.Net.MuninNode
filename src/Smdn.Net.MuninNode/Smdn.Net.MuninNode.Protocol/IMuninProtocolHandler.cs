// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

using Smdn.Net.MuninNode.Transport;

namespace Smdn.Net.MuninNode.Protocol;

/// <summary>
/// Provides an interface that abstracts the handling of data exchange
/// protocols with <c>munin master</c> in <c>Munin-Node</c>.
/// </summary>
/// <seealso href="https://guide.munin-monitoring.org/en/latest/master/network-protocol.html">
/// Data exchange between master and node
/// </seealso>
public interface IMuninProtocolHandler {
  /// <summary>
  /// Handles the start of a transaction processing requests from <c>munin master</c>.
  /// </summary>
  /// <param name="client">
  /// The <see cref="IMuninNodeClient"/> to which the request is sent from and
  /// to which the response should be sent back.
  /// </param>
  /// <param name="cancellationToken">
  /// The <see cref="CancellationToken"/> to monitor for cancellation requests.
  /// </param>
  /// <returns>
  /// The <see cref="ValueTask"/> that represents the asynchronous operation.
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="client"/> is <see langword="null"/>.
  /// </exception>
  /// <exception cref="MuninNodeClientDisconnectedException">
  /// <paramref name="client"/> has been disconnected.
  /// </exception>
  ValueTask HandleTransactionStartAsync(
    IMuninNodeClient client,
    CancellationToken cancellationToken
  );

  /// <summary>
  /// Handles the end of a transaction processing requests from <c>munin master</c>.
  /// </summary>
  /// <param name="client">
  /// The <see cref="IMuninNodeClient"/> to which the request is sent from and
  /// to which the response should be sent back.
  /// </param>
  /// <param name="cancellationToken">
  /// The <see cref="CancellationToken"/> to monitor for cancellation requests.
  /// </param>
  /// <returns>
  /// The <see cref="ValueTask"/> that represents the asynchronous operation.
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="client"/> is <see langword="null"/>.
  /// </exception>
  /// <exception cref="MuninNodeClientDisconnectedException">
  /// <paramref name="client"/> has been disconnected.
  /// </exception>
  ValueTask HandleTransactionEndAsync(
    IMuninNodeClient client,
    CancellationToken cancellationToken
  );

  /// <summary>
  /// Handles commands from the <c>munin master</c> and sends back a response.
  /// </summary>
  /// <param name="client">
  /// The <see cref="IMuninNodeClient"/> to which the request is sent from and
  /// to which the response should be sent back.
  /// </param>
  /// <param name="commandLine">
  /// The <see cref="ReadOnlySequence{T}"/> representing the command line requested from the <paramref name="client"/>.
  /// </param>
  /// <param name="cancellationToken">
  /// The <see cref="CancellationToken"/> to monitor for cancellation requests.
  /// </param>
  /// <returns>
  /// The <see cref="ValueTask"/> that represents the asynchronous operation.
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="client"/> is <see langword="null"/>.
  /// </exception>
  /// <exception cref="MuninNodeClientDisconnectedException">
  /// <paramref name="client"/> has been disconnected.
  /// </exception>
  ValueTask HandleCommandAsync(
    IMuninNodeClient client,
    ReadOnlySequence<byte> commandLine,
    CancellationToken cancellationToken
  );
}
