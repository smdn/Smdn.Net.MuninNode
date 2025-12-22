// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLATTRIBUTE
using System.Diagnostics.CodeAnalysis;
#endif
using System.Text.RegularExpressions;

namespace Smdn.Net.MuninPlugin;

/// <summary>
/// Provides a builder pattern for constructing <see cref="IPluginGraphAttributes"/> that represent
/// attributes for drawing a single graph, defined as <seealso href="https://guide.munin-monitoring.org/en/latest/reference/plugin.html#global-attributes">global attributes</seealso>.
/// </summary>
/// <seealso cref="IPluginGraphAttributes"/>
/// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/plugin.html#global-attributes">Plugin reference - Global attributes</seealso>
/// <seealso href="https://guide.munin-monitoring.org/en/latest/example/graph/graph_args.html">Recommended graph_args</seealso>
public partial class PluginGraphAttributesBuilder {
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/plugin.html#graph-title">Plugin reference - Global attributes - graph_title</seealso>
  private const string RegexTitlePattern = @"^\P{C}+\z"; // except Unicode category 'C' (all other characters)

#if NET9_0_OR_GREATER
  [GeneratedRegex(
    pattern: RegexTitlePattern,
    options: RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant
  )]
  public static partial Regex RegexTitle { get; }
#elif SYSTEM_TEXT_REGULAREXPRESSIONS_GENERATEDREGEXATTRIBUTE
  public static Regex RegexTitle => GetRegexTitle();

  [GeneratedRegex(
    pattern: RegexTitlePattern,
    options: RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant
  )]
  private static partial Regex GetRegexTitle();
#else
  public static Regex RegexTitle { get; } = new(
    pattern: RegexTitlePattern,
    options: RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant
  );
#endif

  private static void ThrowIfNotMatch(Regex regex, string input, string paramName, string attrName)
  {
    if (!regex.IsMatch(input)) {
      throw new ArgumentException(
        message: $"'{input}' is invalid for {attrName}. The value of {paramName} must match the following regular expression: '{regex}'",
        paramName: paramName
      );
    }
  }

  private string title;
  private bool? showGraph;
  private string? category;
  private int? height;
  private string? order;
  private string? printf;
  private bool? scale;
  private string? labelForTotal;
  private string? verticalLabel;
  private int? width;
  private TimeSpan? updateRate;
  private string? graphDataSize;

