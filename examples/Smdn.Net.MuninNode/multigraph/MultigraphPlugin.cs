// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;

using Smdn.Net.MuninPlugin;

class MultigraphPlugin(string name, IReadOnlyCollection<IPlugin> plugins): IMultigraphPlugin {
  public string Name { get; } = name;
  public IReadOnlyCollection<IPlugin> Plugins { get; } = plugins;
  public IPluginDataSource DataSource => throw new NotSupportedException();
  public IPluginGraphAttributes GraphAttributes => throw new NotSupportedException();
  public INodeSessionCallback? SessionCallback => null;
}
