// SPDX-FileCopyrightText: 2021 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

#define ENABLE_IPv6
#undef ENABLE_IPv6

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Smdn.Net.MuninPlugin;

namespace Smdn.Net.MuninNode {
  public class LocalNode : IDisposable {
    private static readonly int maxClients = 1;
    private static readonly Version defaultNodeVersion = new Version(1, 0, 0, 0);

    public IReadOnlyList<Plugin> Plugins { get; }
    public string HostName { get; }
    public TimeSpan Timeout { get; }
    public IPEndPoint LocalEndPoint { get; }

    private readonly Version nodeVersion;
    private readonly ILogger logger;
    private Socket server;

    public LocalNode(
      IReadOnlyList<Plugin> plugins,
      string hostName,
      TimeSpan timeout,
      int portNumber,
      Version nodeVersion = null,
      IServiceProvider serviceProvider = null
    )
    {
      this.Plugins = plugins ?? throw new ArgumentNullException(nameof(plugins));

      if (hostName == null)
        throw new ArgumentNullException(nameof(hostName));
      if (hostName.Length == 0)
        throw ExceptionUtils.CreateArgumentMustBeNonEmptyString(nameof(hostName));

      this.HostName = hostName;
      this.Timeout = timeout;

      this.LocalEndPoint = new IPEndPoint(
#if ENABLE_IPv6
        IPAddress.IPv6Loopback,
#else
        IPAddress.Loopback,
#endif
        portNumber
      );

      this.nodeVersion = nodeVersion ?? defaultNodeVersion;

      this.server = new Socket(
#if ENABLE_IPv6
        AddressFamily.InterNetworkV6,
#else
        AddressFamily.InterNetwork,
#endif
        SocketType.Stream,
        ProtocolType.Tcp
      );

      this.logger = serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger<LocalNode>();
    }

    void IDisposable.Dispose()
    {
      server?.Disconnect(true);
      server?.Dispose();
      server = null;
    }

    public void Close() => (this as IDisposable).Dispose();

    public void Start()
    {
      logger?.LogInformation($"starting (end point: {LocalEndPoint})");

      server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
      server.Bind(LocalEndPoint);
      server.Listen(maxClients);

      logger?.LogInformation($"started");
    }

    public async Task AcceptClientAsync()
    {
      logger?.LogInformation("accepting...");

      var client = await server.AcceptAsync().ConfigureAwait(false);

      try {
        var remoteEndPoint = client.RemoteEndPoint as IPEndPoint;

        if (remoteEndPoint == null) {
          logger?.LogWarning($"cannot accept: {client.RemoteEndPoint.AddressFamily}");
          return;
        }

        if (!IPAddress.IsLoopback(remoteEndPoint.Address)) {
          logger?.LogWarning($"access refused: {client.RemoteEndPoint}");
          return;
        }

        logger?.LogInformation($"session started (master: {client.RemoteEndPoint})");

        await SendResponseAsync(client, $"# munin node at {HostName}").ConfigureAwait(false);

        // https://docs.microsoft.com/ja-jp/dotnet/standard/io/pipelines
        var pipe = new Pipe();

        await Task.WhenAll(
          FillAsync(client, pipe.Writer),
          ReadAsync(client, pipe.Reader)
        ).ConfigureAwait(false);

        logger?.LogInformation($"session ending");
      }
      finally {
        client.Close();

        logger?.LogInformation($"connection closed");
      }

      async Task FillAsync(Socket socket, PipeWriter writer)
      {
        const int minimumBufferSize = 256;

        for (;;) {
          var memory = writer.GetMemory(minimumBufferSize);

          try {
            if (!socket.Connected)
              break;

            var bytesRead = await socket.ReceiveAsync(memory, SocketFlags.None).ConfigureAwait(false);

            if (bytesRead == 0)
              break;

            writer.Advance(bytesRead);
          }
          catch (SocketException ex) when (ex.ErrorCode == 125) {
            break; // expected exception (125: Operation canceled)
          }
          catch (Exception ex) {
            logger?.LogError("unexpected exception", ex);
            break;
          }

          var result = await writer.FlushAsync().ConfigureAwait(false);

          if (result.IsCompleted)
            break;
        }

        await writer.CompleteAsync();
      }

      async Task ReadAsync(Socket socket, PipeReader reader)
      {
        for (;;) {
          var result = await reader.ReadAsync().ConfigureAwait(false);
          var buffer = result.Buffer;

          try {
            while (TryReadLine(ref buffer, out var line)) {
              await ProcessCommandAsync(socket, line).ConfigureAwait(false);
            }
          }
          catch (Exception ex) {
            if (socket.Connected)
              socket.Close();
            logger?.LogError("unexpected exception", ex);
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

        if (
          !reader.TryReadTo(out line, delimiter: CRLF.Span, advancePastDelimiter: true) &&
          !reader.TryReadTo(out line, delimiter: LF, advancePastDelimiter: true)
        ) {
          line = default;
          return false;
        }

#if NET5_0_OR_GREATER
        buffer = reader.UnreadSequence;
#else
        buffer = reader.Sequence.Slice(sequenceReader.Position);
#endif

        return true;
      }
    }

    private static readonly ReadOnlyMemory<byte> CRLF = Encoding.ASCII.GetBytes("\r\n");

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
#if NET5_0_OR_GREATER
        arguments = reader.UnreadSequence;
#else
        arguments = reader.Sequence.Slice(sequenceReader.Position);
#endif
        return true;
      }

      return false;
    }

