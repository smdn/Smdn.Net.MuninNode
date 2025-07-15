// SPDX-FileCopyrightText: 2021 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.MuninPlugin;

#pragma warning disable IDE0055
public class Plugin :
  IPlugin,
  IPluginDataSource,
#pragma warning disable CS0618
  INodeSessionCallback,
#pragma warning restore CS0618
  ITransactionCallback
{
#pragma warning restore IDE0055
  public string Name { get; }

  public PluginGraphAttributes GraphAttributes { get; }
  public IReadOnlyCollection<IPluginField> Fields { get; }

#pragma warning disable CA1033
  IPluginGraphAttributes IPlugin.GraphAttributes => GraphAttributes;

  IPluginDataSource IPlugin.DataSource => this;

  IReadOnlyCollection<IPluginField> IPluginDataSource.Fields => Fields;

#pragma warning disable CS0618
  [Obsolete(message: INodeSessionCallback.ObsoleteMessage.SessionCallbackProperty)]
#pragma warning restore CS0618
  INodeSessionCallback? IPlugin.SessionCallback => this;
#pragma warning restore CA1033

  public Plugin(
    string name,
    PluginGraphAttributes graphAttributes,
    IReadOnlyCollection<IPluginField> fields
  )
  {
    ArgumentExceptionShim.ThrowIfNullOrEmpty(name, nameof(name));

    Name = name;
    GraphAttributes = graphAttributes ?? throw new ArgumentNullException(nameof(graphAttributes));
    Fields = fields ?? throw new ArgumentNullException(nameof(fields));
  }

  [Obsolete]
  ValueTask INodeSessionCallback.ReportSessionStartedAsync(string sessionId, CancellationToken cancellationToken)
    => ReportSessionStartedAsync(sessionId, cancellationToken);

  [Obsolete($"This method will be removed in the next major version release. Override {nameof(StartTransactionAsync)} instead.")]
  protected virtual ValueTask ReportSessionStartedAsync(string sessionId, CancellationToken cancellationToken)
    => default; // do nothing in this class

  [Obsolete]
  ValueTask INodeSessionCallback.ReportSessionClosedAsync(string sessionId, CancellationToken cancellationToken)
    => ReportSessionClosedAsync(sessionId, cancellationToken);

  [Obsolete($"This method will be removed in the next major version release. Override {nameof(EndTransactionAsync)} instead.")]
  protected virtual ValueTask ReportSessionClosedAsync(string sessionId, CancellationToken cancellationToken)
    => default; // do nothing in this class

  ValueTask ITransactionCallback.StartTransactionAsync(CancellationToken cancellationToken)
    => StartTransactionAsync(cancellationToken);

  protected virtual ValueTask StartTransactionAsync(CancellationToken cancellationToken)
    => default; // do nothing in this class

  ValueTask ITransactionCallback.EndTransactionAsync(CancellationToken cancellationToken)
    => EndTransactionAsync(cancellationToken);

  protected virtual ValueTask EndTransactionAsync(CancellationToken cancellationToken)
    => default; // do nothing in this class
}
