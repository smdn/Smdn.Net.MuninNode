// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Smdn.Net.MuninNode.Transport;

[TestFixture]
public class PseudoMuninNodeClientTests {
  [Test]
  [CancelAfter(3000)]
  public async Task DisconnectAsync(CancellationToken cancellationToken)
  {
    var server = new PseudoMuninNodeServer(endPoint: null, node: null);

    await server.StartAsync(cancellationToken);

    var client = await server.AcceptAsync(cancellationToken);

    Assert.That(client, Is.Not.Null);

    Assert.That(
      async () => await client.DisconnectAsync(cancellationToken),
      Throws.Nothing
    );
  }
}