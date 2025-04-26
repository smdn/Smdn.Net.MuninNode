// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;

namespace Smdn.Net.MuninNode.Transport;

/// <summary>
/// The exception that is thrown when a connection is disconnected when responding to a <see cref="IMuninNodeClient"/>.
/// </summary>
public sealed class MuninNodeClientDisconnectedException : InvalidOperationException {
  public MuninNodeClientDisconnectedException()
    : base()
  {
  }

  public MuninNodeClientDisconnectedException(string message)
    : base(message)
  {
  }

  public MuninNodeClientDisconnectedException(string message, Exception innerException)
    : base(message, innerException)
  {
  }
}
