// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;
using System.Net.Sockets;

using Microsoft.Extensions.Logging;

namespace Smdn.Net.MuninNode;

#pragma warning disable IDE0040
partial class NodeBase {
#pragma warning restore IDE0040
  private static readonly Action<ILogger, Exception?> LogStartingNode = LoggerMessage.Define(
    LogLevel.Debug,
    eventId: default, // TODO
    formatString: "Starting munin-node listener."
  );
  private static readonly Action<ILogger, string, EndPoint?, Exception?> LogStartedNode = LoggerMessage.Define<string, EndPoint?>(
    LogLevel.Information,
    eventId: default, // TODO
    formatString: "Started munin-node '{HostName}' on '{EndPoint}'."
  );
  private static readonly Action<ILogger, Exception?> LogStartedAcceptingConnections = LoggerMessage.Define(
    LogLevel.Information,
    eventId: default, // TODO
    formatString: "Started accepting connections."
  );
  private static readonly Action<ILogger, Exception?> LogStoppedAcceptingConnections = LoggerMessage.Define(
    LogLevel.Debug,
    eventId: default, // TODO
    formatString: "Stopped accepting connections."
  );
  private static readonly Action<ILogger, Exception?> LogAcceptingConnection = LoggerMessage.Define(
    LogLevel.Debug,
    eventId: default, // TODO
    formatString: "Accepting a connection..."
  );
  private static readonly Action<ILogger, Exception?> LogAcceptedConnectionClosed = LoggerMessage.Define(
    LogLevel.Debug,
    eventId: default, // TODO
    formatString: "Accepted connection closed."
  );
  private static readonly Action<ILogger, EndPoint?, AddressFamily?, Exception?> LogConnectionCanNotAccept = LoggerMessage.Define<EndPoint?, AddressFamily?>(
    LogLevel.Information,
    eventId: default, // TODO
    formatString: "Can not accept connection from {RemoteEndPoint} ({RemoteEndPointAddressFamily})."
  );
  private static readonly Action<ILogger, Exception?> LogAccessRefused = LoggerMessage.Define(
    LogLevel.Warning,
    eventId: default, // TODO
    formatString: "Access refused."
  );
  private static readonly Action<ILogger, Exception?> LogStartingTransaction = LoggerMessage.Define(
    LogLevel.Debug,
    eventId: default, // TODO
    formatString: "Starting transaction."
  );
  private static readonly Action<ILogger, Exception?> LogUnexpectedExceptionWhileStartingTransaction = LoggerMessage.Define(
    LogLevel.Error,
    eventId: default, // TODO
    formatString: "Unexpected exception occured while starting transaction."
  );
  private static readonly Action<ILogger, Exception?> LogSessionStarted = LoggerMessage.Define(
    LogLevel.Information,
    eventId: default, // TODO
    formatString: "Session started."
  );
  private static readonly Action<ILogger, Exception?> LogSessionClosed = LoggerMessage.Define(
    LogLevel.Information,
    eventId: default, // TODO
    formatString: "Session closed."
  );
  private static readonly Action<ILogger, Exception?> LogSessionOperationCanceledWhileReceiving = LoggerMessage.Define(
    LogLevel.Warning,
    eventId: default, // TODO
    formatString: "Operation canceled while receiving."
  );
  private static readonly Action<ILogger, Exception?> LogSessionUnexpectedExceptionWhileReceiving = LoggerMessage.Define(
    LogLevel.Error,
    eventId: default, // TODO
    formatString: "Unexpected exception occured while receiving."
  );
  private static readonly Action<ILogger, Exception?> LogSessionClientDisconnectedWhileSeinding = LoggerMessage.Define(
    LogLevel.Information,
    eventId: default, // TODO
    formatString: "Client disconnected while sending."
  );
  private static readonly Action<ILogger, Exception?> LogSessionOperationCanceledWhileProcessingCommand = LoggerMessage.Define(
    LogLevel.Warning,
    eventId: default, // TODO
    formatString: "Operation canceled while processing command."
  );
  private static readonly Action<ILogger, Exception?> LogSessionUnexpectedExceptionWhileProcessingCommand = LoggerMessage.Define(
    LogLevel.Error,
    eventId: default, // TODO
    formatString: "Unexpected exception occured while processing command."
  );

  private static readonly Func<ILogger, string, IDisposable?> LoggerScopeForSession = LoggerMessage.DefineScope<string>(
    formatString: "[{SessionId}]"
  );
}
