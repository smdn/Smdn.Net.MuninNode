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
#pragma warning disable IDE0032
  // In addition to the Logger property exposed outside the class, provide an
  // ILogger field that is referenced by the LoggerMessage source generator.
  private readonly ILogger logger;
#pragma warning restore IDE0032

  [LoggerMessage(
    Level = LogLevel.Debug,
    EventId = 1,
    Message = "Starting munin-node listener."
  )]
  private partial void LogDebugStartingNode();

  [LoggerMessage(
    Level = LogLevel.Information,
    EventId = 2,
    Message = "Started munin-node '{HostName}' on '{EndPoint}'."
  )]
  private partial void LogInformationStartedNode(string hostName, EndPoint? endPoint);

  [LoggerMessage(
    Level = LogLevel.Debug,
    EventId = 3,
    Message = "Stopping munin-node '{HostName}'."
  )]
  private partial void LogDebugStoppingNode(string hostName);

  [LoggerMessage(
    Level = LogLevel.Information,
    EventId = 4,
    Message = "Stopped munin-node '{HostName}'."
  )]
  private partial void LogInformationStoppedNode(string hostName);

  [LoggerMessage(
    Level = LogLevel.Information,
    EventId = 5,
    Message = "Started accepting connections."
  )]
  private partial void LogInformationStartedAcceptingConnections();

  [LoggerMessage(
    Level = LogLevel.Debug,
    EventId = 6,
    Message = "Stopped accepting connections."
  )]
  private partial void LogDebugStoppedAcceptingConnections();

  [LoggerMessage(
    Level = LogLevel.Debug,
    EventId = 7,
    Message = "Accepting a connection..."
  )]
  private partial void LogDebugAcceptingConnection();

  [LoggerMessage(
    Level = LogLevel.Debug,
    EventId = 8,
    Message = "Accepted connection closed."
  )]
  private partial void LogDebugAcceptedConnectionClosed();

  [LoggerMessage(
    Level = LogLevel.Information,
    EventId = 9,
    Message = "Can not accept connection from {RemoteEndPoint} ({RemoteEndPointAddressFamily})."
  )]
  private partial void LogInformationConnectionCanNotAccept(EndPoint? remoteEndPoint, AddressFamily? remoteEndPointAddressFamily);

  [LoggerMessage(
    Level = LogLevel.Warning,
    EventId = 10,
    Message = "Access refused."
  )]
  private partial void LogWarningAccessRefused();

  [LoggerMessage(
    Level = LogLevel.Debug,
    EventId = 50,
    Message = "Starting transaction."
  )]
  private partial void LogDebugStartingTransaction();

  [LoggerMessage(
    Level = LogLevel.Error,
    EventId = 51,
    Message = "Unexpected exception occurred while starting transaction."
  )]
  private partial void LogErrorUnexpectedExceptionWhileStartingTransaction(Exception ex);

  [LoggerMessage(
    Level = LogLevel.Information,
    EventId = 61,
    Message = "Session started."
  )]
  private partial void LogInformationSessionStarted();

  [LoggerMessage(
    Level = LogLevel.Information,
    EventId = 62,
    Message = "Session closed."
  )]
  private partial void LogInformationSessionClosed();

  [LoggerMessage(
    Level = LogLevel.Warning,
    EventId = 63,
    Message = "Operation canceled while receiving."
  )]
  private partial void LogWarningSessionOperationCanceledWhileReceiving();

  [LoggerMessage(
    Level = LogLevel.Error,
    EventId = 64,
    Message = "Unexpected exception occurred while receiving."
  )]
  private partial void LogErrorSessionUnexpectedExceptionWhileReceiving(Exception ex);

  [LoggerMessage(
    Level = LogLevel.Information,
    EventId = 65,
    Message = "Client disconnected while sending."
  )]
  private partial void LogInformationSessionClientDisconnectedWhileSending();

  [LoggerMessage(
    Level = LogLevel.Warning,
    EventId = 66,
    Message = "Operation canceled while processing command."
  )]
  private partial void LogWarningSessionOperationCanceledWhileProcessingCommand();

  [LoggerMessage(
    Level = LogLevel.Error,
    EventId = 67,
    Message = "Unexpected exception occurred while processing command."
  )]
  private partial void LogErrorSessionUnexpectedExceptionWhileProcessingCommand(Exception ex);

  private static readonly Func<ILogger, string, IDisposable?> LoggerScopeForSession = LoggerMessage.DefineScope<string>(
    formatString: "[{SessionId}]"
  );

  /*
   * Logger messages for obsolete members
   */
  [LoggerMessage(
    EventId = 200,
    Level = LogLevel.Information,
    Message = "Starting."
  )]
  private partial void LogInformationStartStarting();

  [LoggerMessage(
    EventId = 201,
    Level = LogLevel.Information,
    Message = "Started. (End point: {EndPoint})"
  )]
  private partial void LogInformationStartStarted(EndPoint? endPoint);
}
