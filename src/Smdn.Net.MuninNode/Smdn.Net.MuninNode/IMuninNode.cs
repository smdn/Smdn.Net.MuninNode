// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.MuninNode;

/// <summary>
/// An interface that abstracts an arbitrary <c>Munin-Node</c> implementation.
/// </summary>
public interface IMuninNode {
  /// <summary>
  /// Gets a <see cref="string"/> to be used as the hostname of the <c>Munin-Node</c>.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The value of this property is used as the banner response to the
  /// <c>munin master</c> process and the response to the <c>nodes</c> command.
  /// This can be any name different from the host on which the <c>Munin-Node</c> process runs.
  /// </para>
  /// <para>
  /// The value of this property may not be a name that can be resolved by DNS.
  /// It is not guaranteed to be used to determine the endpoint for the <c>Munin-Node</c>.
  /// </para>
  /// </remarks>
  string HostName { get; }

  /// <summary>
  /// Gets the <see cref="EndPoint"/> object representing the endpoint of the <c>Munin-Node</c>.
  /// </summary>
  /// <exception cref="InvalidOperationException">
  /// This instance is not running, or the state of the this instance is such that
  /// the endpoint cannot be get.
  /// </exception>
  /// <exception cref="NotSupportedException">
  /// Getting endpoint from this instance is not supported.
  /// </exception>
  /// <exception cref="ObjectDisposedException">
  /// Attempted to read a property value after the instance was disposed.
  /// </exception>
  EndPoint EndPoint { get; }

  // suppress warnings due to the absence of `ConfigureAwaitOptions`.
#if SYSTEM_THREADING_TASKS_CONFIGUREAWAITOPTIONS
#pragma warning disable CS0419
#else
#pragma warning disable CS1574
#endif

  /// <summary>
  /// Runs a <c>Munin-Node</c> instance and returns a <see cref="Task"/> that processes
  /// requests from the <c>munin master</c> and only completes when the
  /// <paramref name="cancellationToken"/> is triggered.
  /// </summary>
  /// <param name="cancellationToken">
  /// The <see cref="CancellationToken"/> to trigger stopping <c>Munin-Node</c>.
  /// </param>
  /// <remarks>
  ///   <para>
  ///     This method continues to wait for a connection from the client unless a
  ///     cancellation request is made by <paramref name="cancellationToken"/>.
  ///   </para>
  ///   <para>
  ///     If the cancellation request is made by <paramref name="cancellationToken"/>,
  ///     an <see cref="OperationCanceledException"/> will be thrown even if the operation ends successfully.
  ///     Therefore, if you do not want an exception to be thrown due to an intentional stop
  ///     caused by a cancellation request, call <see cref="Task.ConfigureAwait"/>
  ///     with <see cref="ConfigureAwaitOptions.SuppressThrowing"/> for the <see cref="Task"/>
  ///     returned by this method.
  ///   </para>
  /// </remarks>
  /// <returns>
  /// A <see cref="Task"/> that represents the the server task that handles connections and
  /// requests to the <c>Munin-Node</c> implementation provided by this instance asynchronously.
  /// </returns>
#pragma warning restore CS1574, CS0419
  Task RunAsync(CancellationToken cancellationToken);
}
