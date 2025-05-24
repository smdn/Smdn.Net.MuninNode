// SPDX-FileCopyrightText: 2021 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Smdn.Net.MuninNode.Protocol;
using Smdn.Net.MuninNode.Transport;
using Smdn.Net.MuninPlugin;

namespace Smdn.Net.MuninNode;

/// <summary>
/// Provides an extensible base class with basic Munin-Node functionality.
/// </summary>
/// <seealso href="https://guide.munin-monitoring.org/en/latest/node/index.html">The Munin node</seealso>
public abstract partial class NodeBase : IMuninNode, IMuninNodeProfile, IDisposable, IAsyncDisposable {
  private static readonly Version DefaultNodeVersion = new(1, 0, 0, 0);

  public abstract IPluginProvider PluginProvider { get; }
  public abstract string HostName { get; }

  public virtual Version NodeVersion => DefaultNodeVersion;
  public virtual Encoding Encoding => Encoding.Default;

  protected ILogger? Logger { get; }

  private readonly IMuninProtocolHandlerFactory protocolHandlerFactory;
  private readonly IMuninNodeListenerFactory listenerFactory;
  private readonly IAccessRule? accessRule;

  private IMuninProtocolHandler? protocolHandler;
  private IMuninNodeListener? listener;

#pragma warning disable CA1033 // override GetNodeProfile() instead if necessary
  string IMuninNodeProfile.Version => NodeVersion.ToString();
#pragma warning restore CA1033

  /// <summary>
  /// Gets the <see cref="IMuninNodeListener"/> used by the current instance.
  /// </summary>
  /// <exception cref="ObjectDisposedException">
  /// Attempted to read a property value after the instance was disposed.
  /// </exception>
  /// <value>
  /// <see langword="null"/> if the <see cref="Start"/> or <see cref="StartAsync"/> method has not been called.
  /// </value>
  protected IMuninNodeListener? Listener {
    get {
      ThrowIfDisposed();

      return listener;
    }
  }

  /// <inheritdoc cref="IMuninNode.EndPoint"/>
  /// <seealso cref="GetLocalEndPointToBind"/>
  /// <seealso cref="Start"/>
  /// <seealso cref="StartAsync"/>
  public EndPoint EndPoint {
    get {
      ThrowIfDisposed();

      if (listener is null)
        throw new InvalidOperationException("not yet started");
      if (listener.EndPoint is null)
        throw new NotSupportedException("this instance does not have endpoint");

      return listener.EndPoint;
    }
  }

  private CountdownEvent sessionCountdownEvent = new(initialCount: 1);
  private bool disposed;

  protected NodeBase(
    IMuninNodeListenerFactory listenerFactory,
    IAccessRule? accessRule,
    ILogger? logger
  )
    : this(
      protocolHandlerFactory: MuninProtocolHandlerFactory.Default,
      listenerFactory: listenerFactory ?? throw new ArgumentNullException(nameof(listenerFactory)),
      accessRule: accessRule,
      logger: logger
    )
  {
  }

  protected NodeBase(
    IMuninProtocolHandlerFactory protocolHandlerFactory,
    IMuninNodeListenerFactory listenerFactory,
    IAccessRule? accessRule,
    ILogger? logger
  )
  {
    this.protocolHandlerFactory = protocolHandlerFactory ?? throw new ArgumentNullException(nameof(protocolHandlerFactory));
    this.listenerFactory = listenerFactory ?? throw new ArgumentNullException(nameof(listenerFactory));
    this.accessRule = accessRule;
    Logger = logger;
  }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  public async ValueTask DisposeAsync()
  {
    await DisposeAsyncCore().ConfigureAwait(false);

    Dispose(disposing: false);
    GC.SuppressFinalize(this);
  }

  protected virtual async ValueTask DisposeAsyncCore()
  {
    if (listener is not null)
      await listener.DisposeAsync().ConfigureAwait(false);

    listener = null;

    if (protocolHandler is IAsyncDisposable asyncDisposableProtocolHandler)
      await asyncDisposableProtocolHandler.DisposeAsync().ConfigureAwait(false);
    else if (protocolHandler is IDisposable disposableProtocolHandler)
      disposableProtocolHandler.Dispose();

    protocolHandler = null;

    sessionCountdownEvent?.Dispose();
    sessionCountdownEvent = null!;

    disposed = true;
  }

