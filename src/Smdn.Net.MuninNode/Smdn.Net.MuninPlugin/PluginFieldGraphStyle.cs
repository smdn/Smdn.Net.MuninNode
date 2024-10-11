// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

namespace Smdn.Net.MuninPlugin;

/// <summary>
/// Represents the style of how the field should be drawn on the graph.
/// </summary>
/// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/plugin.html#fieldname-draw">Plugin reference - Field name attributes - {fieldname}.draw</seealso>
public enum PluginFieldGraphStyle {
  Default = default,

  Area = 1,
  Stack = 2,
  AreaStack = 3,

  Line = 100,
  LineWidth1 = 101,
  LineWidth2 = 102,
  LineWidth3 = 103,

  LineStack = 200,
  LineStackWidth1 = 201,
  LineStackWidth2 = 202,
  LineStackWidth3 = 203,
}