    private static readonly ReadOnlyMemory<byte> commandFetch       = Encoding.ASCII.GetBytes("fetch");
    private static readonly ReadOnlyMemory<byte> commandNodes       = Encoding.ASCII.GetBytes("nodes");
    private static readonly ReadOnlyMemory<byte> commandList        = Encoding.ASCII.GetBytes("list");
    private static readonly ReadOnlyMemory<byte> commandConfig      = Encoding.ASCII.GetBytes("config");
    private static readonly ReadOnlyMemory<byte> commandQuit        = Encoding.ASCII.GetBytes("quit");
    private static readonly ReadOnlyMemory<byte> commandCap         = Encoding.ASCII.GetBytes("cap");
    private static readonly ReadOnlyMemory<byte> commandVersion     = Encoding.ASCII.GetBytes("version");
    private static readonly byte commandQuitShort = (byte)'.';

    private ValueTask ProcessCommandAsync(Socket client, ReadOnlySequence<byte> commandLine)
    {
      if (ExpectCommand(commandLine, commandFetch.Span, out var fetchArguments))
        return ProcessCommandFetchAsync(client, fetchArguments);
      else if (ExpectCommand(commandLine, commandNodes.Span, out _))
        return ProcessCommandNodesAsync(client);
      else if (ExpectCommand(commandLine, commandList.Span, out var listArguments))
        return ProcessCommandListAsync(client, listArguments);
      else if (ExpectCommand(commandLine, commandConfig.Span, out var configArguments))
        return ProcessCommandConfigAsync(client, configArguments);
      else if (
        ExpectCommand(commandLine, commandQuit.Span, out _) ||
        (commandLine.Length == 1 && commandLine.FirstSpan[0] == commandQuitShort)
      ) {
        client.Close();
#if NET5_0_OR_GREATER
        return ValueTask.CompletedTask;
#else
        return default(ValueTask);
#endif
      }
      else if (ExpectCommand(commandLine, commandCap.Span, out var capArguments))
        return ProcessCommandCapAsync(client, capArguments);
      else if (ExpectCommand(commandLine, commandVersion.Span, out _))
        return ProcessCommandVersionAsync(client);
      else
        return SendResponseAsync(client, "# Unknown command. Try cap, list, nodes, config, fetch, version or quit");
    }

    private static readonly byte[] endOfLine = new[] { (byte)'\n' };

    private static ValueTask SendResponseAsync(Socket client, string responseLine)
      => SendResponseAsync(client, Enumerable.Repeat(responseLine, 1));

    private static async ValueTask SendResponseAsync(Socket client, IEnumerable<string> responseLines)
    {
      if (responseLines == null)
        throw new ArgumentNullException(nameof(responseLines));

      foreach (var responseLine in responseLines) {
        var resp = Encoding.ASCII.GetBytes(responseLine);

        await client.SendAsync(resp, SocketFlags.None).ConfigureAwait(false);
        await client.SendAsync(endOfLine, SocketFlags.None).ConfigureAwait(false);
      }
    }