  protected virtual void Dispose(bool disposing)
  {
    if (!disposing)
      return;

    listener?.Dispose();
    listener = null!;

    if (protocolHandler is IDisposable disposableProtocolHandler)
      disposableProtocolHandler.Dispose();

    protocolHandler = null;

    sessionCountdownEvent?.Dispose();
    sessionCountdownEvent = null!;

    disposed = true;
  }

  protected void ThrowIfDisposed()
  {
    if (disposed)
      throw new ObjectDisposedException(GetType().FullName);
  }

  protected void ThrowIfPluginProviderIsNull()
  {
    if (PluginProvider is null)
      throw new InvalidOperationException($"{nameof(PluginProvider)} cannot be null");
  }

  /// <summary>
  /// Gets the <see cref="EndPoint"/> to be bound as the <c>Munin-Node</c>'s endpoint.
  /// </summary>
  /// <returns>
  /// An <see cref="EndPoint"/>.
  /// The default implementation returns an <see cref="IPEndPoint"/> with the port number <c>0</c>
  /// and <see cref="IPAddress.IPv6Loopback"/>/<see cref="IPAddress.Loopback"/>.
  /// </returns>
  /// <seealso cref="StartAsync"/>
  /// <seealso cref="LocalEndPoint"/>
  protected virtual EndPoint GetLocalEndPointToBind()
    => new IPEndPoint(
      address:
        Socket.OSSupportsIPv6
          ? IPAddress.IPv6Loopback
          : Socket.OSSupportsIPv4
            ? IPAddress.Loopback
            : throw new NotSupportedException(),
      port: 0
    );

  protected virtual IMuninNodeProfile GetNodeProfile()
    => this;

  /// <summary>
  /// Starts the <c>Munin-Node</c> and prepares to accept connections from clients.
  /// </summary>
  /// <param name="cancellationToken">
  /// The <see cref="CancellationToken"/> to monitor for cancellation requests.
  /// </param>
  /// <returns>
  /// The <see cref="ValueTask"/> that represents the asynchronous operation,
  /// starting the <c>Munin-Node</c> instance.
  /// </returns>
  /// <exception cref="InvalidOperationException">
  /// It is already in the started state. Or, the socket is not created.
  /// </exception>
  /// <seealso cref="GetLocalEndPointToBind"/>
  public ValueTask StartAsync(CancellationToken cancellationToken = default)
  {
    ThrowIfDisposed();

    if (listener is not null)
      throw new InvalidOperationException("already started");

    return StartAsyncCore();

    async ValueTask StartAsyncCore()
    {
      cancellationToken.ThrowIfCancellationRequested();

      if (Logger is not null)
        LogStartingNode(Logger, null);

      listener = await listenerFactory.CreateAsync(
        endPoint: GetLocalEndPointToBind(),
        node: this,
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);

      if (listener is null)
        throw new InvalidOperationException("cannot start listener");

      await listener.StartAsync(cancellationToken).ConfigureAwait(false);

      ThrowIfPluginProviderIsNull();

      protocolHandler = await protocolHandlerFactory.CreateAsync(
        profile: GetNodeProfile(),
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);

      if (Logger is not null)
        LogStartedNode(Logger, HostName, listener.EndPoint, null);

      sessionCountdownEvent.Reset();
    }
  }

  /// <summary>
  /// Stops accepting connections from clients at the <c>Munin-Node</c> currently running.
  /// </summary>
  /// <param name="cancellationToken">
  /// The <see cref="CancellationToken"/> to monitor for cancellation requests.
  /// </param>
  /// <returns>
  /// The <see cref="ValueTask"/> that represents the asynchronous operation,
  /// starting the <c>Munin-Node</c> instance.
  /// </returns>
  /// <remarks>
  /// If current <c>Munin-Node</c> has already stopped, this method does nothing and returns the result.
  /// </remarks>
  public ValueTask StopAsync(CancellationToken cancellationToken = default)
  {
    ThrowIfDisposed();

    if (listener is null)
      return default; // already stopped

    return StopAsyncCore();

    async ValueTask StopAsyncCore()
    {
      if (Logger is not null)
        LogStoppingNode(Logger, HostName, null);

      // decrement by the initial value of 1 (re)set in Start()/StartAsync()
      sessionCountdownEvent.Signal();

      try {
        // wait for all sessions to complete
        sessionCountdownEvent.Wait(cancellationToken);
      }
      catch (OperationCanceledException ex) when (cancellationToken.Equals(ex.CancellationToken)) {
        // revert decremented counter value
        sessionCountdownEvent.AddCount();

        throw;
      }

      await listener.DisposeAsync().ConfigureAwait(false);

      listener = null;

      if (protocolHandler is not null) {
        if (protocolHandler is IAsyncDisposable asyncDisposableProtocolHandler)
          await asyncDisposableProtocolHandler.DisposeAsync().ConfigureAwait(false);
        else if (protocolHandler is IDisposable disposableProtocolHandler)
          disposableProtocolHandler.Dispose();

        protocolHandler = null;
      }

      if (Logger is not null)
        LogStoppedNode(Logger, HostName, null);
    }
  }

