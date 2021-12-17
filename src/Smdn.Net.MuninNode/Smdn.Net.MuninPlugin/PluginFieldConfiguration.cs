// SPDX-FileCopyrightText: 2021 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;

namespace Smdn.Net.MuninPlugin;

public abstract class PluginFieldConfiguration {
  public string DefaultGraphStyle { get; }
  public Range? WarningValueRange { get; }
  public Range? CriticalValueRange { get; }

  protected PluginFieldConfiguration(
    string defaultGraphStyle,
    Range? warningValueRange = null,
    Range? criticalValueRange = null
  )
  {
    DefaultGraphStyle = defaultGraphStyle;
    WarningValueRange = warningValueRange;
    CriticalValueRange = criticalValueRange;
  }

  public abstract IEnumerable<PluginField> FetchFields();
}
