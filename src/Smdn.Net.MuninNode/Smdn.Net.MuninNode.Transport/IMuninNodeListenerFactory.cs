// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.MuninNode.Transport;

/// <summary>
/// Provides an interface that abstracts the factory for creating the client
/// listener implementation of the transport layer used by <c>Munin-Node</c>.
/// </summary>
/// <seealso cref="IMuninNodeListener"/>
public interface IMuninNodeListenerFactory {
  /// <summary>
  /// Creates and returns a <see cref="IMuninNodeListener"/> for the specific <c>Munin-Node</c>.
  /// </summary>
  /// <param name="endPoint">
  /// The <see cref="EndPoint"/> that will be bound with the <see cref="IMuninNodeListener"/> used by the <c>Munin-Node</c> to be created.
  /// </param>
  /// <param name="node">
  /// The <see cref="IMuninNode"/> representing the <c>Munin-Node</c> where the <see cref="IMuninNodeListener"/> to be created will be used.
  /// </param>
  /// <param name="cancellationToken">
  /// The <see cref="CancellationToken"/> to monitor for cancellation requests.
  /// </param>
  /// <remarks>
  /// The <paramref name="node"/> parameter should not be used to determine an endpoint to bind to.
  /// It is intended to be used as a key for the management and identification of the
  /// <see cref="IMuninNodeListener"/> that has been created.
  /// </remarks>
  /// <returns>
  /// The <see cref="ValueTask{IMuninNodeListener}"/> that represents the asynchronous operation,
  /// creating the <see cref="IMuninNodeListener"/> bound to the <paramref name="endPoint"/>.
  /// </returns>
  ValueTask<IMuninNodeListener> CreateAsync(
    EndPoint endPoint,
    IMuninNode node,
    CancellationToken cancellationToken
  );
}
