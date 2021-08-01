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

using Smdn.Net.MuninPlugin;

namespace Smdn.Net.MuninNode {
  public class LocalNode : IDisposable {
    public const int DefaultAlternativePortNumber = 44693;

    private static readonly int maxClients = 1;
    private static readonly Version defaultNodeVersion = new Version(1, 0, 0, 0);

    public IReadOnlyList<Plugin> Plugins { get; }
    public string HostName { get; }
    public TimeSpan Timeout { get; }
    public IPEndPoint LocalEndPoint { get; }

    private readonly Version nodeVersion;
    private Socket server;

    public LocalNode(
      IReadOnlyList<Plugin> plugins,
      string hostName,
      TimeSpan timeout,
      int portNumber,
      Version nodeVersion = null
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
      server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
      server.Bind(LocalEndPoint);
      server.Listen(maxClients);
    }

    public async Task AcceptClient()
    {
      Console.WriteLine("<munin-node> accepting...");

      var client = await server.AcceptAsync().ConfigureAwait(false);

      try {
        var remoteEndPoint = client.RemoteEndPoint as IPEndPoint;

        if (remoteEndPoint == null) {
          Console.WriteLine($"<munin-node> cannot accept: {client.RemoteEndPoint.AddressFamily}");
          return;
        }

        if (!IPAddress.IsLoopback(remoteEndPoint.Address)) {
          Console.WriteLine($"<munin-node> access refused: {client.RemoteEndPoint}");
          return;
        }

        Console.WriteLine($"<munin-node> session started (master: {client.RemoteEndPoint})");

        await SendResponseAsync(client, $"# munin node at {HostName}").ConfigureAwait(false);

        // https://docs.microsoft.com/ja-jp/dotnet/standard/io/pipelines
        var pipe = new Pipe();

        await Task.WhenAll(
          FillAsync(client, pipe.Writer),
          ReadAsync(client, pipe.Reader)
        ).ConfigureAwait(false);

        Console.WriteLine("<munin-node> session ending");
      }
      finally {
        client.Close();

        Console.WriteLine("<munin-node> connection closed");
      }

      static async Task FillAsync(Socket socket, PipeWriter writer)
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
            Console.Error.WriteLine(ex);
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
              await ProcessCommandAsync(socket, ToString(line).TrimEnd()).ConfigureAwait(false);
            }
          }
          catch (Exception ex) {
            if (socket.Connected)
              socket.Close();
            Console.Error.WriteLine(ex);
            break;
          }

          reader.AdvanceTo(buffer.Start, buffer.End);

          if (result.IsCompleted)
            break;
        }
      }

      static bool TryReadLine(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line)
      {
        var endOfLine = buffer.PositionOf((byte)'\n');

        if (endOfLine == null) {
          line = default;
          return false;
        }

        line = buffer.Slice(0, endOfLine.Value); // may contain '\r' if ends with '\r\n'
        buffer = buffer.Slice(buffer.GetPosition(1, endOfLine.Value));

        return true;
      }

      static string ToString(ReadOnlySequence<byte> sequence)
      {
        return string.Create((int)sequence.Length, sequence, (span, seq) => {
          var index = 0;
          var position = seq.Start;

          while (seq.TryGet(ref position, out var memory, advance: true)) {
            var s = memory.Span;

            for (var i = 0; i < s.Length && index < span.Length; i++, index++) {
              span[index] = (char)s[i];
            }
          }
        });
      }
    }

    private static bool ExpectCommand(string command, string expectedCommand, out string arguments)
    {
      arguments = string.Empty;

      if (command.Length == expectedCommand.Length) {
        // <command> <LF>
        return command.Equals(expectedCommand, StringComparison.Ordinal);
      }
      else if (expectedCommand.Length < command.Length && command[expectedCommand.Length] == ' ') {
        // <command> <SP> <arguments> <LF>
        arguments = command.Substring(expectedCommand.Length + 1);
        return command.StartsWith(expectedCommand, StringComparison.Ordinal);
      }

      return false;
    }

    private Task ProcessCommandAsync(Socket client, string command)
    {
      if (ExpectCommand(command, "fetch", out var fetchArguments))
        return ProcessCommandFetch(client, command, fetchArguments);
      else if (ExpectCommand(command, "nodes", out _))
        return ProcessCommandNodes(client, command);
      else if (ExpectCommand(command, "list", out var listArguments))
        return ProcessCommandList(client, command, listArguments);
      else if (ExpectCommand(command, "config", out var configArguments))
        return ProcessCommandConfig(client, command, configArguments);
      else if (ExpectCommand(command, "quit", out _) || command.Equals(".")) {
        client.Close();
        return Task.CompletedTask;
      }
      else if (ExpectCommand(command, "cap", out var capArguments))
        return ProcessCommandCap(client, command, capArguments);
      else if (ExpectCommand(command, "version", out _))
        return ProcessCommandVersion(client, command);
      else
        return SendResponseAsync(client, "# Unknown command. Try cap, list, nodes, config, fetch, version or quit");
    }

    private static readonly byte[] endOfLine = new[] { (byte)'\n' };

    private static Task SendResponseAsync(Socket client, string responseLine)
      => SendResponseAsync(client, Enumerable.Repeat(responseLine, 1));

    private static async Task SendResponseAsync(Socket client, IEnumerable<string> responseLines)
    {
      if (responseLines == null)
        throw new ArgumentNullException(nameof(responseLines));

      foreach (var responseLine in responseLines) {
        var resp = Encoding.ASCII.GetBytes(responseLine);

        await client.SendAsync(resp, SocketFlags.None).ConfigureAwait(false);
        await client.SendAsync(endOfLine, SocketFlags.None).ConfigureAwait(false);
      }
    }

    private Task ProcessCommandNodes(Socket client, string command)
    {
      return SendResponseAsync(
        client,
        new[] {
          HostName,
          "."
        }
      );
    }

    private Task ProcessCommandVersion(Socket client, string command)
    {
      return SendResponseAsync(
        client,
        $"munins node on {HostName} version: {nodeVersion}"
      );
    }

    private Task ProcessCommandCap(Socket client, string command, string arguments)
    {
      // TODO: multigraph (http://guide.munin-monitoring.org/en/latest/plugin/protocol-multigraph.html)
      // TODO: dirtyconfig (http://guide.munin-monitoring.org/en/latest/plugin/protocol-dirtyconfig.html)
      // XXX: ignores capability arguments
      return SendResponseAsync(
        client,
        "cap"
      );
    }

    private Task ProcessCommandList(Socket client, string command, string arguments)
    {
      // XXX: ignore [node] arguments
      return SendResponseAsync(
        client,
        string.Join(" ", Plugins.Select(plugin => plugin.Name))
      );
    }

    private Task ProcessCommandFetch(Socket client, string command, string arguments)
    {
      var plugin = Plugins.FirstOrDefault(plugin => string.Equals(arguments, plugin.Name, StringComparison.Ordinal));

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

    private Task ProcessCommandConfig(Socket client, string command, string arguments)
    {
      var plugin = Plugins.FirstOrDefault(plugin => string.Equals(arguments, plugin.Name, StringComparison.Ordinal));

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