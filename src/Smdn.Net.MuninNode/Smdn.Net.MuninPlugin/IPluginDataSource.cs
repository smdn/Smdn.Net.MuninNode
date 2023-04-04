// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System.Collections.Generic;

namespace Smdn.Net.MuninPlugin;

public interface IPluginDataSource {
  IReadOnlyCollection<IPluginField> Fields { get; }
}
