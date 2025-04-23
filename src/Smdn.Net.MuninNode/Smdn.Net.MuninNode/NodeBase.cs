// SPDX-FileCopyrightText: 2021 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

// TODO: use LoggerMessage.Define
#pragma warning disable CA1848 // For improved performance, use the LoggerMessage delegates instead of calling 'LoggerExtensions.LogInformation(ILogger, string?, params object?[])'

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Smdn.Net.MuninPlugin;
#if !SYSTEM_TEXT_ENCODINGEXTENSIONS
using Smdn.Text.Encodings;
#endif

using Smdn.Net.MuninNode.Transport;

namespace Smdn.Net.MuninNode;

/// <summary>
/// Provides an extensible base class with basic Munin-Node functionality.
/// </summary>
/// <seealso href="https://guide.munin-monitoring.org/en/latest/node/index.html">The Munin node</seealso>
public abstract partial class NodeBase : IMuninNode, IDisposable, IAsyncDisposable {
  private static readonly Version DefaultNodeVersion = new(1, 0, 0, 0);

  public abstract IPluginProvider PluginProvider { get; }
  public abstract string HostName { get; }

  public virtual Version NodeVersion => DefaultNodeVersion;
  public virtual Encoding Encoding => Encoding.Default;

  protected ILogger? Logger { get; }

  private readonly IMuninNodeServerFactory serverFactory;
  private readonly IAccessRule? accessRule;

  private IMuninNodeServer? server;

  /// <summary>
  /// Gets the <see cref="IMuninNodeServer"/> used by the current instance.
  /// </summary>
  /// <exception cref="ObjectDisposedException">
  /// Attempted to read a property value after the instance was disposed.
  /// </exception>
  /// <value>
  /// <see langword="null"/> if the <see cref="Start"/> or <see cref="StartAsync"/> method has not been called.
  /// </value>
  protected IMuninNodeServer? Server {
    get {
      ThrowIfDisposed();

      return server;
    }
  }

  /// <summary>
  /// Gets the <see cref="EndPoint"/> actually bound with the current instance.
  /// </summary>
  /// <exception cref="InvalidOperationException">
  /// The <see cref="Start"/> or <see cref="StartAsync"/> method has not been called.
  /// </exception>
  /// <exception cref="NotSupportedException">
  /// Getting endpoint from this instance is not supported.
  /// </exception>
  /// <exception cref="ObjectDisposedException">
  /// Attempted to read a property value after the instance was disposed.
  /// </exception>
  /// <seealso cref="GetLocalEndPointToBind"/>
  public EndPoint LocalEndPoint {
    get {
      ThrowIfDisposed();

      if (server is null)
        throw new InvalidOperationException("not yet started");
      if (server.EndPoint is null)
        throw new NotSupportedException("this instance does not have endpoint");

      return server.EndPoint;
    }
  }

  private readonly ArrayBufferWriter<byte> responseBuffer = new(initialCapacity: 1024); // TODO: define best initial capacity

  private bool disposed;

  protected NodeBase(
    IMuninNodeServerFactory serverFactory,
    IAccessRule? accessRule,
    ILogger? logger
  )
  {
    this.serverFactory = serverFactory ?? throw new ArgumentNullException(nameof(serverFactory));
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
    if (server is not null)
      await server.DisposeAsync().ConfigureAwait(false);

    server = null;

    disposed = true;
  }

  protected virtual void Dispose(bool disposing)
  {
    if (!disposing)
      return;

    server?.Dispose();
    server = null!;

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

    if (server is not null)
      throw new InvalidOperationException("already started");

    return StartAsyncCore();

    async ValueTask StartAsyncCore()
    {
      cancellationToken.ThrowIfCancellationRequested();

      Logger?.LogInformation("starting");

      server = await serverFactory.CreateAsync(
        endPoint: GetLocalEndPointToBind(),
        node: this,
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);

      if (server is null)
        throw new InvalidOperationException("cannot start server");

      await server.StartAsync(cancellationToken).ConfigureAwait(false);

      Logger?.LogInformation("started (end point: {EndPoint})", server.EndPoint);
    }
  }

  /// <summary>
  /// Starts accepting multiple sessions.
  /// The <see cref="ValueTask" /> this method returns will never complete unless the cancellation requested by the <paramref name="cancellationToken" />.
  /// </summary>
  /// <param name="throwIfCancellationRequested">
  /// If <see langworkd="true" />, throws an <see cref="OperationCanceledException" /> on cancellation requested.
  /// If <see langworkd="false" />, completes the task without throwing an <see cref="OperationCanceledException" />.
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