    private ValueTask ProcessCommandNodesAsync(Socket client)
    {
      return SendResponseAsync(
        client,
        new[] {
          HostName,
          "."
        }
      );
    }

    private ValueTask ProcessCommandVersionAsync(Socket client)
    {
      return SendResponseAsync(
        client,
        $"munins node on {HostName} version: {nodeVersion}"
      );
    }

    private ValueTask ProcessCommandCapAsync(Socket client, ReadOnlySequence<byte> arguments)
    {
      // TODO: multigraph (http://guide.munin-monitoring.org/en/latest/plugin/protocol-multigraph.html)
      // TODO: dirtyconfig (http://guide.munin-monitoring.org/en/latest/plugin/protocol-dirtyconfig.html)
      // XXX: ignores capability arguments
      return SendResponseAsync(
        client,
        "cap"
      );
    }

    private ValueTask ProcessCommandListAsync(Socket client, ReadOnlySequence<byte> arguments)
    {
      // XXX: ignore [node] arguments
      return SendResponseAsync(
        client,
        string.Join(" ", Plugins.Select(plugin => plugin.Name))
      );
    }

    private ValueTask ProcessCommandFetchAsync(Socket client, ReadOnlySequence<byte> arguments)
    {
      var plugin = Plugins.FirstOrDefault(plugin => string.Equals(Encoding.ASCII.GetString(arguments), plugin.Name, StringComparison.Ordinal));

      if (plugin == null) {
        return SendResponseAsync(
          client,
          new[] {
            "# Unknown service",
            ".",
          }
        );
      }

      return SendResponseAsync(
        client,
        plugin.FieldConfiguration.FetchFields().Select(f => $"{f.ID}.value {f.Value}").Append(".")
      );
    }

    private ValueTask ProcessCommandConfigAsync(Socket client, ReadOnlySequence<byte> arguments)
    {
      var plugin = Plugins.FirstOrDefault(plugin => string.Equals(Encoding.ASCII.GetString(arguments), plugin.Name, StringComparison.Ordinal));

      if (plugin == null) {
        return SendResponseAsync(
          client,
          new[] {
            "# Unknown service",
            ".",
          }
        );
      }

      var responseLines = new List<string>() {
        $"graph_title {plugin.GraphConfiguration.Title}",
        $"graph_category {plugin.GraphConfiguration.Category}",
        $"graph_args {plugin.GraphConfiguration.Arguments}",
        $"graph_scale {(plugin.GraphConfiguration.Scale ? "yes" : "no")}",
        $"graph_vlabel {plugin.GraphConfiguration.VerticalLabel}",
        $"update_rate {(int)plugin.GraphConfiguration.UpdateRate.TotalSeconds}",
      };

      if (plugin.GraphConfiguration.Width.HasValue)
        responseLines.Add($"graph_width {plugin.GraphConfiguration.Width.Value}");
      if (plugin.GraphConfiguration.Height.HasValue)
        responseLines.Add($"graph_height {plugin.GraphConfiguration.Height.Value}");

      foreach (var field in plugin.FieldConfiguration.FetchFields()) {
        responseLines.Add($"{field.ID}.label {field.Label}");

        var draw = field.GraphStyle;

        if (string.IsNullOrEmpty(draw))
          draw = plugin.FieldConfiguration.DefaultGraphStyle;

        if (!string.IsNullOrEmpty(draw))
          responseLines.Add($"{field.ID}.draw {draw}");

        // TODO: add support for "field.warning integer" and "field.warning integer:integer"
#if false
        if (plugin.FieldConfiguration.WarningValue.HasValue) {
          var warningRange = plugin.FieldConfiguration.WarningValue.Value;

          responseLines.Add($"{field.ID}.warning {warningRange.Start}:{warningRange.End}");
        }
        if (plugin.FieldConfiguration.CriticalValue.HasValue) {
          var criticalRange = plugin.FieldConfiguration.CriticalValue.Value;

          responseLines.Add($"{field.ID}.critical {criticalRange.Start}:{criticalRange.End}");
        }
#endif
      }

      responseLines.Add(".");

      return SendResponseAsync(
        client,
        responseLines
      );
    }
  }
}