// SPDX-FileCopyrightText: 2021 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

// TODO: use LoggerMessage.Define
#pragma warning disable CA1848 // For improved performance, use the LoggerMessage delegates instead of calling 'LoggerExtensions.LogInformation(ILogger, string?, params object?[])'

#if NET6_0_OR_GREATER
#define SYSTEM_NET_SOCKETS_SOCKET_ACCEPTASYNC_CANCELLATIONTOKEN
#define SYSTEM_NET_SOCKETS_SOCKET_DISCONNECTASYNC_BOOL_CANCELLATIONTOKEN
#endif

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

namespace Smdn.Net.MuninNode;

public abstract class NodeBase : IDisposable, IAsyncDisposable {
  private static readonly Version defaultNodeVersion = new(1, 0, 0, 0);

  public IReadOnlyCollection<IPlugin> Plugins { get; }
  public string HostName { get; }

  public virtual Version NodeVersion => defaultNodeVersion;
  public virtual Encoding Encoding => Encoding.Default;

  private readonly ILogger? logger;
  private Socket? server;

  public NodeBase(
    IReadOnlyCollection<IPlugin> plugins,
    string hostName,
    ILogger? logger
  )
  {
    Plugins = plugins ?? throw new ArgumentNullException(nameof(plugins));

    if (hostName == null)
      throw new ArgumentNullException(nameof(hostName));
    if (hostName.Length == 0)
      throw ExceptionUtils.CreateArgumentMustBeNonEmptyString(nameof(hostName));

    HostName = hostName;

    this.logger = logger;
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

  protected virtual
#if SYSTEM_NET_SOCKETS_SOCKET_DISCONNECTASYNC_BOOL_CANCELLATIONTOKEN
  async
#endif
  ValueTask DisposeAsyncCore()
  {
    try {
      if (server is not null && server.Connected) {
#if SYSTEM_NET_SOCKETS_SOCKET_DISCONNECTASYNC_BOOL_CANCELLATIONTOKEN
        await server.DisconnectAsync(reuseSocket: false);
#else
        server.Disconnect(reuseSocket: false);
#endif
      }
    }
    catch (SocketException) {
      // swallow
    }

    server?.Close();
    server?.Dispose();
    server = null;

#if !SYSTEM_NET_SOCKETS_SOCKET_DISCONNECTASYNC_BOOL_CANCELLATIONTOKEN
    return default;
#endif
  }

  protected virtual void Dispose(bool disposing)
  {
    if (!disposing)
      return;

    try {
      if (server is not null && server.Connected)
        server.Disconnect(reuseSocket: false);
    }
    catch (SocketException) {
      // swallow
    }

    server?.Close();
    server?.Dispose();
    server = null!;
  }

  protected abstract Socket CreateServerSocket();

  public void Start()
  {
    if (server is not null)
      throw new InvalidOperationException("already started");

    logger?.LogInformation($"starting");

    server = CreateServerSocket() ?? throw new InvalidOperationException("cannot start server");

    logger?.LogInformation("started (end point: {LocalEndPoint})", server.LocalEndPoint);
  }

  protected abstract bool IsClientAcceptable(IPEndPoint remoteEndPoint);

  public async ValueTask AcceptClientAsync(
    CancellationToken cancellationToken = default
  )
  {
    if (server is null)
      throw new InvalidOperationException("not started or already closed");

    logger?.LogInformation("accepting...");

    var client = await server
#if SYSTEM_NET_SOCKETS_SOCKET_ACCEPTASYNC_CANCELLATIONTOKEN
      .AcceptAsync(cancellationToken: cancellationToken)
#else
      .AcceptAsync()
#endif
      .ConfigureAwait(false);

    IPEndPoint? remoteEndPoint = null;

    try {
      cancellationToken.ThrowIfCancellationRequested();

      remoteEndPoint = client.RemoteEndPoint as IPEndPoint;

      if (remoteEndPoint is null) {
        logger?.LogWarning(
          "cannot accept {RemoteEndPoint} ({RemoteEndPointAddressFamily})",
          client.RemoteEndPoint?.ToString() ?? "(null)",
          client.RemoteEndPoint?.AddressFamily
        );
        return;
      }

      if (!IsClientAcceptable(remoteEndPoint)) {
        logger?.LogWarning("access refused: {RemoteEndPoint}", remoteEndPoint);
        return;
      }

      var sessionId = GenerateSessionId(server.LocalEndPoint, remoteEndPoint);

      cancellationToken.ThrowIfCancellationRequested();

      logger?.LogDebug("[{RemoteEndPoint}] sending banner", remoteEndPoint);

      try {
        await SendResponseAsync(
          client,
          Encoding,
          $"# munin node at {HostName}",
          cancellationToken
        ).ConfigureAwait(false);
      }
      catch (SocketException ex) when (
        ex.SocketErrorCode is
          SocketError.Shutdown or // EPIPE (32)
          SocketError.ConnectionAborted or // WSAECONNABORTED (10053)
          SocketError.OperationAborted or // ECANCELED (125)
          SocketError.ConnectionReset // ECONNRESET (104)
      ) {
        logger?.LogWarning(
          "[{RemoteEndPoint}] client closed session while sending banner",
          remoteEndPoint
        );

        return;
      }
      catch (Exception ex) {
        logger?.LogCritical(
          ex,
          "[{RemoteEndPoint}] unexpected exception occured while sending banner",
          remoteEndPoint
        );

        return;
      }

      cancellationToken.ThrowIfCancellationRequested();

      logger?.LogInformation("[{RemoteEndPoint}] session started; ID={SessionId}", remoteEndPoint, sessionId);

      try {
        foreach (var plugin in Plugins) {
          if (plugin.SessionCallback is not null)
            await plugin.SessionCallback.ReportSessionStartedAsync(sessionId, cancellationToken).ConfigureAwait(false);
        }

        // https://docs.microsoft.com/ja-jp/dotnet/standard/io/pipelines
        var pipe = new Pipe();

        await Task.WhenAll(
          FillAsync(client, remoteEndPoint, pipe.Writer, cancellationToken),
          ReadAsync(client, remoteEndPoint, pipe.Reader, cancellationToken)
        ).ConfigureAwait(false);

        logger?.LogInformation("[{RemoteEndPoint}] session closed; ID={SessionId}", remoteEndPoint, sessionId);
      }
      finally {
        foreach (var plugin in Plugins) {
          if (plugin.SessionCallback is not null)
            await plugin.SessionCallback.ReportSessionClosedAsync(sessionId, cancellationToken).ConfigureAwait(false);
        }
      }
    }
    finally {
      client.Close();

      logger?.LogInformation("[{RemoteEndPoint}] connection closed", remoteEndPoint);
    }

    static string GenerateSessionId(EndPoint? localEndPoint, IPEndPoint remoteEndPoint)
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

    async Task FillAsync(
      Socket socket,
      IPEndPoint remoteEndPoint,
      PipeWriter writer,
      CancellationToken cancellationToken
    )
    {
      const int minimumBufferSize = 256;

      for (; ; ) {
        cancellationToken.ThrowIfCancellationRequested();

        var memory = writer.GetMemory(minimumBufferSize);

        try {
          if (!socket.Connected)
            break;

          var bytesRead = await socket.ReceiveAsync(
            buffer: memory,
            socketFlags: SocketFlags.None,
            cancellationToken: cancellationToken
          ).ConfigureAwait(false);

          if (bytesRead == 0)
            break;

          writer.Advance(bytesRead);
        }
        catch (SocketException ex) when (
          ex.SocketErrorCode is
            SocketError.OperationAborted or // ECANCELED (125)
            SocketError.ConnectionReset // ECONNRESET (104)
        ) {
          logger?.LogInformation(
            "[{RemoteEndPoint}] expected socket exception ({NumericSocketErrorCode} {SocketErrorCode})",
            remoteEndPoint,
            (int)ex.SocketErrorCode,
            ex.SocketErrorCode
          );
          break; // expected exception
        }
        catch (ObjectDisposedException) {
          logger?.LogInformation(
            "[{RemoteEndPoint}] socket has been disposed",
            remoteEndPoint
          );
          break; // expected exception
        }
        catch (OperationCanceledException) {
          logger?.LogInformation(
            "[{RemoteEndPoint}] operation canceled",
            remoteEndPoint
          );
          throw;
        }
        catch (Exception ex) {
          logger?.LogCritical(
            ex,
            "[{RemoteEndPoint}] unexpected exception while receiving",
            remoteEndPoint
          );
          break;
        }

        var result = await writer.FlushAsync(
          cancellationToken: cancellationToken
        ).ConfigureAwait(false);

        if (result.IsCompleted)
          break;
      }

      await writer.CompleteAsync().ConfigureAwait(false);
    }

    async Task ReadAsync(
      Socket socket,
      IPEndPoint remoteEndPoint,
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
            await ProcessCommandAsync(
              client: socket,
              commandLine: line,
              cancellationToken: cancellationToken
            ).ConfigureAwait(false);
          }
        }
        catch (OperationCanceledException) {
          logger?.LogInformation(
            "[{RemoteEndPoint}] operation canceled",
            remoteEndPoint
          );
          throw;
        }
        catch (Exception ex) {
          logger?.LogCritical(
            ex,
            "[{RemoteEndPoint}] unexpected exception while processing command",
            remoteEndPoint
          );

          if (socket.Connected)
            socket.Close();
          break;
        }

        reader.AdvanceTo(buffer.Start, buffer.End);

        if (result.IsCompleted)
          break;
      }
    }

    static bool TryReadLine(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line)
    {
      var reader = new SequenceReader<byte>(buffer);
      const byte LF = (byte)'\n';

#pragma warning disable SA1003
      if (
#if LANG_VERSION_11_OR_GREATER
        !reader.TryReadTo(out line, delimiter: "\r\n"u8, advancePastDelimiter: true)
#else
        !reader.TryReadTo(out line, delimiter: CRLF.Span, advancePastDelimiter: true)
#endif
        &&
        !reader.TryReadTo(out line, delimiter: LF, advancePastDelimiter: true)
      ) {
        line = default;
        return false;
      }
#pragma warning restore SA1003

#if SYSTEM_BUFFERS_SEQUENCEREADER_UNREADSEQUENCE
      buffer = reader.UnreadSequence;
#else
      buffer = reader.Sequence.Slice(reader.Position);
#endif

      return true;
    }
  }

