// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NUnit.Framework;

namespace Smdn.Net.MuninNode.Hosting;

[TestFixture]
public class MuninNodeBackgroundServiceTests {
  [Test]
  public void Dispose()
  {
    var services = new ServiceCollection();

    services.AddHostedMuninNodeService(
      configureNode: options => { },
      buildNode: builder => { }
    );

    var serviceProvider = services.BuildServiceProvider();
    using var muninNodeService = (MuninNodeBackgroundService)serviceProvider.GetRequiredService<IHostedService>();

    Assert.That(
      muninNodeService.Dispose,
      Throws.Nothing
    );
    Assert.That(
      muninNodeService.Dispose,
      Throws.Nothing,
      "dispose again"
    );

    Assert.That(
      () => _ = muninNodeService.EndPoint,
      Throws.TypeOf<ObjectDisposedException>()
    );
    Assert.That(
      async () => await muninNodeService.StartAsync(default),
      Throws.TypeOf<ObjectDisposedException>()
    );
  }

  [Test]
  [CancelAfter(5000)]
  public async Task ExecuteAsync(CancellationToken cancellationToken)
  {
    const string HostName = "munin-node.localhost";
    var services = new ServiceCollection();

    services.AddHostedMuninNodeService(
      configureNode: options => {
        options.HostName = HostName;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
          options.UseLoopbackAddress(port: 0);
        else
          options.UseAnyAddress(port: 0);
      },
      buildNode: builder => { }
    );

    var serviceProvider = services.BuildServiceProvider();
    using var muninNodeService = (MuninNodeBackgroundService)serviceProvider.GetRequiredService<IHostedService>();

    // start service
    await muninNodeService.StartAsync(cancellationToken);

    Assert.That(muninNodeService.EndPoint, Is.Not.Null);

    var ipEndPoint = muninNodeService.EndPoint as IPEndPoint;

    Assert.That(ipEndPoint, Is.Not.Null);
    Assert.That(ipEndPoint.Port, Is.Not.Zero);

    // connect to node
    Assert.That(
      async () => await ConnectToNodeAsync(ipEndPoint, HostName, cancellationToken),
      Throws.Nothing
    );

    // stop service
    await muninNodeService.StopAsync(cancellationToken);

    // attempt to connect to stoppted node
    using var ctsTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(1));
    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, ctsTimeout.Token);

    Assert.That(
      async () => await ConnectToNodeAsync(ipEndPoint, HostName, cts.Token),
      Throws.TypeOf<SocketException>().Or.InstanceOf<OperationCanceledException>()
    );

    static async Task ConnectToNodeAsync(
      IPEndPoint endPoint,
      string expectedBannerHostName,
      CancellationToken cancellationToken
    )
    {
      using var client = new TcpClient();

      await client.ConnectAsync(endPoint);

      using var reader = new StreamReader(
        stream: client.GetStream(),
        leaveOpen: false
      );

      Assert.That(
        await reader.ReadLineAsync(cancellationToken),
        Contains.Substring(expectedBannerHostName),
        "banner response"
      );

      client.Close();
    }
  }
}
