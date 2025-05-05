// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.MuninNode.Protocol;

/// <summary>
/// Provides an interface that abstracts the factory for creating the munin
/// protocol handler used by <c>Munin-Node</c>.
/// </summary>
/// <seealso cref="NodeBase"/>
/// <seealso cref="IMuninProtocolHandler"/>
public interface IMuninProtocolHandlerFactory {
  /// <summary>
  /// Creates and returns a <see cref="IMuninProtocolHandler"/> for the specific <c>Munin-Node</c>.
  /// </summary>
  /// <param name="profile">
  /// The <see cref="IMuninNodeProfile"/> that represents the profile of the <c>Munin-Node</c>
  /// for which this handler to be created will process requests.
  /// </param>
  /// <param name="cancellationToken">
  /// The <see cref="CancellationToken"/> to monitor for cancellation requests.
  /// </param>
  /// <returns>
  /// The <see cref="ValueTask{IMuninProtocolHandler}"/> that represents the asynchronous operation,
  /// creating the <see cref="IMuninProtocolHandler"/> bound to the <paramref name="profile"/>.
  /// </returns>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="profile"/> is <see langword="null"/>.
  /// </exception>
  ValueTask<IMuninProtocolHandler> CreateAsync(
    IMuninNodeProfile profile,
    CancellationToken cancellationToken
  );
}
