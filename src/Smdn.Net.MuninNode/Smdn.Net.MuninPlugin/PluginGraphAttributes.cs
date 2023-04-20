// SPDX-FileCopyrightText: 2021 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;

namespace Smdn.Net.MuninPlugin;

/// <summary>
/// Represents attributes related to the drawing of a single graph.
/// Defines graph attributes that should be returned when the plugin is called with the 'config' argument.
/// This type represents the collection of 'field name attributes'.
/// </summary>
/// <seealso href="http://guide.munin-monitoring.org/en/latest/reference/plugin.html#global-attributes">Plugin reference - Global attributes</seealso>
public sealed class PluginGraphAttributes {
  /// <summary>Gets a value for the <c>graph_title</c>.</summary>
  /// <seealso href="http://guide.munin-monitoring.org/en/latest/reference/plugin.html#graph-title">Plugin reference - Global attributes - graph_title</seealso>
  public string Title { get; }

  /// <summary>Gets a value for the <c>graph_category</c>.</summary>
  /// <seealso href="http://guide.munin-monitoring.org/en/latest/reference/plugin.html#graph-category">Plugin reference - Global attributes - graph_category</seealso>
  /// <seealso href="http://guide.munin-monitoring.org/en/latest/reference/graph-category.html">Plugin graph categories</seealso>
  public string Category { get; }

  /// <summary>Gets a value for the <c>graph_args</c>.</summary>
  /// <seealso href="http://guide.munin-monitoring.org/en/latest/reference/plugin.html#graph-args">Plugin reference - Global attributes - graph_args</seealso>
  /// <seealso href="http://guide.munin-monitoring.org/en/latest/example/graph/graph_args.html">Recommended graph_args</seealso>
  public string Arguments { get; }

  /// <summary>Gets a value indicating whether the field value should be scaled. Represents the value for the <c>graph_scale</c>.</summary>
  /// <seealso href="http://guide.munin-monitoring.org/en/latest/reference/plugin.html#graph-scale">Plugin reference - Global attributes - graph_scale</seealso>
  public bool Scale { get; }

  /// <summary>Gets a value for the <c>graph_vlabel</c>.</summary>
  /// <seealso href="http://guide.munin-monitoring.org/en/latest/reference/plugin.html#graph-vlabel">Plugin reference - Global attributes - graph_vlabel</seealso>
  public string VerticalLabel { get; }

  /// <summary>Gets a value for the <c>update_rate</c>.</summary>
  /// <seealso href="http://guide.munin-monitoring.org/en/latest/reference/plugin.html#update-rate">Plugin reference - Global attributes - update_rate</seealso>
  public TimeSpan UpdateRate { get; }

  /// <summary>Gets a value for the <c>graph_width</c>.</summary>
  /// <seealso href="http://guide.munin-monitoring.org/en/latest/reference/plugin.html#graph-width">Plugin reference - Global attributes - graph_width</seealso>
  public int? Width { get; }

  /// <summary>Gets a value for the <c>graph_height</c>.</summary>
  /// <seealso href="http://guide.munin-monitoring.org/en/latest/reference/plugin.html#graph-height">Plugin reference - Global attributes - graph_height</seealso>
  public int? Height { get; }

  public PluginGraphAttributes(
    string title,
    string category,
    string verticalLabel,
    bool scale,
    string arguments,
    TimeSpan updateRate,
    int? width = null,
    int? height = null
  )
  {
    if (title == null)
      throw new ArgumentNullException(nameof(title));
    if (title.Length == 0)
      throw ExceptionUtils.CreateArgumentMustBeNonEmptyString(nameof(title));
    if (category == null)
      throw new ArgumentNullException(nameof(category));
    if (category.Length == 0)
      throw ExceptionUtils.CreateArgumentMustBeNonEmptyString(nameof(category));
    if (arguments == null)
      throw new ArgumentNullException(nameof(arguments));
    if (arguments.Length == 0)
      throw ExceptionUtils.CreateArgumentMustBeNonEmptyString(nameof(arguments));
    if (verticalLabel == null)
      throw new ArgumentNullException(nameof(verticalLabel));
    if (verticalLabel.Length == 0)
      throw ExceptionUtils.CreateArgumentMustBeNonEmptyString(nameof(verticalLabel));
    if (width.HasValue && width.Value <= 0)
      throw ExceptionUtils.CreateArgumentMustBeGreaterThan(0, nameof(width), width);
    if (height.HasValue && height.Value <= 0)
      throw ExceptionUtils.CreateArgumentMustBeGreaterThan(0, nameof(height), height);

    Title = title;
    Category = category;
    Arguments = arguments;
    Scale = scale;
    VerticalLabel = verticalLabel;
    Width = width;
    Height = height;

    if (updateRate < TimeSpan.FromSeconds(1.0))
      throw new ArgumentOutOfRangeException(nameof(updateRate), updateRate, "must be at least 1 seconds");

    UpdateRate = updateRate;
  }
}
