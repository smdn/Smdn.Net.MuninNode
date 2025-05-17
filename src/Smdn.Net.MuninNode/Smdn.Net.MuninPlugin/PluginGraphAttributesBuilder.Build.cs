// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;

namespace Smdn.Net.MuninPlugin;

#pragma warning disable IDE0040
partial class PluginGraphAttributesBuilder {
#pragma warning restore IDE0040
  public IPluginGraphAttributes Build()
  {
    var attributes = new List<string>(capacity: 8);

    if (showGraph is bool graph)
      attributes.Add($"graph {(graph ? "yes" : "no")}");

    var joinedGraphArgs = string.Join(" ", graphArgs);

    if (!string.IsNullOrEmpty(joinedGraphArgs))
      attributes.Add($"graph_args {joinedGraphArgs}");

    if (category is not null)
      attributes.Add($"graph_category {category}");

    if (height is int h)
      attributes.Add($"graph_height {h}");

    // attributes.Add($"graph_info {graph_info}");

    if (!string.IsNullOrEmpty(order))
      attributes.Add($"graph_order {order}");

    // attributes.Add($"graph_period {graph_period}");

    if (printf is not null)
      attributes.Add($"graph_printf {printf}");

    if (scale is bool s)
      attributes.Add($"graph_scale {(s ? "yes" : "no")}");

    attributes.Add($"graph_title {title}");

    if (labelForTotal is not null)
      attributes.Add($"graph_total {labelForTotal}");

    if (verticalLabel is not null)
      attributes.Add($"graph_vlabel {verticalLabel}");

    if (width is int w)
      attributes.Add($"graph_width {w}");

    if (updateRate is TimeSpan urate)
      attributes.Add($"update_rate {(int)urate.TotalSeconds}");

    attributes.TrimExcess();

    return new PluginGraphAttributes(attributes);
  }

  private sealed class PluginGraphAttributes(IReadOnlyList<string> attributes) : IPluginGraphAttributes {
    public IEnumerable<string> EnumerateAttributes() => attributes;
  }
}