  /// <inheritdoc cref="IMuninNode.RunAsync(CancellationToken)"/>
  /// <seealso cref="IMuninNode.RunAsync(CancellationToken)"/>
  /// <seealso cref="StartAsync(CancellationToken)"/>
  /// <seealso cref="AcceptAsync(bool,CancellationToken)"/>
  public Task RunAsync(CancellationToken cancellationToken)
  {
    ThrowIfDisposed();

    return RunAsyncCore(cancellationToken);

    async Task RunAsyncCore(CancellationToken ct)
    {
      await StartAsync(ct).ConfigureAwait(false);

      await AcceptAsync(
        throwIfCancellationRequested: true,
        cancellationToken: ct
      ).ConfigureAwait(false);
    }
  }

  /// <summary>
  /// Starts accepting multiple sessions.
  /// The <see cref="ValueTask" /> this method returns will never complete unless the cancellation requested by the <paramref name="cancellationToken" />.
  /// </summary>
  /// <param name="throwIfCancellationRequested">
  /// If <see langword="true" />, throws an <see cref="OperationCanceledException" /> on cancellation requested.
  /// If <see langword="false" />, completes the task without throwing an <see cref="OperationCanceledException" />.
  /// </param>
  /// <param name="cancellationToken">
  /// The <see cref="CancellationToken" /> to stop accepting sessions.
  /// </param>
  public async ValueTask AcceptAsync(
    bool throwIfCancellationRequested,
    CancellationToken cancellationToken
  )
  {
    ThrowIfDisposed();

    if (Logger is not null)
      LogStartedAcceptingConnections(Logger, null);

    try {
      for (; ; ) {
        if (cancellationToken.IsCancellationRequested) {
          if (throwIfCancellationRequested)
            cancellationToken.ThrowIfCancellationRequested();
          else
            break;
        }

        await AcceptSingleSessionAsync(cancellationToken).ConfigureAwait(false);
      }
    }
    catch (OperationCanceledException ex) {
      if (throwIfCancellationRequested || ex.CancellationToken != cancellationToken)
        throw;
    }

    if (Logger is not null)
      LogStoppedAcceptingConnections(Logger, null);
  }

  /// <summary>
  /// Starts accepting single session.
  /// The <see cref="ValueTask" /> this method returns will complete when the accepted session is closed or the cancellation requested by the <paramref name="cancellationToken" />.
  /// </summary>
  /// <param name="cancellationToken">
  /// The <see cref="CancellationToken" /> to stop accepting sessions.
  /// </param>
  public async ValueTask AcceptSingleSessionAsync(
    CancellationToken cancellationToken = default
  )
  {
    ThrowIfDisposed();

    if (listener is null)
      throw new InvalidOperationException("not started or already closed");

    ThrowIfPluginProviderIsNull();

    if (Logger is not null)
      LogAcceptingConnection(Logger, null);

    var client = await listener.AcceptAsync(
      cancellationToken: cancellationToken
    ).ConfigureAwait(false);

    // begin logger scope with this client's endpoint
    using var scope = Logger?.BeginScope(client.EndPoint);

    try {
      sessionCountdownEvent.AddCount();

      cancellationToken.ThrowIfCancellationRequested();

      if (!CanAccept(client))
        return;

      await ProcessSessionAsync(
        client,
        cancellationToken
      ).ConfigureAwait(false);
    }
    finally {
      await client.DisposeAsync().ConfigureAwait(false);

      sessionCountdownEvent.Signal();

      if (Logger is not null)
        LogAcceptedConnectionClosed(Logger, null);
    }

    bool CanAccept(IMuninNodeClient client)
    {
      if (client.EndPoint is not IPEndPoint remoteIPEndPoint) {
        if (Logger is not null)
          LogConnectionCanNotAccept(Logger, client.EndPoint, client.EndPoint?.AddressFamily, null);
        return false;
      }

      if (accessRule is not null && !accessRule.IsAcceptable(remoteIPEndPoint)) {
        if (Logger is not null)
          LogAccessRefused(Logger, null);
        return false;
      }

      return true;
    }
  }

