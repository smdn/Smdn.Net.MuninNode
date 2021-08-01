// SPDX-FileCopyrightText: 2021 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;

using Smdn.Net.MuninPlugin;

class UptimeFieldConfiguration : PluginFieldConfiguration {
  private readonly string label;
  private readonly DateTime startAt;

  public UptimeFieldConfiguration(string label, DateTime startAt)
    : base(defaultGraphStyle: "AREA")
  {
    this.label = label;
    this.startAt = startAt;
  }

  public override IEnumerable<PluginField> FetchFields()
  {
    yield return new PluginField(
      label: label,
      value: (DateTime.Now - startAt).TotalMinutes,
      graphStyle: DefaultGraphStyle
    );
  }
}