    Logger?.LogInformation("starting to accept");

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

    Logger?.LogInformation("stopped to accept");
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

    if (server is null)
      throw new InvalidOperationException("not started or already closed");

    ThrowIfPluginProviderIsNull();

    Logger?.LogInformation("accepting...");

    var client = await server.AcceptAsync(
      cancellationToken: cancellationToken
    ).ConfigureAwait(false);

    // holds a reference to the endpoint before the client being disposed
    var remoteEndPoint = client.EndPoint;

    try {
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

      Logger?.LogInformation("[{RemoteEndPoint}] connection closed", remoteEndPoint);
    }

    bool CanAccept(IMuninNodeClient client)
    {
      if (client.EndPoint is not IPEndPoint remoteIPEndPoint) {
        Logger?.LogWarning(
          "cannot accept {RemoteEndPoint} ({RemoteEndPointAddressFamily})",
          client.EndPoint?.ToString() ?? "(null)",
          client.EndPoint?.AddressFamily
        );

        return false;
      }

      if (accessRule is not null && !accessRule.IsAcceptable(remoteIPEndPoint)) {
        Logger?.LogWarning("access refused: {RemoteEndPoint}", remoteIPEndPoint);

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
    cancellationToken.ThrowIfCancellationRequested();

    // holds a reference to the endpoint before the client being disposed
    var remoteEndPoint = client.EndPoint;
    var sessionId = GenerateSessionId(server!.EndPoint, remoteEndPoint);

    Logger?.LogDebug("[{RemoteEndPoint}] sending banner", remoteEndPoint);

    try {
      await SendResponseAsync(
        client,
        $"# munin node at {HostName}",
        cancellationToken
      ).ConfigureAwait(false);
    }
    catch (MuninNodeClientDisconnectedException) {
      Logger?.LogWarning(
        "[{RemoteEndPoint}] client closed session while sending banner",
        remoteEndPoint
      );

      return;
    }
#pragma warning disable CA1031
    catch (Exception ex) {
      Logger?.LogCritical(
        ex,
        "[{RemoteEndPoint}] unexpected exception occured while sending banner",
        remoteEndPoint
      );

      return;
    }
#pragma warning restore CA1031

    cancellationToken.ThrowIfCancellationRequested();

    Logger?.LogInformation("[{RemoteEndPoint}] session started; ID={SessionId}", remoteEndPoint, sessionId);

    try {
      if (PluginProvider.SessionCallback is not null)
        await PluginProvider.SessionCallback.ReportSessionStartedAsync(sessionId, cancellationToken).ConfigureAwait(false);

      foreach (var plugin in PluginProvider.Plugins) {
        if (plugin.SessionCallback is not null)
          await plugin.SessionCallback.ReportSessionStartedAsync(sessionId, cancellationToken).ConfigureAwait(false);
      }

      // https://docs.microsoft.com/ja-jp/dotnet/standard/io/pipelines
      var pipe = new Pipe();

      await Task.WhenAll(
        ReceiveCommandAsync(client, remoteEndPoint, pipe.Writer, cancellationToken),
        ProcessCommandAsync(client, remoteEndPoint, pipe.Reader, cancellationToken)
      ).ConfigureAwait(false);

      Logger?.LogInformation("[{RemoteEndPoint}] session closed; ID={SessionId}", remoteEndPoint, sessionId);
    }
    finally {
      foreach (var plugin in PluginProvider.Plugins) {
        if (plugin.SessionCallback is not null)
          await plugin.SessionCallback.ReportSessionClosedAsync(sessionId, cancellationToken).ConfigureAwait(false);
      }

      if (PluginProvider.SessionCallback is not null)
        await PluginProvider.SessionCallback.ReportSessionClosedAsync(sessionId, cancellationToken).ConfigureAwait(false);
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
    EndPoint? remoteEndPoint,
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
        Logger?.LogInformation(
          "[{RemoteEndPoint}] operation canceled",
          remoteEndPoint
        );
        throw;
      }
#pragma warning disable CA1031
      catch (Exception ex) {
        Logger?.LogError(
          ex,
          "[{RemoteEndPoint}] unexpected exception while receiving",
          remoteEndPoint
        );
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
    EndPoint? remoteEndPoint,
    PipeReader reader,
    CancellationToken cancellationToken
  )
  {
    for (; ; ) {
      cancellationToken.ThrowIfCancellationRequested();

      var result = await reader.ReadAsync(
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);
      var buffer = result.Buffer;

      try {
        while (TryReadLine(ref buffer, out var line)) {
          await RespondToCommandAsync(
            client: client,
            commandLine: line,
            cancellationToken: cancellationToken
          ).ConfigureAwait(false);
        }
      }
      catch (MuninNodeClientDisconnectedException) {
        Logger?.LogInformation(
          "[{RemoteEndPoint}] client disconnected",
          remoteEndPoint
        );
        break; // expected exception
      }
      catch (OperationCanceledException) {
        Logger?.LogInformation(
          "[{RemoteEndPoint}] operation canceled",
          remoteEndPoint
        );
        throw;
      }
#pragma warning disable CA1031
      catch (Exception ex) {
        Logger?.LogCritical(
          ex,
          "[{RemoteEndPoint}] unexpected exception while processing command",
          remoteEndPoint
        );

        await client.DisconnectAsync(cancellationToken).ConfigureAwait(false);

        break;
      }
#pragma warning restore CA1031

      reader.AdvanceTo(buffer.Start, buffer.End);

      if (result.IsCompleted)
        break;
    }

    static bool TryReadLine(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line)
    {
      var reader = new SequenceReader<byte>(buffer);
      const byte LF = (byte)'\n';

      if (
        !reader.TryReadTo(out line, delimiter: "\r\n"u8, advancePastDelimiter: true) &&
        !reader.TryReadTo(out line, delimiter: LF, advancePastDelimiter: true)
      ) {
        line = default;
        return false;
      }

#if SYSTEM_BUFFERS_SEQUENCEREADER_UNREADSEQUENCE
      buffer = reader.UnreadSequence;
#else
      buffer = reader.Sequence.Slice(reader.Position);
#endif

      return true;
    }
  }

  private static bool ExpectCommand(
    ReadOnlySequence<byte> commandLine,
    ReadOnlySpan<byte> expectedCommand,
    out ReadOnlySequence<byte> arguments
  )
  {
    arguments = default;

    var reader = new SequenceReader<byte>(commandLine);

    if (!reader.IsNext(expectedCommand, advancePast: true))
      return false;

    const byte SP = (byte)' ';

    if (reader.Remaining == 0) {
      // <command> <EOL>
      arguments = default;
      return true;
    }
    else if (reader.IsNext(SP, advancePast: true)) {
      // <command> <SP> <arguments> <EOL>
#if SYSTEM_BUFFERS_SEQUENCEREADER_UNREADSEQUENCE
      arguments = reader.UnreadSequence;
#else
      arguments = reader.Sequence.Slice(reader.Position);
#endif
      return true;
    }

    return false;
  }

  private static readonly byte CommandQuitShort = (byte)'.';

  private ValueTask RespondToCommandAsync(
    IMuninNodeClient client,
    ReadOnlySequence<byte> commandLine,
    CancellationToken cancellationToken
  )
  {
    if (ExpectCommand(commandLine, "fetch"u8, out var fetchArguments)) {
      return ProcessCommandFetchAsync(client, fetchArguments, cancellationToken);
    }
    else if (ExpectCommand(commandLine, "nodes"u8, out _)) {
      return ProcessCommandNodesAsync(client, cancellationToken);
    }
    else if (ExpectCommand(commandLine, "list"u8, out var listArguments)) {
      return ProcessCommandListAsync(client, listArguments, cancellationToken);
    }
    else if (ExpectCommand(commandLine, "config"u8, out var configArguments)) {
      return ProcessCommandConfigAsync(client, configArguments, cancellationToken);
    }
    else if (
      ExpectCommand(commandLine, "quit"u8, out _) ||
      (commandLine.Length == 1 && commandLine.FirstSpan[0] == CommandQuitShort)
    ) {
      return client.DisconnectAsync(cancellationToken);
    }
    else if (ExpectCommand(commandLine, "cap"u8, out var capArguments)) {
      return ProcessCommandCapAsync(client, capArguments, cancellationToken);
    }
    else if (ExpectCommand(commandLine, "version"u8, out _)) {
      return ProcessCommandVersionAsync(client, cancellationToken);
    }
    else {
      return SendResponseAsync(
        client,
        "# Unknown command. Try cap, list, nodes, config, fetch, version or quit",
        cancellationToken
      );
    }
  }

#pragma warning disable IDE0230
  private static readonly ReadOnlyMemory<byte> EndOfLine = new[] { (byte)'\n' };
#pragma warning restore IDE0230

  private static readonly string[] ResponseLinesUnknownService = [
    "# Unknown service",
    ".",
  ];

  private ValueTask SendResponseAsync(
    IMuninNodeClient client,
    string responseLine,
    CancellationToken cancellationToken
  )
    => SendResponseAsync(
      client: client,
      responseLines: Enumerable.Repeat(responseLine, 1),
      cancellationToken: cancellationToken
    );

  private async ValueTask SendResponseAsync(
    IMuninNodeClient client,
    IEnumerable<string> responseLines,
    CancellationToken cancellationToken
  )
  {
    if (responseLines is null)
      throw new ArgumentNullException(nameof(responseLines));

    cancellationToken.ThrowIfCancellationRequested();

    try {
      foreach (var responseLine in responseLines) {
#if SYSTEM_TEXT_ENCODINGEXTENSIONS
        _ = Encoding.GetBytes(responseLine, responseBuffer);

        responseBuffer.Write(EndOfLine.Span);
#else
        var totalByteCount = Encoding.GetByteCount(responseLine) + EndOfLine.Length;
        var buffer = responseBuffer.GetMemory(totalByteCount);
        var bytesWritten = Encoding.GetBytes(responseLine, buffer.Span);

        EndOfLine.CopyTo(buffer[bytesWritten..]);

        bytesWritten += EndOfLine.Length;

        responseBuffer.Advance(bytesWritten);
#endif
      }

      await client.SendAsync(
        buffer: responseBuffer.WrittenMemory,
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);
    }
    finally {
#if SYSTEM_BUFFERS_ARRAYBUFFERWRITER_RESETWRITTENCOUNT
      responseBuffer.ResetWrittenCount();
#else
      responseBuffer.Clear();
#endif
    }
  }

  private ValueTask ProcessCommandNodesAsync(
    IMuninNodeClient client,
    CancellationToken cancellationToken
  )
  {
    return SendResponseAsync(
      client: client,
      responseLines: [
        HostName,
        ".",
      ],
      cancellationToken: cancellationToken
    );
  }

  private ValueTask ProcessCommandVersionAsync(
    IMuninNodeClient client,
    CancellationToken cancellationToken
  )
  {
    return SendResponseAsync(
      client: client,
      responseLine: $"munins node on {HostName} version: {NodeVersion}",
      cancellationToken: cancellationToken
    );
  }

  private ValueTask ProcessCommandCapAsync(
    IMuninNodeClient client,
#pragma warning disable IDE0060
    ReadOnlySequence<byte> arguments,
#pragma warning restore IDE0060
    CancellationToken cancellationToken
  )
  {
    // TODO: multigraph (https://guide.munin-monitoring.org/en/latest/plugin/protocol-multigraph.html)
    // TODO: dirtyconfig (https://guide.munin-monitoring.org/en/latest/plugin/protocol-dirtyconfig.html)
    // XXX: ignores capability arguments
    return SendResponseAsync(
      client: client,
      responseLine: "cap",
      cancellationToken: cancellationToken
    );
  }

  private ValueTask ProcessCommandListAsync(
    IMuninNodeClient client,
#pragma warning disable IDE0060
    ReadOnlySequence<byte> arguments,
#pragma warning restore IDE0060
    CancellationToken cancellationToken
  )
  {
    ThrowIfPluginProviderIsNull();

    // XXX: ignore [node] arguments
    return SendResponseAsync(
      client: client,
      responseLine: string.Join(" ", PluginProvider.Plugins.Select(static plugin => plugin.Name)),
      cancellationToken: cancellationToken
    );
  }

  private async ValueTask ProcessCommandFetchAsync(
    IMuninNodeClient client,
    ReadOnlySequence<byte> arguments,
    CancellationToken cancellationToken
  )
  {
    ThrowIfPluginProviderIsNull();

    var plugin = PluginProvider.Plugins.FirstOrDefault(
      plugin => string.Equals(Encoding.GetString(arguments), plugin.Name, StringComparison.Ordinal)
    );

    if (plugin is null) {
      await SendResponseAsync(
        client,
        ResponseLinesUnknownService,
        cancellationToken
      ).ConfigureAwait(false);

      return;
    }

    var responseLines = new List<string>(capacity: plugin.DataSource.Fields.Count + 1);

    foreach (var field in plugin.DataSource.Fields) {
      var valueString = await field.GetFormattedValueStringAsync(
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);

      responseLines.Add($"{field.Name}.value {valueString}");
    }

    responseLines.Add(".");

    await SendResponseAsync(
      client: client,
      responseLines: responseLines,
      cancellationToken: cancellationToken
    ).ConfigureAwait(false);
  }

  private static string? TranslateFieldDrawAttribute(PluginFieldGraphStyle style)
    => style switch {
      PluginFieldGraphStyle.Default => null,
      PluginFieldGraphStyle.Area => "AREA",
      PluginFieldGraphStyle.Stack => "STACK",
      PluginFieldGraphStyle.AreaStack => "AREASTACK",
      PluginFieldGraphStyle.Line => "LINE",
      PluginFieldGraphStyle.LineWidth1 => "LINE1",
      PluginFieldGraphStyle.LineWidth2 => "LINE2",
      PluginFieldGraphStyle.LineWidth3 => "LINE3",
      PluginFieldGraphStyle.LineStack => "LINESTACK",
      PluginFieldGraphStyle.LineStackWidth1 => "LINE1STACK",
      PluginFieldGraphStyle.LineStackWidth2 => "LINE2STACK",
      PluginFieldGraphStyle.LineStackWidth3 => "LINE3STACK",
      _ => throw new InvalidOperationException($"undefined draw attribute value: ({(int)style} {style})"),
    };

  private ValueTask ProcessCommandConfigAsync(
    IMuninNodeClient client,
    ReadOnlySequence<byte> arguments,
    CancellationToken cancellationToken
  )
  {
    ThrowIfPluginProviderIsNull();

    var plugin = PluginProvider.Plugins.FirstOrDefault(
      plugin => string.Equals(Encoding.GetString(arguments), plugin.Name, StringComparison.Ordinal)
    );

    if (plugin is null) {
      return SendResponseAsync(
        client,
        ResponseLinesUnknownService,
        cancellationToken
      );
    }

    var responseLines = new List<string>(capacity: 20);

    responseLines.AddRange(
      plugin.GraphAttributes.EnumerateAttributes()
    );

    // The fields referenced by {fieldname}.negative must be defined ahread of others,
    // and thus lists the negative field settings first.
    // Otherwise, the following error occurs when generating the graph.
    // "[RRD ERROR] Unable to graph /var/cache/munin/www/XXX.png : undefined v name XXXXXXXXXXXXXX"
    var orderedFields = plugin.DataSource.Fields.OrderBy(f => IsNegativeField(f, plugin.DataSource.Fields) ? 0 : 1);

    foreach (var field in orderedFields) {
      var fieldAttrs = field.Attributes;
      bool? graph = null;

      responseLines.Add($"{field.Name}.label {fieldAttrs.Label}");

      var draw = TranslateFieldDrawAttribute(fieldAttrs.GraphStyle);

      if (draw is not null)
        responseLines.Add($"{field.Name}.draw {draw}");

      if (fieldAttrs.NormalRangeForWarning.HasValue)
        AddFieldValueRange("warning", fieldAttrs.NormalRangeForWarning);

      if (fieldAttrs.NormalRangeForCritical.HasValue)
        AddFieldValueRange("critical", fieldAttrs.NormalRangeForCritical);

      if (!string.IsNullOrEmpty(fieldAttrs.NegativeFieldName)) {
        var negativeField = plugin.DataSource.Fields.FirstOrDefault(
          f => string.Equals(fieldAttrs.NegativeFieldName, f.Name, StringComparison.Ordinal)
        );

        if (negativeField is not null)
          responseLines.Add($"{field.Name}.negative {negativeField.Name}");
      }

      // this field is defined as the negative field of other field, so should not be graphed
      if (IsNegativeField(field, plugin.DataSource.Fields))
        graph = false;

      if (graph is bool drawGraph)
        responseLines.Add($"{field.Name}.graph {(drawGraph ? "yes" : "no")}");

      void AddFieldValueRange(string attr, PluginFieldNormalValueRange range)
      {
        if (range.Min.HasValue && range.Max.HasValue)
          responseLines.Add($"{field.Name}.{attr} {range.Min.Value}:{range.Max.Value}");
        else if (range.Min.HasValue)
          responseLines.Add($"{field.Name}.{attr} {range.Min.Value}:");
        else if (range.Max.HasValue)
          responseLines.Add($"{field.Name}.{attr} :{range.Max.Value}");
      }
    }

    responseLines.Add(".");

    return SendResponseAsync(
      client: client,
      responseLines: responseLines,
      cancellationToken: cancellationToken
    );

    static bool IsNegativeField(IPluginField field, IReadOnlyCollection<IPluginField> fields)
      => fields.Any(
        f => string.Equals(field.Name, f.Attributes.NegativeFieldName, StringComparison.Ordinal)
      );
  }
}