  private async ValueTask ProcessSessionAsync(
    IMuninNodeClient client,
    CancellationToken cancellationToken
  )
  {
#if DEBUG
    if (protocolHandler is null)
      throw new InvalidOperationException($"{nameof(protocolHandler)} is not set");
#endif

    cancellationToken.ThrowIfCancellationRequested();

    try {
      if (Logger is not null)
        LogStartingTransaction(Logger, null);

#if !DEBUG
#pragma warning disable CS8602
#endif
      await protocolHandler.HandleTransactionStartAsync(client, cancellationToken).ConfigureAwait(false);
#pragma warning restore CS8602
    }
    catch (Exception ex) when (ex is not OperationCanceledException) {
      if (Logger is not null)
        LogUnexpectedExceptionWhileStartingTransaction(Logger, ex);
      return;
    }

    cancellationToken.ThrowIfCancellationRequested();

    var sessionId = GenerateSessionId(listener!.EndPoint, client.EndPoint);

    // begin logger scope with this session id
    using var scope = Logger is null
      ? null
      : LoggerScopeForSession(Logger, sessionId);

    if (Logger is not null)
      LogSessionStarted(Logger, null);

    try {
      // TODO: rename INodeSessionCallback to ITransactionCallback
      if (PluginProvider.SessionCallback is INodeSessionCallback pluginProviderSessionCallback)
        await pluginProviderSessionCallback.ReportSessionStartedAsync(sessionId, cancellationToken).ConfigureAwait(false);

      foreach (var pluginSessionCallback in EnumerateSessionCallbackForPlugins(PluginProvider)) {
        await pluginSessionCallback.ReportSessionStartedAsync(sessionId, cancellationToken).ConfigureAwait(false);
      }

      // https://docs.microsoft.com/ja-jp/dotnet/standard/io/pipelines
      var pipe = new Pipe();

      await Task.WhenAll(
        ReceiveCommandAsync(client, pipe.Writer, cancellationToken),
        ProcessCommandAsync(client, pipe.Reader, cancellationToken)
      ).ConfigureAwait(false);

      if (Logger is not null)
        LogSessionClosed(Logger, null);
    }
    finally {
      foreach (var pluginSessionCallback in EnumerateSessionCallbackForPlugins(PluginProvider)) {
        await pluginSessionCallback.ReportSessionClosedAsync(sessionId, cancellationToken).ConfigureAwait(false);
      }

      if (PluginProvider.SessionCallback is INodeSessionCallback pluginProviderSessionCallback)
        await pluginProviderSessionCallback.ReportSessionClosedAsync(sessionId, cancellationToken).ConfigureAwait(false);

      await protocolHandler.HandleTransactionEndAsync(client, cancellationToken).ConfigureAwait(false);
    }

    static IEnumerable<INodeSessionCallback> EnumerateSessionCallbackForPlugins(IPluginProvider pluginProvider)
    {
      foreach (var plugin in pluginProvider.EnumeratePlugins(flattenMultigraphPlugins: true)) {
        if (plugin.SessionCallback is INodeSessionCallback pluginSessionCallback)
          yield return pluginSessionCallback;
      }
    }
  }

  private static string GenerateSessionId(EndPoint? localEndPoint, EndPoint? remoteEndPoint)
  {
#if SYSTEM_SECURITY_CRYPTOGRAPHY_SHA1_HASHSIZEINBYTES
    const int SHA1HashSizeInBytes = SHA1.HashSizeInBytes;
#else
    const int SHA1HashSizeInBytes = 160/*bits*/ / 8;
#endif

    var sessionIdentity = Encoding.ASCII.GetBytes($"{localEndPoint}\n{remoteEndPoint}\n{DateTimeOffset.Now:o}");

    Span<byte> sha1hash = stackalloc byte[SHA1HashSizeInBytes];

#pragma warning disable CA5350
#if SYSTEM_SECURITY_CRYPTOGRAPHY_SHA1_TRYHASHDATA
    SHA1.TryHashData(sessionIdentity, sha1hash, out var bytesWrittenSHA1);
#else
    using var sha1 = SHA1.Create();

    sha1.TryComputeHash(sessionIdentity, sha1hash, out var bytesWrittenSHA1);
#endif
#pragma warning restore CA5350

    return Convert.ToBase64String(sha1hash);
  }

