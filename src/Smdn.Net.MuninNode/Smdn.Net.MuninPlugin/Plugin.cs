// SPDX-FileCopyrightText: 2021 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.MuninPlugin;

public class Plugin : IPlugin, IPluginDataSource, INodeSessionCallback {
  public string Name { get; }

  public PluginGraphAttributes GraphAttributes { get; }
  public IReadOnlyCollection<IPluginField> Fields { get; }

  IPluginDataSource IPlugin.DataSource => this;
  IReadOnlyCollection<IPluginField> IPluginDataSource.Fields => Fields;

  INodeSessionCallback? IPlugin.SessionCallback => this;

  public Plugin(
    string name,
    PluginGraphAttributes graphAttributes,
    IReadOnlyCollection<IPluginField> fields
  )
  {
    if (name == null)
      throw new ArgumentNullException(nameof(name));
    if (name.Length == 0)
      throw ExceptionUtils.CreateArgumentMustBeNonEmptyString(nameof(name));

    Name = name;
    GraphAttributes = graphAttributes ?? throw new ArgumentNullException(nameof(graphAttributes));
    Fields = fields ?? throw new ArgumentNullException(nameof(fields));
  }

  ValueTask INodeSessionCallback.ReportSessionStartedAsync(string sessionId, CancellationToken cancellationToken)
    => ReportSessionStartedAsync(sessionId, cancellationToken);

  protected virtual ValueTask ReportSessionStartedAsync(string sessionId, CancellationToken cancellationToken)
    => default; // do nothing in this class

  ValueTask INodeSessionCallback.ReportSessionClosedAsync(string sessionId, CancellationToken cancellationToken)
    => ReportSessionClosedAsync(sessionId, cancellationToken);

  protected virtual ValueTask ReportSessionClosedAsync(string sessionId, CancellationToken cancellationToken)
    => default; // do nothing in this class
}
