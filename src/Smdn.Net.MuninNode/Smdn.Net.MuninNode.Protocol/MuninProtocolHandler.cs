// SPDX-FileCopyrightText: 2021 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
#if SYSTEM_TEXT_ENCODINGEXTENSIONS
using System.Text;
#endif
using System.Threading;
using System.Threading.Tasks;

using Smdn.Net.MuninNode.Transport;
using Smdn.Net.MuninPlugin;

#if !SYSTEM_TEXT_ENCODINGEXTENSIONS
using Smdn.Text.Encodings;
#endif

namespace Smdn.Net.MuninNode.Protocol;

/// <summary>
/// Provides the default implementation of <see cref="IMuninProtocolHandler"/>.
/// </summary>
public class MuninProtocolHandler : IMuninProtocolHandler {
  /// <summary>
  /// A simple object pool for <see cref="ArrayBufferWriter{T}"/>.
  /// </summary>
  /// <seealso href="https://learn.microsoft.com/en-us/dotnet/standard/collections/thread-safe/how-to-create-an-object-pool"/>
  private sealed class ArrayBufferWriterPool(int initialCapacity) {
    private readonly ConcurrentBag<ArrayBufferWriter<byte>> pool = new();

    public ArrayBufferWriter<byte> Take()
      => pool.TryTake(out var item) ? item : new(initialCapacity: initialCapacity);

    public void Return(ArrayBufferWriter<byte> item)
    {
      if (item is null)
        throw new ArgumentNullException(nameof(item));

#if SYSTEM_BUFFERS_ARRAYBUFFERWRITER_RESETWRITTENCOUNT
      item.ResetWrittenCount();
#else
      item.Clear();
#endif

      pool.Add(item);
    }
  }

  private readonly IMuninNodeProfile profile;
  private readonly string banner;
  private readonly string versionInformation;
  private readonly ArrayBufferWriterPool bufferWriterPool = new(initialCapacity: 256);

  public MuninProtocolHandler(
    IMuninNodeProfile profile
  )
  {
    this.profile = profile ?? throw new ArgumentNullException(nameof(profile));

    banner = $"# munin node at {profile.HostName}";
    versionInformation = $"munins node on {profile.HostName} version: {profile.Version}";
  }

  /// <inheritdoc cref="IMuninProtocolHandler.HandleTransactionStartAsync"/>
  public ValueTask HandleTransactionStartAsync(
    IMuninNodeClient client,
    CancellationToken cancellationToken = default
  )
  {
    if (client is null)
      throw new ArgumentNullException(nameof(client));

#if SYSTEM_THREADING_TASKS_VALUETASK_FROMCANCELED
    if (cancellationToken.IsCancellationRequested)
      return ValueTask.FromCanceled(cancellationToken);
#else
    cancellationToken.ThrowIfCancellationRequested();
#endif

    return HandleTransactionStartAsyncCore(client, cancellationToken);
  }

  /// <remarks>
  /// In the default implementation, a banner response is sent back to the client.
  /// </remarks>
  protected virtual ValueTask HandleTransactionStartAsyncCore(
    IMuninNodeClient client,
    CancellationToken cancellationToken
  )
    => SendResponseAsync(
      client,
      banner,
      cancellationToken
    );

  /// <inheritdoc cref="IMuninProtocolHandler.HandleTransactionEndAsync"/>
  public ValueTask HandleTransactionEndAsync(
    IMuninNodeClient client,
    CancellationToken cancellationToken = default
  )
  {
    if (client is null)
      throw new ArgumentNullException(nameof(client));

#if SYSTEM_THREADING_TASKS_VALUETASK_FROMCANCELED
    if (cancellationToken.IsCancellationRequested)
      return ValueTask.FromCanceled(cancellationToken);
#else
    cancellationToken.ThrowIfCancellationRequested();
#endif

    return HandleTransactionEndAsyncCore(client, cancellationToken);
  }

  protected virtual ValueTask HandleTransactionEndAsyncCore(
    IMuninNodeClient client,
    CancellationToken cancellationToken
  )
    => default; // do nothing in this class

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

