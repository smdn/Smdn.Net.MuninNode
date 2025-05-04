// SPDX-FileCopyrightText: 2021 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Smdn.Net.MuninPlugin;
#if !SYSTEM_TEXT_ENCODINGEXTENSIONS
using Smdn.Text.Encodings;
#endif

using Smdn.Net.MuninNode.Transport;

namespace Smdn.Net.MuninNode;

#pragma warning disable IDE0040
partial class NodeBase {
#pragma warning restore IDE0040
  private readonly ArrayBufferWriter<byte> responseBuffer = new(initialCapacity: 1024); // TODO: define best initial capacity

  private ValueTask SendBannerResponseAsync(
    IMuninNodeClient client,
    CancellationToken cancellationToken
  )
  {
#if SYSTEM_THREADING_TASKS_VALUETASK_FROMCANCELED
    if (cancellationToken.IsCancellationRequested)
      return ValueTask.FromCanceled(cancellationToken);
#else
    cancellationToken.ThrowIfCancellationRequested();
#endif

    return SendResponseAsync(
      client,
      $"# munin node at {HostName}",
      cancellationToken
    );
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

    var queryItem = Encoding.GetString(arguments);
    var plugin = PluginProvider.Plugins.FirstOrDefault(
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

  private ValueTask ProcessCommandConfigAsync(
    IMuninNodeClient client,
    ReadOnlySequence<byte> arguments,
    CancellationToken cancellationToken
  )
  {
    ThrowIfPluginProviderIsNull();

    var queryItem = Encoding.GetString(arguments);
    var plugin = PluginProvider.Plugins.FirstOrDefault(
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
