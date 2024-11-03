// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Collections.Generic;

namespace Smdn.Net.MuninPlugin;

/// <summary>
/// Provides an interface that abstracts the plugin graph attributes, related to the drawing of a single graph.
/// </summary>
/// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/plugin.html#global-attributes">Plugin reference - Global attributes</seealso>
public interface IPluginGraphAttributes {
  /// <summary>
  /// Enumerates plugin graph attributes defined by types that implement this interface.
  /// </summary>
  /// <returns><see cref="IEnumerable{String}"/> that enumerates graph attributes.</returns>
  IEnumerable<string> EnumerateAttributes();
}
