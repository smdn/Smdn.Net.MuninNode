// SPDX-FileCopyrightText: 2021 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;

namespace Smdn.Net.MuninPlugin;

// ref: http://guide.munin-monitoring.org/en/latest/reference/plugin.html#global-attributes
public sealed class PluginGraphAttributes {
  public string Title { get; }
  public string Category { get; }
  public string Arguments { get; }
  public bool Scale { get; }
  public string VerticalLabel { get; }
  public TimeSpan UpdateRate { get; }
  public int? Width { get; }
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
