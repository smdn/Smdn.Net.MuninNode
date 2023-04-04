// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

namespace Smdn.Net.MuninPlugin;

public interface IPlugin {
  string Name { get; }
  PluginGraphAttributes GraphAttributes { get; }
  IPluginDataSource DataSource { get; }
  INodeSessionCallback? SessionCallback { get; }
}
