// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.MuninNode.Transport;

/// <summary>
/// Provides an interface that abstracts the listener implementation of
/// the transport layer that accepts the connections to the <c>Munin-Node</c>.
/// </summary>
/// <seealso cref="IMuninNodeClient"/>
/// <seealso cref="IMuninNodeListenerFactory"/>
public interface IMuninNodeListener : IDisposable, IAsyncDisposable {
  /// <summary>
  /// Gets the <see cref="EndPoint"/> that is bound with this instance.
  /// </summary>
  /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
  /// <exception cref="InvalidOperationException">The instance is not started yet.</exception>
  /// <value>
  /// <see langword="null"/> if this instance does not have <see cref="EndPoint"/>.
  /// </value>
  EndPoint? EndPoint { get; }

  /// <summary>
  /// Start the listener and enable to accept connections from clients.
  /// </summary>
  /// <param name="cancellationToken">
  /// The <see cref="CancellationToken"/> to monitor for cancellation requests.
  /// </param>
  /// <returns>
  /// The <see cref="ValueTask"/> that represents the asynchronous operation,
  /// starting the listener.
  /// </returns>
  ValueTask StartAsync(CancellationToken cancellationToken);

  /// <summary>
  /// Waits for a single client and accepts an incoming connection.
  /// </summary>
  /// <param name="cancellationToken">
  /// The <see cref="CancellationToken"/> to monitor for cancellation requests.
  /// </param>
  /// <returns>
  /// The <see cref="ValueTask{IMuninNodeClient}"/> that represents the asynchronous operation,
  /// creating the <see cref="IMuninNodeClient"/> representing the accepted client.
  /// </returns>
  ValueTask<IMuninNodeClient> AcceptAsync(CancellationToken cancellationToken);
}