  /// <summary>
  /// Initializes a new instance of the <see cref="PluginGraphAttributesBuilder"/> class by
  /// copying the configurations on an existing <see cref="PluginGraphAttributesBuilder"/>.
  /// </summary>
  /// <param name="title">
  /// The <see cref="string"/> value for the <c>graph_title</c>.
  /// </param>
  /// <param name="baseBuilder">
  /// The <see cref="PluginGraphAttributesBuilder"/> instance from which values other than <c>graph_title</c> attribute are copied.
  /// </param>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="title"/> is <see langword="null"/>, or
  /// <paramref name="baseBuilder"/> is <see langword="null"/>.
  /// </exception>
  /// <exception cref="ArgumentException">
  /// <paramref name="title"/> is empty, or
  /// <paramref name="title"/> contains invalid characters.
  /// </exception>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/plugin.html#graph-title">Plugin reference - Global attributes - graph_title</seealso>
  public PluginGraphAttributesBuilder(string title, PluginGraphAttributesBuilder baseBuilder)
    : this(title)
  {
    if (baseBuilder is null)
      throw new ArgumentNullException(nameof(baseBuilder));

    this.showGraph = baseBuilder.showGraph;
    this.category = baseBuilder.category;
    this.height = baseBuilder.height;
    this.order = baseBuilder.order;
    this.printf = baseBuilder.printf;
    this.scale = baseBuilder.scale;
    this.labelForTotal = baseBuilder.labelForTotal;
    this.verticalLabel = baseBuilder.verticalLabel;
    this.width = baseBuilder.width;
    this.updateRate = baseBuilder.updateRate;

    this.graphArgs = [.. baseBuilder.graphArgs];
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="PluginGraphAttributesBuilder"/> class with the setting value for the <c>graph_title</c> attribute.
  /// </summary>
  /// <param name="title">The <see cref="string"/> value for the <c>graph_title</c> attribute.</param>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="title"/> is <see langword="null"/>.
  /// </exception>
  /// <exception cref="ArgumentException">
  /// <paramref name="title"/> is empty, or
  /// <paramref name="title"/> contains invalid characters.
  /// </exception>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/plugin.html#graph-title">Plugin reference - Global attributes - graph_title</seealso>
#if !SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLATTRIBUTE
#pragma warning disable CS8618
#endif
  public PluginGraphAttributesBuilder(string title)
  {
    WithTitle(title);
  }
#pragma warning restore CS8618

  /// <summary>Sets a value for the <c>graph</c> to <c>yes</c>.</summary>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/plugin.html#graph">Plugin reference - Global attributes - graph</seealso>
  public PluginGraphAttributesBuilder ShowGraph()
  {
    showGraph = true;

    return this;
  }

  /// <summary>Sets a value for the <c>graph</c> to <c>no</c>.</summary>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/plugin.html#graph">Plugin reference - Global attributes - graph</seealso>
  public PluginGraphAttributesBuilder HideGraph()
  {
    showGraph = false;

    return this;
  }

  /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/plugin.html#graph-category">Plugin reference - Global attributes - graph_category</seealso>
  private const string RegexCategoryPattern = @"^[a-z0-9-.]+\z";

#if NET9_0_OR_GREATER
  [GeneratedRegex(
    pattern: RegexCategoryPattern,
    options: RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant
  )]
  public static partial Regex RegexCategory { get; }
#elif SYSTEM_TEXT_REGULAREXPRESSIONS_GENERATEDREGEXATTRIBUTE
  public static Regex RegexCategory => GetRegexCategory();

  [GeneratedRegex(
    pattern: RegexCategoryPattern,
    options: RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant
  )]
  private static partial Regex GetRegexCategory();
#else
  public static Regex RegexCategory { get; } = new(
    pattern: RegexCategoryPattern,
    options: RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant
  );
#endif

  /// <summary>Sets a value for the <c>graph_category</c>.</summary>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/plugin.html#graph-category">Plugin reference - Global attributes - graph_category</seealso>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/graph-category.html">Plugin graph categories</seealso>
  /// <exception cref="ArgumentNullException"><paramref name="category"/> is <see langword="null"/>.</exception>
  /// <exception cref="ArgumentException"><paramref name="category"/> is empty.</exception>
  public PluginGraphAttributesBuilder WithCategory(string category)
  {
    ArgumentExceptionShim.ThrowIfNullOrWhiteSpace(category, nameof(category));

    ThrowIfNotMatch(RegexCategory, category, nameof(category), "graph_category");

    this.category = category;

    return this;
  }

  /// <summary>Sets a value for the <c>graph_height</c>.</summary>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/plugin.html#graph-height">Plugin reference - Global attributes - graph_height</seealso>
  /// <exception cref="ArgumentOutOfRangeException"><paramref name="height"/> is less than or equal to <c>0</c>.</exception>
  public PluginGraphAttributesBuilder WithHeight(int height)
  {
    ArgumentOutOfRangeExceptionShim.ThrowIfLessThanOrEqual(height, 0, nameof(height));

    this.height = height;

    return this;
  }

  /*
   * TODO: graph_info
   */

  /// <summary>Sets a value for the <c>graph_order</c>.</summary>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/plugin.html#graph-order">Plugin reference - Global attributes - graph_order</seealso>
  /// <exception cref="ArgumentNullException"><paramref name="order"/> is <see langword="null"/>.</exception>
  public PluginGraphAttributesBuilder WithFieldOrder(/*params*/ IEnumerable<string> order)
  {
    if (order is null)
      throw new ArgumentNullException(nameof(order));

    this.order = string.Join(' ', order);

    return this;
  }

  /*
   * TODO: graph_period
   */

  /// <summary>Sets a value for the <c>graph_printf</c>.</summary>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/plugin.html#graph-printf">Plugin reference - Global attributes - graph_printf</seealso>
  /// <exception cref="ArgumentNullException"><paramref name="printf"/> is <see langword="null"/>.</exception>
  /// <exception cref="ArgumentException"><paramref name="printf"/> is empty.</exception>
  public PluginGraphAttributesBuilder WithFormatString(string printf)
  {
    ArgumentExceptionShim.ThrowIfNullOrWhiteSpace(printf, nameof(printf));

    this.printf = printf;

    return this;
  }

  /// <summary>Sets a value for the <c>graph_scale</c> to <c>yes</c>.</summary>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/plugin.html#graph-scale">Plugin reference - Global attributes - graph_scale</seealso>
  public PluginGraphAttributesBuilder EnableUnitScaling()
  {
    this.scale = true;

    return this;
  }

  /// <summary>Sets a value for the <c>graph_scale</c> to <c>no</c>.</summary>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/plugin.html#graph-scale">Plugin reference - Global attributes - graph_scale</seealso>
  public PluginGraphAttributesBuilder DisableUnitScaling()
  {
    this.scale = false;

    return this;
  }

  /// <summary>Overwrites a value for the <c>graph_title</c>.</summary>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/plugin.html#graph-title">Plugin reference - Global attributes - graph_title</seealso>
  /// <exception cref="ArgumentNullException"><paramref name="title"/> is <see langword="null"/>.</exception>
  /// <exception cref="ArgumentException">
  /// <paramref name="title"/> is empty, or
  /// <paramref name="title"/> contains invalid characters.
  /// </exception>
#if SYSTEM_DIAGNOSTICS_CODEANALYSIS_MEMBERNOTNULLATTRIBUTE
  [MemberNotNull(nameof(title))]
#endif
  public PluginGraphAttributesBuilder WithTitle(string title)
  {
    ArgumentExceptionShim.ThrowIfNullOrWhiteSpace(title, nameof(title));

    ThrowIfNotMatch(RegexTitle, title, nameof(title), "graph_title");

    this.title = title;

    return this;
  }

  /// <summary>Sets a value for the <c>graph_total</c>.</summary>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/plugin.html#graph-total">Plugin reference - Global attributes - graph_total</seealso>
  /// <exception cref="ArgumentNullException"><paramref name="labelForTotal"/> is <see langword="null"/>.</exception>
  /// <exception cref="ArgumentException"><paramref name="labelForTotal"/> is empty.</exception>
  public PluginGraphAttributesBuilder WithTotal(string labelForTotal)
  {
    ArgumentExceptionShim.ThrowIfNullOrWhiteSpace(labelForTotal, nameof(labelForTotal));

    this.labelForTotal = labelForTotal;

    return this;
  }

  /// <summary>Sets a value for the <c>graph_vlabel</c>.</summary>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/plugin.html#graph-vlabel">Plugin reference - Global attributes - graph_vlabel</seealso>
  /// <exception cref="ArgumentNullException"><paramref name="verticalLabel"/> is <see langword="null"/>.</exception>
  /// <exception cref="ArgumentException"><paramref name="verticalLabel"/> is empty.</exception>
  public PluginGraphAttributesBuilder WithVerticalLabel(string verticalLabel)
  {
    ArgumentExceptionShim.ThrowIfNullOrWhiteSpace(verticalLabel, nameof(verticalLabel));

    this.verticalLabel = verticalLabel;

    return this;
  }

  /// <summary>Sets a value for the <c>graph_width</c>.</summary>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/plugin.html#graph-width">Plugin reference - Global attributes - graph_width</seealso>
  /// <exception cref="ArgumentOutOfRangeException"><paramref name="width"/> is less than or equal to <c>0</c>.</exception>
  public PluginGraphAttributesBuilder WithWidth(int width)
  {
    ArgumentOutOfRangeExceptionShim.ThrowIfLessThanOrEqual(width, 0, nameof(width));

    this.width = width;

    return this;
  }

  private static readonly TimeSpan UpdateRateMinValue = TimeSpan.FromSeconds(1.0);

  /// <summary>Sets a value for the <c>update_rate</c>.</summary>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/plugin.html#update-rate">Plugin reference - Global attributes - update_rate</seealso>
  /// <exception cref="ArgumentOutOfRangeException"><paramref name="updateRate"/> is less than <c>1</c> seconds.</exception>
  public PluginGraphAttributesBuilder WithUpdateRate(TimeSpan updateRate)
  {
    ArgumentOutOfRangeExceptionShim.ThrowIfLessThan(updateRate, UpdateRateMinValue, nameof(updateRate));

    this.updateRate = updateRate;

    return this;
  }

  /// <summary>Sets a value for the <c>graph_data_size</c>.</summary>
  /// <exception cref="ArgumentException">
  /// <paramref name="graphDataSize"/> is empty, or,
  /// <paramref name="graphDataSize"/> is a value other than <c>normal</c>, <c>huge</c>, or <c>custom &lt;computer-readable|human-readable&gt;</c>.
  /// </exception>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/munin.conf.html#cmdoption-munin-conf-arg-graph-data-size">
  /// munin.conf - GLOBAL DIRECTIVES - graph_data_size &lt;normal|huge|custom&gt;
  /// </seealso>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/advanced/custom-rrd-sizing.html">
  /// Per plugin custom rrd sizing
  /// </seealso>
  public PluginGraphAttributesBuilder WithGraphDataSize(string graphDataSize)
  {
    ArgumentExceptionShim.ThrowIfNullOrWhiteSpace(graphDataSize, nameof(graphDataSize));

    this.graphDataSize = graphDataSize.StartsWith("custom ", StringComparison.Ordinal)
      ? graphDataSize // custom
      : graphDataSize switch {
        "normal" => graphDataSize,
        "huge" => graphDataSize,
        _ => throw new ArgumentException(message: $"The value '{graphDataSize}' is invalid for 'graph_data_size'", paramName: nameof(graphDataSize)),
      };

    return this;
  }

  /*
   * methods for composite configurations or delegating
   */

  /// <summary>Sets a value for the <c>graph_category</c> to <c>other</c>.</summary>
  /// <seealso cref="WithCategory(string)"/>
  /// <seealso cref="WithCategory(WellKnownCategory)"/>
  public PluginGraphAttributesBuilder WithCategoryOther()
    => WithCategory(WellKnownCategory.Other);

  /// <summary>Sets a value for the <c>graph_category</c>.</summary>
  /// <seealso cref="WithCategory(string)"/>
  public PluginGraphAttributesBuilder WithCategory(WellKnownCategory category)
    => WithCategory(TranslateWellKnownCategory(category));

#pragma warning disable SA1124 // Do not use #regions
  #region cSpell:disable
  private static string TranslateWellKnownCategory(WellKnownCategory category)
    => category switch {
      WellKnownCategory.OneSec => "1sec",
      WellKnownCategory.AntiVirus => "antivirus",
      WellKnownCategory.ApplicationServer => "appserver",
      WellKnownCategory.AuthenticationServer => "auth",
      WellKnownCategory.Backup => "backup",
      WellKnownCategory.MessagingServer => "chat",
      WellKnownCategory.Cloud => "cloud",
      WellKnownCategory.ContentManagementSystem => "cms",
      WellKnownCategory.Cpu => "cpu",
      WellKnownCategory.DatabaseServer => "db",
      WellKnownCategory.DevelopmentTool => "devel",
      WellKnownCategory.Disk => "disk",
      WellKnownCategory.Dns => "dns",
      WellKnownCategory.FileTransfer => "filetransfer",
      WellKnownCategory.Forum => "forum",
      WellKnownCategory.FileSystem => "fs",
      WellKnownCategory.NetworkFiltering => "fw",
      WellKnownCategory.GameServer => "games",
      WellKnownCategory.HighThroughputComputing => "htc",
      WellKnownCategory.LoadBalancer => "loadbalancer",
      WellKnownCategory.Mail => "mail",
      WellKnownCategory.MailingList => "mailinglist",
      WellKnownCategory.Memory => "memory",
      WellKnownCategory.Munin => "munin",
      WellKnownCategory.Network => "network",
      WellKnownCategory.Other => "other",
      WellKnownCategory.Printing => "printing",
      WellKnownCategory.Process => "processes",
      WellKnownCategory.Radio => "radio",
      WellKnownCategory.StorageAreaNetwork => "san",
      WellKnownCategory.Search => "search",
      WellKnownCategory.Security => "security",
      WellKnownCategory.Sensor => "sensors",
      WellKnownCategory.SpamFilter => "spamfilter",
      WellKnownCategory.Streaming => "streaming",
      WellKnownCategory.System => "system",
      WellKnownCategory.TimeSynchronization => "time",
      WellKnownCategory.Video => "tv",
      WellKnownCategory.Virtualization => "virtualization",
      WellKnownCategory.VoIP => "voip",
      WellKnownCategory.WebServer => "webserver",
      WellKnownCategory.Wiki => "wiki",
      WellKnownCategory.Wireless => "wireless",
      _ => throw new ArgumentException("not a well known category", paramName: nameof(category)),
    };
  #endregion cSpell:enable
#pragma warning restore SA1124

  /// <summary>Sets a value for the <c>graph_width</c> and <c>graph_height</c>.</summary>
  /// <seealso cref="WithWidth"/>
  /// <seealso cref="WithHeight"/>
  /// <exception cref="ArgumentOutOfRangeException">
  /// <paramref name="width"/> is less than or equal to <c>0</c>, or
  /// <paramref name="height"/> is less than or equal to <c>0</c>.
  /// </exception>
  public PluginGraphAttributesBuilder WithSize(int width, int height)
    => WithWidth(width).WithHeight(height);
}
