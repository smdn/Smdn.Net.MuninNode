// SPDX-FileCopyrightText: 2021 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;

namespace Smdn.Net.MuninPlugin;

public class Plugin {
  public string Name { get; }
  public PluginGraphConfiguration GraphConfiguration { get; }
  public PluginFieldConfiguration FieldConfiguration { get; }

  public Plugin(
    string name,
    PluginGraphConfiguration graphConfiguration,
    PluginFieldConfiguration fieldConfiguration
  )
  {
    if (name == null)
      throw new ArgumentNullException(nameof(name));
    if (name.Length == 0)
      throw ExceptionUtils.CreateArgumentMustBeNonEmptyString(nameof(name));

    Name = name;
    GraphConfiguration = graphConfiguration ?? throw new ArgumentNullException(nameof(graphConfiguration));
    FieldConfiguration = fieldConfiguration ?? throw new ArgumentNullException(nameof(fieldConfiguration));
  }
}