  private async Task ReceiveCommandAsync(
    IMuninNodeClient client,
    PipeWriter writer,
    CancellationToken cancellationToken
  )
  {
    for (; ; ) {
      cancellationToken.ThrowIfCancellationRequested();

      try {
        var count = await client.ReceiveAsync(writer, cancellationToken).ConfigureAwait(false);

        if (count == 0)
          break;
      }
      catch (OperationCanceledException) {
        if (Logger is not null)
          LogSessionOperationCanceledWhileReceiving(Logger, null);
        throw;
      }
      catch (ObjectDisposedException) {
        break; // client has been disconnected/disposed
      }
#pragma warning disable CA1031
      catch (Exception ex) {
        if (Logger is not null)
          LogSessionUnexpectedExceptionWhileReceiving(Logger, ex);
        break; // swallow
      }
#pragma warning restore CA1031

      var result = await writer.FlushAsync(
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);

      if (result.IsCompleted)
        break;
    }

    await writer.CompleteAsync().ConfigureAwait(false);
  }

  private async Task ProcessCommandAsync(
    IMuninNodeClient client,
    PipeReader reader,
    CancellationToken cancellationToken
  )
  {
#if DEBUG
    if (protocolHandler is null)
      throw new InvalidOperationException($"{nameof(protocolHandler)} is not set");
#endif

    for (; ; ) {
      cancellationToken.ThrowIfCancellationRequested();

      var result = await reader.ReadAsync(
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);
      var buffer = result.Buffer;

      try {
        while (TryReadLine(ref buffer, out var line)) {
          // process each line read from the client as a single request command line
#if !DEBUG
#pragma warning disable CS8602
#endif
          await protocolHandler.HandleCommandAsync(
            client: client,
            commandLine: line,
            cancellationToken: cancellationToken
          ).ConfigureAwait(false);
#pragma warning restore CS8602
        }
      }
      catch (MuninNodeClientDisconnectedException) {
        if (Logger is not null)
          LogSessionClientDisconnectedWhileSending(Logger, null);
        break; // expected exception
      }
      catch (OperationCanceledException) {
        if (Logger is not null)
          LogSessionOperationCanceledWhileProcessingCommand(Logger, null);
        throw;
      }
#pragma warning disable CA1031
      catch (Exception ex) {
        if (Logger is not null)
          LogSessionUnexpectedExceptionWhileProcessingCommand(Logger, ex);

        await client.DisconnectAsync(cancellationToken).ConfigureAwait(false);

        break;
      }
#pragma warning restore CA1031

      reader.AdvanceTo(buffer.Start, buffer.End);

      if (result.IsCompleted)
        break;
    }

#pragma warning disable CS1587
    /// <summary>
    /// Output from the beginning of a given <paramref name="buffer"/> to a newline as <paramref name="line"/>,
    /// and reassign the rest of the <paramref name="buffer"/> after the newline as <paramref name="buffer"/>.
    /// </summary>
    /// <remarks>
    /// Treats <c>CR LF</c> sequences and <c>LF</c> as line breaks.
    /// </remarks>
    /// <returns>
    /// <see langword="true"/> a single line can be read from <paramref name="buffer"/>, false otherwise.
    /// </returns>
#pragma warning disable CS1587
    static bool TryReadLine(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line)
    {
      var reader = new SequenceReader<byte>(buffer);
      const byte CR = (byte)'\r';
      const byte LF = (byte)'\n';

      // read sequence until LF
      if (!reader.TryReadTo(out line, delimiter: LF, advancePastDelimiter: true)) {
        line = default;
        return false;
      }

      // trim the CR just before the LF, in case the line ends with CRLF
      if (1 < reader.Consumed) {
        var lineReader = new SequenceReader<byte>(line);

        lineReader.Advance(reader.Consumed - 2 /* <CR?>+<LF> */);

        if (lineReader.IsNext(CR))
          line = line.Slice(0, lineReader.Position); // trim CR
      }

#if SYSTEM_BUFFERS_SEQUENCEREADER_UNREADSEQUENCE
      buffer = reader.UnreadSequence;
#else
      buffer = reader.Sequence.Slice(reader.Position);
#endif

      return true;
    }
  }
}