#if !LANG_VERSION_11_OR_GREATER
  private static readonly ReadOnlyMemory<byte> CRLF = Encoding.ASCII.GetBytes("\r\n");
#endif

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

  private static readonly byte commandQuitShort = (byte)'.';

#if LANG_VERSION_11_OR_GREATER
  private ValueTask ProcessCommandAsync(
    Socket client,
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
      (commandLine.Length == 1 && commandLine.FirstSpan[0] == commandQuitShort)
    ) {
      client.Close();
#if SYSTEM_THREADING_TASKS_VALUETASK_COMPLETEDTASK
      return ValueTask.CompletedTask;
#else
      return default;
#endif
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
        Encoding,
        "# Unknown command. Try cap, list, nodes, config, fetch, version or quit",
        cancellationToken
      );
    }
  }
#else
  private static readonly ReadOnlyMemory<byte> commandFetch       = Encoding.ASCII.GetBytes("fetch");
  private static readonly ReadOnlyMemory<byte> commandNodes       = Encoding.ASCII.GetBytes("nodes");
  private static readonly ReadOnlyMemory<byte> commandList        = Encoding.ASCII.GetBytes("list");
  private static readonly ReadOnlyMemory<byte> commandConfig      = Encoding.ASCII.GetBytes("config");
  private static readonly ReadOnlyMemory<byte> commandQuit        = Encoding.ASCII.GetBytes("quit");
  private static readonly ReadOnlyMemory<byte> commandCap         = Encoding.ASCII.GetBytes("cap");
  private static readonly ReadOnlyMemory<byte> commandVersion     = Encoding.ASCII.GetBytes("version");

  private ValueTask ProcessCommandAsync(
    Socket client,
    ReadOnlySequence<byte> commandLine,
    CancellationToken cancellationToken
  )
  {
    cancellationToken.ThrowIfCancellationRequested();

    if (ExpectCommand(commandLine, commandFetch.Span, out var fetchArguments)) {
      return ProcessCommandFetchAsync(client, fetchArguments, cancellationToken);
    }
    else if (ExpectCommand(commandLine, commandNodes.Span, out _)) {
      return ProcessCommandNodesAsync(client, cancellationToken);
    }
    else if (ExpectCommand(commandLine, commandList.Span, out var listArguments)) {
      return ProcessCommandListAsync(client, listArguments, cancellationToken);
    }
    else if (ExpectCommand(commandLine, commandConfig.Span, out var configArguments)) {
      return ProcessCommandConfigAsync(client, configArguments, cancellationToken);
    }
    else if (
      ExpectCommand(commandLine, commandQuit.Span, out _) ||
      (commandLine.Length == 1 && commandLine.FirstSpan[0] == commandQuitShort)
    ) {
      client.Close();
#if SYSTEM_THREADING_TASKS_VALUETASK_COMPLETEDTASK
      return ValueTask.CompletedTask;
#else
      return default;
#endif
    }
    else if (ExpectCommand(commandLine, commandCap.Span, out var capArguments)) {
      return ProcessCommandCapAsync(client, capArguments, cancellationToken);
    }
    else if (ExpectCommand(commandLine, commandVersion.Span, out _)) {
      return ProcessCommandVersionAsync(client, cancellationToken);
    }
    else {
      return SendResponseAsync(
        client: client,
        encoding: Encoding,
        responseLine: "# Unknown command. Try cap, list, nodes, config, fetch, version or quit",
        cancellationToken: cancellationToken
      );
    }
  }
