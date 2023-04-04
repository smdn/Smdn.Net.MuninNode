// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.MuninPlugin;

public interface INodeSessionCallback {
  ValueTask ReportSessionStartedAsync(string sessionId, CancellationToken cancellationToken);
  ValueTask ReportSessionClosedAsync(string sessionId, CancellationToken cancellationToken);
}
