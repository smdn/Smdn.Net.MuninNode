// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.MuninPlugin;

/// <summary>
/// Defines the callbacks when a request session from the <c>munin-update</c> starts or ends.
/// </summary>
[Obsolete(message: ObsoleteMessage.TypeReference)]
public interface INodeSessionCallback {
  /// <summary>
  /// Implements a callback to be called when <c>munin-update</c> starts a session.
  /// </summary>
  /// <remarks>This method is called back when the <see cref="MuninNode.NodeBase"/> starts processing a session.</remarks>
  /// <param name="sessionId">A unique ID that <see cref="MuninNode.NodeBase"/> associates with the session.</param>
  /// <param name="cancellationToken">The <see cref="CancellationToken" /> to monitor for cancellation requests.</param>
  ValueTask ReportSessionStartedAsync(string sessionId, CancellationToken cancellationToken);

  /// <summary>
  /// Implements a callback to be called when <c>munin-update</c> ends a session.
  /// </summary>
  /// <remarks>This method is called back when the <see cref="MuninNode.NodeBase"/> ends processing a session.</remarks>
  /// <param name="sessionId">A unique ID that <see cref="MuninNode.NodeBase"/> associates with the session.</param>
  /// <param name="cancellationToken">The <see cref="CancellationToken" /> to monitor for cancellation requests.</param>
  ValueTask ReportSessionClosedAsync(string sessionId, CancellationToken cancellationToken);

  internal static class ObsoleteMessage {
    public const string TypeReference =
      $"{nameof(INodeSessionCallback)} is deprecated and will be removed in the next major version release. " +
      $"Use {nameof(ITransactionCallback)} interface instead.";

    public const string SessionCallbackProperty =
      $"{nameof(INodeSessionCallback)} is deprecated and will be removed in the next major version release. " +
      $"Instead of setting an object that implements ${nameof(INodeSessionCallback)} to the property, " +
      $"implement the {nameof(ITransactionCallback)} interface to the type itself.";
  }
}