#endif

#pragma warning disable IDE0230
  private static readonly ReadOnlyMemory<byte> endOfLine = new[] { (byte)'\n' };
#pragma warning restore IDE0230

  private static ValueTask SendResponseAsync(
    Socket client,
    Encoding encoding,
    string responseLine,
    CancellationToken cancellationToken
  )
    => SendResponseAsync(
      client: client,
      encoding: encoding,
      responseLines: Enumerable.Repeat(responseLine, 1),
      cancellationToken: cancellationToken
    );

  private static async ValueTask SendResponseAsync(
    Socket client,
    Encoding encoding,
    IEnumerable<string> responseLines,
    CancellationToken cancellationToken
  )
  {
    if (responseLines == null)
      throw new ArgumentNullException(nameof(responseLines));

    cancellationToken.ThrowIfCancellationRequested();

    foreach (var responseLine in responseLines) {
      var resp = encoding.GetBytes(responseLine);

      await client.SendAsync(
        buffer: resp,
        socketFlags: SocketFlags.None,
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);

      await client.SendAsync(
        buffer: endOfLine,
        socketFlags: SocketFlags.None,
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);
    }
  }

  private ValueTask ProcessCommandNodesAsync(
    Socket client,
    CancellationToken cancellationToken
  )
  {
    return SendResponseAsync(
      client: client,
      encoding: Encoding,
      responseLines: new[] {
        HostName,
        ".",
      },
      cancellationToken: cancellationToken
    );
  }

  private ValueTask ProcessCommandVersionAsync(
    Socket client,
    CancellationToken cancellationToken
  )
  {
    return SendResponseAsync(
      client: client,
      encoding: Encoding,
      responseLine: $"munins node on {HostName} version: {NodeVersion}",
      cancellationToken: cancellationToken
    );
  }

  private ValueTask ProcessCommandCapAsync(
    Socket client,
#pragma warning disable IDE0060
    ReadOnlySequence<byte> arguments,
#pragma warning restore IDE0060
    CancellationToken cancellationToken
  )
  {
    // TODO: multigraph (http://guide.munin-monitoring.org/en/latest/plugin/protocol-multigraph.html)
    // TODO: dirtyconfig (http://guide.munin-monitoring.org/en/latest/plugin/protocol-dirtyconfig.html)
    // XXX: ignores capability arguments
    return SendResponseAsync(
      client: client,
      encoding: Encoding,
      responseLine: "cap",
      cancellationToken: cancellationToken
    );
  }

  private ValueTask ProcessCommandListAsync(
    Socket client,
#pragma warning disable IDE0060
    ReadOnlySequence<byte> arguments,
#pragma warning restore IDE0060
    CancellationToken cancellationToken
  )
  {
    // XXX: ignore [node] arguments
    return SendResponseAsync(
      client: client,
      encoding: Encoding,
      responseLine: string.Join(" ", Plugins.Select(static plugin => plugin.Name)),
      cancellationToken: cancellationToken
    );
  }

  private async ValueTask ProcessCommandFetchAsync(
    Socket client,
    ReadOnlySequence<byte> arguments,
    CancellationToken cancellationToken
  )
  {
    var plugin = Plugins.FirstOrDefault(
      plugin => string.Equals(Encoding.GetString(arguments), plugin.Name, StringComparison.Ordinal)
    );

    if (plugin == null) {
      await SendResponseAsync(
        client,
        Encoding,
        new[] {
          "# Unknown service",
          ".",
        },
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
      encoding: Encoding,
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
    Socket client,
    ReadOnlySequence<byte> arguments,
    CancellationToken cancellationToken
  )
  {
    var plugin = Plugins.FirstOrDefault(
      plugin => string.Equals(Encoding.GetString(arguments), plugin.Name, StringComparison.Ordinal)
    );

    if (plugin == null) {
      return SendResponseAsync(
        client,
        Encoding,
        new[] {
          "# Unknown service",
          ".",
        },
        cancellationToken
      );
    }

    var graphAttrs = plugin.GraphAttributes;

    var responseLines = new List<string>() {
      $"graph_title {graphAttrs.Title}",
      $"graph_category {graphAttrs.Category}",
      $"graph_args {graphAttrs.Arguments}",
      $"graph_scale {(graphAttrs.Scale ? "yes" : "no")}",
      $"graph_vlabel {graphAttrs.VerticalLabel}",
      $"update_rate {(int)graphAttrs.UpdateRate.TotalSeconds}",
    };

    if (graphAttrs.Width.HasValue)
      responseLines.Add($"graph_width {graphAttrs.Width.Value}");
    if (graphAttrs.Height.HasValue)
      responseLines.Add($"graph_height {graphAttrs.Height.Value}");

    foreach (var field in plugin.DataSource.Fields) {
      var fieldAttrs = field.Attributes;

      responseLines.Add($"{field.Name}.label {fieldAttrs.Label}");

      var draw = TranslateFieldDrawAttribute(fieldAttrs.GraphStyle);

      if (draw is not null)
        responseLines.Add($"{field.Name}.draw {draw}");

#if false
      // TODO: add support for "field.warning integer" and "field.warning integer:integer"
      if (fieldAttrs.WarningValueRange.HasValue) {
        var warningRange = fieldAttrs.WarningValueRange.Value;

        responseLines.Add($"{field.Name}.warning {warningRange.Start}:{warningRange.End}");
      }

      if (fieldAttrs.CriticalValueRange.HasValue) {
        var criticalRange = fieldAttrs.CriticalValueRange.Value;

        responseLines.Add($"{field.Name}.critical {criticalRange.Start}:{criticalRange.End}");
      }
#endif
    }

    responseLines.Add(".");

    return SendResponseAsync(
      client: client,
      encoding: Encoding,
      responseLines: responseLines,
      cancellationToken: cancellationToken
    );
  }
}
