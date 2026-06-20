// SPDX-FileCopyrightText: 2026 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#if SYSTEM_THREADING_TASKS_TASK_WAITASYNC // Implementing this interface requires Task.WaitAsync
using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.MuninNode;

/// <summary>
/// Provides a mechanism to asynchronously observe and await the lifecycle state
/// transitions of a <see cref="IMuninNode"/>.
/// </summary>
/// <remarks>
/// This interface is designed to be used in conjunction with background services
/// or hosting environments, allowing consumers to safely wait for a node to fully
/// complete its startup or shutdown processes without relying on polling or
/// thread-blocking synchronization primitives.
/// </remarks>
public interface IMuninNodeLifecycle {
  /// <summary>
  /// Asynchronously waits until the <see cref="IMuninNode"/> has fully transitioned
  /// to the started state.
  /// </summary>
  /// <param name="cancellationToken">
  /// A <see cref="CancellationToken"/> used to cancel the wait operation.
  /// </param>
  /// <returns>
  /// A <see cref="Task"/> that represents the asynchronous wait operation.
  /// The task completes when the node is fully started.
  /// </returns>
  /// <seealso cref="IMuninNode.RunAsync(CancellationToken)"/>
  Task WaitForStartedAsync(CancellationToken cancellationToken = default);

  /// <summary>
  /// Asynchronously waits until the <see cref="IMuninNode"/> has fully transitioned
  /// to the stopped state.
  /// </summary>
  /// <param name="cancellationToken">
  /// A <see cref="CancellationToken"/> used to cancel the wait operation.
  /// </param>
  /// <returns>
  /// A <see cref="Task"/> that represents the asynchronous wait operation.
  /// The task completes when the node is fully stopped.
  /// </returns>
  /// <seealso cref="IMuninNode.RunAsync(CancellationToken)"/>
  Task WaitForStoppedAsync(CancellationToken cancellationToken = default);
}
#endif
