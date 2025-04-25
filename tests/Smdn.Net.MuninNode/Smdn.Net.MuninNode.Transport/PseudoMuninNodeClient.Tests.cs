// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

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
    var listener = new PseudoMuninNodeListener(endPoint: null, node: null);

    await listener.StartAsync(cancellationToken);

    var client = await listener.AcceptAsync(cancellationToken);

    Assert.That(client, Is.Not.Null);

    Assert.That(
      async () => await client.DisconnectAsync(cancellationToken),
      Throws.Nothing
    );
  }
}
