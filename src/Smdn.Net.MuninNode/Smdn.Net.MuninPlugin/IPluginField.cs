// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.MuninPlugin;

// ref: http://guide.munin-monitoring.org/en/latest/reference/plugin.html#field-name-attributes
public interface IPluginField {
  string Name { get; }

  PluginFieldAttributes Attributes { get; }

  ValueTask<string> GetFormattedValueStringAsync(CancellationToken cancellationToken);
}