  /// <inheritdoc cref="IMuninProtocolHandler.HandleCommandAsync"/>
  public ValueTask HandleCommandAsync(
    IMuninNodeClient client,
    ReadOnlySequence<byte> commandLine,
    CancellationToken cancellationToken = default
  )
  {
    if (client is null)
      throw new ArgumentNullException(nameof(client));

#if SYSTEM_THREADING_TASKS_VALUETASK_FROMCANCELED
    if (cancellationToken.IsCancellationRequested)
      return ValueTask.FromCanceled(cancellationToken);
#else
    cancellationToken.ThrowIfCancellationRequested();
#endif

    return HandleCommandAsyncCore(
      client: client,
      commandLine: commandLine,
      cancellationToken: cancellationToken
    );
  }

  private static readonly byte CommandQuitShort = (byte)'.';

  protected virtual ValueTask HandleCommandAsyncCore(
    IMuninNodeClient client,
    ReadOnlySequence<byte> commandLine,
    CancellationToken cancellationToken
  )
  {
    if (client is null)
      throw new ArgumentNullException(nameof(client));

    if (ExpectCommand(commandLine, "fetch"u8, out var fetchArguments)) {
      return HandleFetchCommandAsync(client, fetchArguments, cancellationToken);
    }
    else if (ExpectCommand(commandLine, "nodes"u8, out _)) {
      return HandleNodesCommandAsync(client, cancellationToken);
    }
    else if (ExpectCommand(commandLine, "list"u8, out var listArguments)) {
      return HandleListCommandAsync(client, listArguments, cancellationToken);
    }
    else if (ExpectCommand(commandLine, "config"u8, out var configArguments)) {
      return HandleConfigCommandAsync(client, configArguments, cancellationToken);
    }
    else if (
      ExpectCommand(commandLine, "quit"u8, out _) ||
      (commandLine.Length == 1 && commandLine.FirstSpan[0] == CommandQuitShort)
    ) {
      return HandleQuitCommandAsync(client, cancellationToken);
    }
    else if (ExpectCommand(commandLine, "cap"u8, out var capArguments)) {
      return HandleCapCommandAsync(client, capArguments, cancellationToken);
    }
    else if (ExpectCommand(commandLine, "version"u8, out _)) {
      return HandleVersionCommandAsync(client, cancellationToken);
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

  protected async ValueTask SendResponseAsync(
    IMuninNodeClient client,
    IEnumerable<string> responseLines,
    CancellationToken cancellationToken
  )
  {
    if (client is null)
      throw new ArgumentNullException(nameof(client));
    if (responseLines is null)
      throw new ArgumentNullException(nameof(responseLines));

    cancellationToken.ThrowIfCancellationRequested();

    var writer = bufferWriterPool.Take();

    try {
      foreach (var responseLine in responseLines) {
#if SYSTEM_TEXT_ENCODINGEXTENSIONS
        _ = profile.Encoding.GetBytes(responseLine, writer);

        writer.Write(EndOfLine.Span);
#else
        var totalByteCount = profile.Encoding.GetByteCount(responseLine) + EndOfLine.Length;
        var buffer = writer.GetMemory(totalByteCount);
        var bytesWritten = profile.Encoding.GetBytes(responseLine, buffer.Span);

        EndOfLine.CopyTo(buffer[bytesWritten..]);

        bytesWritten += EndOfLine.Length;

        writer.Advance(bytesWritten);
#endif
      }

      await client.SendAsync(
        buffer: writer.WrittenMemory,
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);
    }
    finally {
      bufferWriterPool.Return(writer);
    }
  }

  /// <summary>
  /// Handles the <c>quit</c> command.
  /// </summary>
  /// <remarks>
  /// This implementation calls <see cref="IMuninNodeClient.DisconnectAsync"/> to disconnect the connection for the current transaction.
  /// </remarks>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/master/network-protocol.html">
  /// Data exchange between master and node - `quit` command
  /// </seealso>
  protected virtual ValueTask HandleQuitCommandAsync(
    IMuninNodeClient client,
    CancellationToken cancellationToken
  )
    => (client ?? throw new ArgumentNullException(nameof(client))).DisconnectAsync(cancellationToken);

  /// <summary>
  /// Handles the <c>nodes</c> command and sends back a response.
  /// </summary>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/master/network-protocol.html">
  /// Data exchange between master and node - `nodes` command
  /// </seealso>
  protected virtual ValueTask HandleNodesCommandAsync(
    IMuninNodeClient client,
    CancellationToken cancellationToken
  )
  {
    return SendResponseAsync(
      client: client ?? throw new ArgumentNullException(nameof(client)),
      responseLines: [
        profile.HostName,
        ".",
      ],
      cancellationToken: cancellationToken
    );
  }

  /// <summary>
  /// Handles the <c>version</c> command and sends back a response.
  /// </summary>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/master/network-protocol.html">
  /// Data exchange between master and node - `version` command
  /// </seealso>
  protected virtual ValueTask HandleVersionCommandAsync(
    IMuninNodeClient client,
    CancellationToken cancellationToken
  )
  {
    return SendResponseAsync(
      client: client ?? throw new ArgumentNullException(nameof(client)),
      responseLine: versionInformation,
      cancellationToken: cancellationToken
    );
  }

  /// <summary>
  /// Handles the <c>cap</c> command and sends back a response.
  /// </summary>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/master/network-protocol.html">
  /// Data exchange between master and node - `cap` command
  /// </seealso>
  protected virtual ValueTask HandleCapCommandAsync(
    IMuninNodeClient client,
    ReadOnlySequence<byte> arguments,
    CancellationToken cancellationToken
  )
  {
    // TODO: multigraph (https://guide.munin-monitoring.org/en/latest/plugin/protocol-multigraph.html)
    // TODO: dirtyconfig (https://guide.munin-monitoring.org/en/latest/plugin/protocol-dirtyconfig.html)
    // XXX: ignores capability arguments
    return SendResponseAsync(
      client: client ?? throw new ArgumentNullException(nameof(client)),
      responseLine: "cap",
      cancellationToken: cancellationToken
    );
  }

  /// <summary>
  /// Handles the <c>list</c> command and sends back a response.
  /// </summary>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/master/network-protocol.html">
  /// Data exchange between master and node - `list` command
  /// </seealso>
  protected virtual ValueTask HandleListCommandAsync(
    IMuninNodeClient client,
    ReadOnlySequence<byte> arguments,
    CancellationToken cancellationToken
  )
  {
    // XXX: ignore [node] arguments
    return SendResponseAsync(
      client: client ?? throw new ArgumentNullException(nameof(client)),
      responseLine: string.Join(" ", profile.PluginProvider.Plugins.Select(static plugin => plugin.Name)),
      cancellationToken: cancellationToken
    );
  }

  /// <summary>
  /// Handles the <c>fetch</c> command and sends back a response.
  /// </summary>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/master/network-protocol.html">
  /// Data exchange between master and node - `fetch` command
  /// </seealso>
  protected virtual async ValueTask HandleFetchCommandAsync(
    IMuninNodeClient client,
    ReadOnlySequence<byte> arguments,
    CancellationToken cancellationToken
  )
  {
    if (client is null)
      throw new ArgumentNullException(nameof(client));

    var queryItem = profile.Encoding.GetString(arguments);
    var plugin = profile.PluginProvider.Plugins.FirstOrDefault(
      plugin => string.Equals(queryItem, plugin.Name, StringComparison.Ordinal)
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

  /// <summary>
  /// Handles the <c>config</c> command and sends back a response.
  /// </summary>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/master/network-protocol.html">
  /// Data exchange between master and node - `config` command
  /// </seealso>
  protected virtual ValueTask HandleConfigCommandAsync(
    IMuninNodeClient client,
    ReadOnlySequence<byte> arguments,
    CancellationToken cancellationToken
  )
  {
    if (client is null)
      throw new ArgumentNullException(nameof(client));

    var queryItem = profile.Encoding.GetString(arguments);
    var plugin = profile.PluginProvider.Plugins.FirstOrDefault(
      plugin => string.Equals(queryItem, plugin.Name, StringComparison.Ordinal)
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
