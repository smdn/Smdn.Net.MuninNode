// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.MuninNode.Transport;

internal sealed class MuninNodeListenerFactory : IMuninNodeListenerFactory {
  public static readonly MuninNodeListenerFactory Instance = new();

  public ValueTask<IMuninNodeListener> CreateAsync(
    EndPoint endPoint,
    IMuninNode node,
    CancellationToken cancellationToken
  )
#pragma warning disable CA2000
    => new(
      new MuninNodeListener(
        endPoint: endPoint,
        logger: null,
        serviceProvider: null
      )
    );
#pragma warning restore CA2000
}
