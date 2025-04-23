// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.MuninNode.Transport;

internal sealed class PseudoMuninNodeServerFactory : IMuninNodeServerFactory {
  public PseudoMuninNodeServerFactory()
  {
  }

  public ValueTask<IMuninNodeServer> CreateAsync(
    EndPoint endPoint,
    IMuninNode node,
    CancellationToken cancellationToken
  )
    => new(
      new PseudoMuninNodeServer(endPoint, node)
    );
}
