// SPDX-FileCopyrightText: 2026 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using Microsoft.Extensions.Logging;

namespace Smdn.Net.MuninNode.Hosting;

#pragma warning disable IDE0040
partial class MuninNodeBackgroundService {
#pragma warning restore IDE0040
  [LoggerMessage(
    Level = LogLevel.Information,
    EventId = 1,
    Message = "Munin node '{HostName}' starting."
  )]
  private static partial void LogInformationStarting(ILogger logger, string hostName);

  [LoggerMessage(
    Level = LogLevel.Information,
    EventId = 2,
    Message = "Munin node '{HostName}' started."
  )]
  private static partial void LogInformationStarted(ILogger logger, string hostName);

  [LoggerMessage(
    Level = LogLevel.Information,
    EventId = 3,
    Message = "Munin node '{HostName}' stopping."
  )]
  private static partial void LogInformationStopping(ILogger logger, string hostName);

  [LoggerMessage(
    Level = LogLevel.Information,
    EventId = 4,
    Message = "Munin node '{HostName}' stopped."
  )]
  private static partial void LogInformationStopped(ILogger logger, string hostName);
}
