// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;

namespace Smdn.Net.MuninPlugin;

internal static class IPluginProviderExtensions {
  public static IEnumerable<IPlugin> EnumeratePlugins(
    this IPluginProvider pluginProvider,
    bool flattenMultigraphPlugins
  )
  {
    if (pluginProvider is null)
      throw new ArgumentNullException(nameof(pluginProvider));

    foreach (var plugin in pluginProvider.Plugins) {
      if (flattenMultigraphPlugins && plugin is IMultigraphPlugin multigraphPlugin) {
        // expand IMultigraphPlugin.Plugins so that each IPlugin is handled as an individual plugin
        foreach (var subPlugin in multigraphPlugin.Plugins) {
          yield return subPlugin;
        }
      }
      else {
        yield return plugin;
      }
    }
  }
}
