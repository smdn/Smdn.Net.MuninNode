// SPDX-FileCopyrightText: 2021 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
  /// A simple object pool.
  /// </summary>
  /// <seealso href="https://learn.microsoft.com/en-us/dotnet/standard/collections/thread-safe/how-to-create-an-object-pool"/>
  private class ObjectPool<T>(Func<T> create, Action<T> clear) {
    private readonly ConcurrentBag<T> pool = new();

    public T Take()
      => pool.TryTake(out var item) ? item : create();

    public void Return(T item)
    {
      if (item is null)
        throw new ArgumentNullException(nameof(item));

      pool.Add(item);

      clear(item);
    }
  }

  private sealed class ArrayBufferWriterPool(int initialCapacity)
    : ObjectPool<ArrayBufferWriter<byte>>(
      create: () => new ArrayBufferWriter<byte>(initialCapacity: initialCapacity),
      clear: item
#if SYSTEM_BUFFERS_ARRAYBUFFERWRITER_RESETWRITTENCOUNT
        => item.ResetWrittenCount()
#else
        => item.Clear()
#endif
    ) {
  }

  private sealed class StringBuilderPool(int initialCapacity)
    : ObjectPool<StringBuilder>(
      create: () => new StringBuilder(capacity: initialCapacity),
      clear: item => item.Clear()
    ) {
  }

  private readonly IMuninNodeProfile profile;
  private readonly string banner;
  private readonly string versionInformation;
  private readonly ArrayBufferWriterPool bufferWriterPool = new(initialCapacity: 256);
  private readonly StringBuilderPool responseBuilderPool = new(initialCapacity: 512);
  private readonly Dictionary<string, IPlugin> plugins = new(StringComparer.Ordinal);

  /// <summary>
  /// Gets a value indicating whether the <c>munin master</c> supports
  /// <c>dirtyconfig</c> protocol extension and enables it.
  /// </summary>
  /// <seealso cref="HandleCapCommandAsync"/>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/plugin/protocol-dirtyconfig.html">
  /// Protocol extension: dirtyconfig
  /// </seealso>
  protected bool IsDirtyConfigEnabled { get; private set; }

  /// <summary>
  /// Gets a value indicating whether the <c>munin master</c> supports
  /// <c>multigraph</c> protocol extension and enables it.
  /// </summary>
  /// <seealso cref="HandleCapCommandAsync"/>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/plugin/protocol-multigraph.html">
  /// Protocol extension: multiple graphs from one plugin
  /// </seealso>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/plugin/multigraphing.html">
  /// Multigraph plugins
  /// </seealso>
  protected bool IsMultigraphEnabled { get; private set; }

  private string joinedPluginList = string.Empty;

  public MuninProtocolHandler(
    IMuninNodeProfile profile
  )
  {
    this.profile = profile ?? throw new ArgumentNullException(nameof(profile));

    banner = $"# munin node at {profile.HostName}";
    versionInformation = $"munins node on {profile.HostName} version: {profile.Version}";

    ReinitializePluginDictionary();
  }

  private void ReinitializePluginDictionary()
  {
    var flattenMultigraphPlugins = !IsMultigraphEnabled;

    plugins.Clear();

    foreach (var plugin in profile.PluginProvider.EnumeratePlugins(flattenMultigraphPlugins)) {
      plugins[plugin.Name] = plugin; // duplicate plugin names are not considered
    }

    joinedPluginList = string.Join(
      ' ',
      profile.PluginProvider.EnumeratePlugins(flattenMultigraphPlugins).Select(static plugin => plugin.Name)
    );
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

  private static bool SequenceContains(
    ReadOnlySequence<byte> sequence,
    ReadOnlySpan<byte> value
  )
  {
    var reader = new SequenceReader<byte>(sequence);

    const byte SP = (byte)' ';

    // read the given sequence by dividing the SP as a delimiter
    for (; ; ) {
      if (!reader.TryReadTo(out ReadOnlySequence<byte> segment, delimiter: SP, advancePastDelimiter: false))
        return reader.IsNext(value, advancePast: true);

      var segmentReader = new SequenceReader<byte>(segment);

      if (segmentReader.IsNext(value, advancePast: false))
        return true;

      reader.Advance(1); // SP
    }
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

  protected ValueTask SendResponseAsync(
    IMuninNodeClient client,
    IEnumerable<string> responseLines,
    CancellationToken cancellationToken
  )
  {
    if (client is null)
      throw new ArgumentNullException(nameof(client));
    if (responseLines is null)
      throw new ArgumentNullException(nameof(responseLines));

    var builder = responseBuilderPool.Take();

    try {
      foreach (var line in responseLines) {
        builder.Append(line).Append('\n');
      }

      return SendResponseAsync(client, builder, cancellationToken);
    }
    finally {
      responseBuilderPool.Return(builder);
    }
  }

  private async ValueTask SendResponseAsync(
    IMuninNodeClient client,
    StringBuilder responseBuilder,
    CancellationToken cancellationToken
  )
  {
    if (client is null)
      throw new ArgumentNullException(nameof(client));
    if (responseBuilder is null)
      throw new ArgumentNullException(nameof(responseBuilder));

    cancellationToken.ThrowIfCancellationRequested();

    var writer = bufferWriterPool.Take();

    try {
#if SYSTEM_TEXT_STRINGBUILDER_GETCHUNKS
      foreach (var chunk in responseBuilder.GetChunks()) {
#if SYSTEM_TEXT_ENCODINGEXTENSIONS
        _ = profile.Encoding.GetBytes(chunk.Span, writer);
#else
        var byteCount = profile.Encoding.GetByteCount(chunk);
        var buffer = writer.GetMemory(byteCount);
        var bytesWritten = profile.Encoding.GetBytes(chunk, buffer.Span);

        writer.Advance(bytesWritten);
#endif
      }
#else
      var responseChars = ArrayPool<char>.Shared.Rent(responseBuilder.Length);

      try {
        responseBuilder.CopyTo(0, responseChars, 0, responseBuilder.Length);

        var responseCharsMemory = responseChars.AsMemory(0, responseBuilder.Length);
        var byteCount = profile.Encoding.GetByteCount(responseCharsMemory.Span);
        var buffer = writer.GetMemory(byteCount);
        var bytesWritten = profile.Encoding.GetBytes(responseCharsMemory.Span, buffer.Span);

        writer.Advance(bytesWritten);
      }
      finally {
        ArrayPool<char>.Shared.Return(responseChars);
      }
#endif

      await client.SendAsync(
        buffer: writer.WrittenMemory,
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);
    }
    finally {
      bufferWriterPool.Return(writer);
    }
  }

  private static readonly ReadOnlyMemory<byte> UnknownServiceResponse = "# Unknown service\n.\n"u8.ToArray();

  private static ValueTask SendUnknownServiceResponseAsync(
    IMuninNodeClient client,
    CancellationToken cancellationToken
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

    return client.SendAsync(
      UnknownServiceResponse,
      cancellationToken
    );
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
  /// <remarks>
  /// This implementation does not set the value of the <c>MUNIN_CAP_DIRTYCONFIG</c> environment variable,
  /// even when <c>dirtyconfig</c> is enabled.
  /// </remarks>
  /// <seealso cref="IsDirtyConfigEnabled"/>
  /// <seealso cref="IsMultigraphEnabled"/>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/master/network-protocol.html">
  /// Data exchange between master and node - `cap` command
  /// </seealso>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/plugin/protocol-dirtyconfig.html">
  /// Protocol extension: dirtyconfig
  /// </seealso>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/plugin/protocol-multigraph.html">
  /// Protocol extension: multiple graphs from one plugin
  /// </seealso>
  protected virtual ValueTask HandleCapCommandAsync(
    IMuninNodeClient client,
    ReadOnlySequence<byte> arguments,
    CancellationToken cancellationToken
  )
  {
    var wasMultigraphEnabled = IsMultigraphEnabled;

    // 'Protocol extension: dirtyconfig' (https://guide.munin-monitoring.org/en/latest/plugin/protocol-dirtyconfig.html)
    IsDirtyConfigEnabled = SequenceContains(arguments, "dirtyconfig"u8);

    // 'Protocol extension: multiple graphs from one plugin' (https://guide.munin-monitoring.org/en/latest/plugin/protocol-multigraph.html)
    IsMultigraphEnabled = SequenceContains(arguments, "multigraph"u8);

    if (IsMultigraphEnabled != wasMultigraphEnabled)
      ReinitializePluginDictionary();

    return SendResponseAsync(
      client: client ?? throw new ArgumentNullException(nameof(client)),
      responseLine: GetCapResponseLine(IsDirtyConfigEnabled, IsMultigraphEnabled),
      cancellationToken: cancellationToken
    );

    static string GetCapResponseLine(bool dirtyconfig, bool multigraph)
    {
      if (dirtyconfig && multigraph)
        return "cap dirtyconfig multigraph";

      if (dirtyconfig)
        return "cap dirtyconfig";

      if (multigraph)
        return "cap multigraph";

      return "cap";
    }
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
      responseLine: joinedPluginList,
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

    if (!plugins.TryGetValue(queryItem, out var plugin)) {
      await SendUnknownServiceResponseAsync(
        client,
        cancellationToken
      ).ConfigureAwait(false);

      return;
    }

    var responseBuilder = responseBuilderPool.Take();

    try {
      if (plugin is IMultigraphPlugin multigraphPlugin) {
        // 'Protocol extension: multiple graphs from one plugin' (https://guide.munin-monitoring.org/en/latest/plugin/protocol-multigraph.html)
        foreach (var subPlugin in multigraphPlugin.Plugins) {
          responseBuilder.Append(provider: null, $"multigraph {subPlugin.Name}\n");

          await WriteFetchResponseAsync(
            subPlugin.DataSource,
            responseBuilder,
            cancellationToken
          ).ConfigureAwait(false);
        }
      }
      else {
        await WriteFetchResponseAsync(
          plugin.DataSource,
          responseBuilder,
          cancellationToken
        ).ConfigureAwait(false);
      }

      responseBuilder.Append(".\n");

      await SendResponseAsync(
        client: client,
        responseBuilder: responseBuilder,
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);
    }
    finally {
      responseBuilderPool.Return(responseBuilder);
    }
  }

  private static async ValueTask WriteFetchResponseAsync(
    IPluginDataSource dataSource,
    StringBuilder responseBuilder,
    CancellationToken cancellationToken
  )
  {
    foreach (var field in dataSource.Fields) {
      var valueString = await field.GetFormattedValueStringAsync(
        cancellationToken: cancellationToken
      ).ConfigureAwait(false);

      responseBuilder.Append(provider: null, $"{field.Name}.value {valueString}\n");
    }
  }

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

    if (!plugins.TryGetValue(queryItem, out var plugin)) {
      return SendUnknownServiceResponseAsync(
        client,
        cancellationToken
      );
    }

    return HandleConfigCommandAsyncCore();

    async ValueTask HandleConfigCommandAsyncCore()
    {
      var responseBuilder = responseBuilderPool.Take();

      try {
        if (plugin is IMultigraphPlugin multigraphPlugin) {
          // 'Protocol extension: multiple graphs from one plugin' (https://guide.munin-monitoring.org/en/latest/plugin/protocol-multigraph.html)
          foreach (var subPlugin in multigraphPlugin.Plugins) {
            responseBuilder.Append(provider: null, $"multigraph {subPlugin.Name}\n");

            await WriteConfigResponseAsync(
              subPlugin,
              includeFetchResponse: IsDirtyConfigEnabled,
              responseBuilder,
              cancellationToken
            ).ConfigureAwait(false);
          }
        }
        else {
          await WriteConfigResponseAsync(
            plugin,
            includeFetchResponse: IsDirtyConfigEnabled,
            responseBuilder,
            cancellationToken
          ).ConfigureAwait(false);
        }

        responseBuilder.Append(".\n");

        await SendResponseAsync(
          client: client,
          responseBuilder: responseBuilder,
          cancellationToken: cancellationToken
        ).ConfigureAwait(false);
      }
      finally {
        responseBuilderPool.Return(responseBuilder);
      }
    }

    static ValueTask WriteConfigResponseAsync(
      IPlugin plugin,
      bool includeFetchResponse,
      StringBuilder responseBuilder,
      CancellationToken cancellationToken
    )
    {
      WriteConfigResponse(
        plugin,
        responseBuilder
      );

      if (includeFetchResponse) {
        return WriteFetchResponseAsync(
          dataSource: plugin.DataSource,
          responseBuilder: responseBuilder,
          cancellationToken: cancellationToken
        );
      }

      return default;
    }
  }

  private static void WriteConfigResponse(
    IPlugin plugin,
    StringBuilder responseBuilder
  )
  {
    /*
     * global attributes
     */
    foreach (var graphAttribute in plugin.GraphAttributes.EnumerateAttributes()) {
      responseBuilder.Append(graphAttribute).Append('\n');
    }

    /*
     * data source attributes
     */
    WriteConfigDataSourceAttributes(
      plugin.DataSource,
      responseBuilder
    );
  }

  private static void WriteConfigDataSourceAttributes(
    IPluginDataSource dataSource,
    StringBuilder responseBuilder
  )
  {
    var shouldHandleNegativeFields = dataSource
      .Fields
      .Any(static f => !string.IsNullOrEmpty(f.Attributes.NegativeFieldName));

    // The fields referenced by {fieldname}.negative must be defined ahread of others,
    // and thus lists the negative field settings first.
    // Otherwise, the following error occurs when generating the graph.
    // "[RRD ERROR] Unable to graph /var/cache/munin/www/XXX.png : undefined v name XXXXXXXXXXXXXX"
    IEnumerable<IPluginField> orderedFields = shouldHandleNegativeFields
      ? dataSource.Fields.OrderBy(f => IsNegativeField(f, dataSource.Fields) ? 0 : 1)
      : dataSource.Fields;

    foreach (var field in orderedFields) {
      var fieldAttrs = field.Attributes;
      bool? graph = null;

      responseBuilder.Append(provider: null, $"{field.Name}.label {fieldAttrs.Label}\n");

      if (TranslateFieldDrawAttribute(fieldAttrs.GraphStyle) is string attrDraw)
        responseBuilder.Append(provider: null, $"{field.Name}.draw {attrDraw}\n");

      if (FormatNormalValueRange(fieldAttrs.NormalRangeForWarning) is string attrWarning)
        responseBuilder.Append(provider: null, $"{field.Name}.warning {attrWarning}\n");

      if (FormatNormalValueRange(fieldAttrs.NormalRangeForCritical) is string attrCritical)
        responseBuilder.Append(provider: null, $"{field.Name}.critical {attrCritical}\n");

      if (shouldHandleNegativeFields && !string.IsNullOrEmpty(fieldAttrs.NegativeFieldName)) {
        var negativeField = dataSource.Fields.FirstOrDefault(
          f => string.Equals(fieldAttrs.NegativeFieldName, f.Name, StringComparison.Ordinal)
        );

        if (negativeField is not null)
          responseBuilder.Append(provider: null, $"{field.Name}.negative {negativeField.Name}\n");
      }

      // this field is defined as the negative field of other field, so should not be graphed
      if (shouldHandleNegativeFields && IsNegativeField(field, dataSource.Fields))
        graph = false;

      if (graph is bool drawGraph)
        responseBuilder.Append(provider: null, $"{field.Name}.graph {(drawGraph ? "yes" : "no")}\n");
    }

    static bool IsNegativeField(IPluginField field, IReadOnlyCollection<IPluginField> fields)
      => fields.Any(
        f => string.Equals(field.Name, f.Attributes.NegativeFieldName, StringComparison.Ordinal)
      );

    static string? FormatNormalValueRange(PluginFieldNormalValueRange range)
    {
      if (range.Min.HasValue && range.Max.HasValue)
        return $"{range.Min.Value}:{range.Max.Value}";
      else if (range.Min.HasValue)
        return $"{range.Min.Value}:";
      else if (range.Max.HasValue)
        return $":{range.Max.Value}";
      else
        return null;
    }

    static string? TranslateFieldDrawAttribute(PluginFieldGraphStyle style)
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
  }
}
