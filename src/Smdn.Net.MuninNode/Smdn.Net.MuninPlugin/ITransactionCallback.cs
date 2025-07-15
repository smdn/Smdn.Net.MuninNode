// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System.Threading;
using System.Threading.Tasks;

using Smdn.Net.MuninNode.Protocol;

namespace Smdn.Net.MuninPlugin;

/// <summary>
/// Defines the callbacks when a transaction with the <c>munin master</c> starts and ends.
/// </summary>
public interface ITransactionCallback {
  /// <summary>
  /// Implements a callback to be invoked when <c>munin-update</c> starts a transaction.
  /// </summary>
  /// <remarks>
  /// This method is invoked when the <see cref="IMuninProtocolHandler"/> is requested to start
  /// processing a transaction.
  /// </remarks>
  /// <param name="cancellationToken">
  /// The <see cref="CancellationToken" /> to monitor for cancellation requests.
  /// </param>
  /// <seealso cref="IMuninProtocolHandler.HandleTransactionStartAsync"/>
  ValueTask StartTransactionAsync(CancellationToken cancellationToken);

  /// <summary>
  /// Implements a callback to be invoked when <c>munin-update</c> ends a transaction.
  /// </summary>
  /// <remarks>
  /// This method is invoked when the <see cref="IMuninProtocolHandler"/> is requested to end
  /// processing a transaction.
  /// </remarks>
  /// <param name="cancellationToken">
  /// The <see cref="CancellationToken" /> to monitor for cancellation requests.
  /// </param>
  /// <seealso cref="Smdn.Net.MuninNode.Protocol.IMuninProtocolHandler.HandleTransactionEndAsync"/>
  ValueTask EndTransactionAsync(CancellationToken cancellationToken);
}
