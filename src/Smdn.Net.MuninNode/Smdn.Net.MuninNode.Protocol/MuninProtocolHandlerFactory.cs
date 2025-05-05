// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.MuninNode.Protocol;

public static class MuninProtocolHandlerFactory {
  /// <summary>
  /// Gets the default implementation of <see cref="IMuninProtocolHandlerFactory"/>.
  /// </summary>
  public static IMuninProtocolHandlerFactory Default { get; } = new DefaultFactory();

  private class DefaultFactory : IMuninProtocolHandlerFactory {
    public ValueTask<IMuninProtocolHandler> CreateAsync(
      IMuninNodeProfile profile,
      CancellationToken cancellationToken
    )
      => new(
        new MuninProtocolHandler(profile ?? throw new ArgumentNullException(nameof(profile)))
      );
  }
}
