// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.MuninNode.Transport;

/// <summary>
/// Provides an interface that abstracts the factory for creating
/// server implementation of the transport layer used by <c>Munin-Node</c>.
/// </summary>
/// <seealso cref="IMuninNodeServer"/>
public interface IMuninNodeServerFactory {
  /// <summary>
  /// Creates and returns a <see cref="IMuninNodeServer"/> for the specific <c>Munin-Node</c>.
  /// </summary>
  /// <param name="endPoint">
  /// The <see cref="EndPoint"/> that will be bound with the <see cref="IMuninNodeServer"/> used by the <c>Munin-Node</c> to be created.
  /// </param>
  /// <param name="node">
  /// The <see cref="IMuninNode"/> representing the <c>Munin-Node</c> where the <see cref="IMuninNodeServer"/> to be created will be used.
  /// </param>
  /// <param name="cancellationToken">
  /// The <see cref="CancellationToken"/> to monitor for cancellation requests.
  /// </param>
  /// <remarks>
  /// The <paramref name="node"/> parameter should not be used to determine an endpoint to bind to.
  /// It is intended to be used as a key for the management and identification of the
  /// <see cref="IMuninNodeServer"/> that has been created.
  /// </remarks>
  /// <returns>
  /// The <see cref="ValueTask{IMuninNodeServer}"/> that represents the asynchronous operation,
  /// creating the <see cref="IMuninNodeServer"/> bound to the <paramref name="endPoint"/>.
  /// </returns>
  ValueTask<IMuninNodeServer> CreateAsync(
    EndPoint endPoint,
    IMuninNode node,
    CancellationToken cancellationToken
  );
}
