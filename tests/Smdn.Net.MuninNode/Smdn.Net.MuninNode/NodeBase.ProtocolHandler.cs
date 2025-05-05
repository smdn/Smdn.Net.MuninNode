// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using Smdn.Net.MuninNode.Protocol;
using Smdn.Net.MuninNode.Transport;

namespace Smdn.Net.MuninNode;

#pragma warning disable IDE0040
partial class NodeBaseTests {
#pragma warning restore IDE0040
  private class DisposeFreeProtocolHandler : IMuninProtocolHandler {
    public ValueTask HandleTransactionStartAsync(
      IMuninNodeClient client,
      CancellationToken cancellationToken
    )
      => throw new NotImplementedException();

    public ValueTask HandleTransactionEndAsync(
      IMuninNodeClient client,
      CancellationToken cancellationToken
    )
      => throw new NotImplementedException();

    public ValueTask HandleCommandAsync(
      IMuninNodeClient client,
      ReadOnlySequence<byte> commandLine,
      CancellationToken cancellationToken
    )
      => throw new NotImplementedException();
  }

  private class DisposableProtocolHandler : DisposeFreeProtocolHandler, IDisposable {
    public bool IsDisposed { get; private set; }

    public void Dispose()
      => IsDisposed = true;
  }

  private sealed class AsyncDisposableProtocolHandler : DisposableProtocolHandler, IAsyncDisposable {
    public bool IsAsyncDisposed { get; private set; }

    public ValueTask DisposeAsync()
    {
      IsAsyncDisposed = true;

      return default;
    }
  }

  private sealed class FromFuncMuninProtocolHandlerFactory(Func<IMuninProtocolHandler> factory) : IMuninProtocolHandlerFactory {
    public ValueTask<IMuninProtocolHandler> CreateAsync(
      IMuninNodeProfile profile,
      CancellationToken cancellationToken
    )
      => new(factory());
  }

  private static PseudoMuninNode CreateNodeWithProtocolHandler(IMuninProtocolHandler protocolHandler)
    => new(
      protocolHandlerFactory: new FromFuncMuninProtocolHandlerFactory(factory: () => protocolHandler)
    );

  [Test]
  public async Task Dispose_DisposeFreeProtocolHandler()
  {
    using var node = CreateNodeWithProtocolHandler(
      new DisposeFreeProtocolHandler()
    );

    await node.StartAsync();

    Assert.That(
      node.Dispose,
      Throws.Nothing
    );
  }

  [Test]
  public async Task Dispose_DisposableProtocolHandler()
  {
    var disposableProtocolHandler = new DisposableProtocolHandler();
    using var node = CreateNodeWithProtocolHandler(
      disposableProtocolHandler
    );

    await node.StartAsync();

    Assert.That(
      node.Dispose,
      Throws.Nothing
    );

    Assert.That(disposableProtocolHandler.IsDisposed, Is.True);
  }

  [Test]
  public async Task Dispose_AsyncDisposableProtocolHandler()
  {
    var asyncDisposableProtocolHandler = new AsyncDisposableProtocolHandler();
    using var node = CreateNodeWithProtocolHandler(
      asyncDisposableProtocolHandler
    );

    await node.StartAsync();

    Assert.That(
      node.Dispose,
      Throws.Nothing
    );

    Assert.That(asyncDisposableProtocolHandler.IsDisposed, Is.True);
    Assert.That(asyncDisposableProtocolHandler.IsAsyncDisposed, Is.False);
  }

  [Test]
  public async Task DisposeAsync_DisposeFreeProtocolHandler()
  {
    using var node = CreateNodeWithProtocolHandler(
      new DisposeFreeProtocolHandler()
    );

    await node.StartAsync();

    Assert.That(
      node.DisposeAsync,
      Throws.Nothing
    );
  }

  [Test]
  public async Task DisposeAsync_DisposableProtocolHandler()
  {
    var disposableProtocolHandler = new DisposableProtocolHandler();
    using var node = CreateNodeWithProtocolHandler(
      disposableProtocolHandler
    );

    await node.StartAsync();

    Assert.That(
      node.DisposeAsync,
      Throws.Nothing
    );

    Assert.That(disposableProtocolHandler.IsDisposed, Is.True);
  }

  [Test]
  public async Task DisposeAsync_AsyncDisposableProtocolHandler()
  {
    var asyncDisposableProtocolHandler = new AsyncDisposableProtocolHandler();
    using var node = CreateNodeWithProtocolHandler(
      asyncDisposableProtocolHandler
    );

    await node.StartAsync();

    Assert.That(
      node.DisposeAsync,
      Throws.Nothing
    );

    Assert.That(asyncDisposableProtocolHandler.IsDisposed, Is.False);
    Assert.That(asyncDisposableProtocolHandler.IsAsyncDisposed, Is.True);
  }
}
