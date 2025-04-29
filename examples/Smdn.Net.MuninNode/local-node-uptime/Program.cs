// SPDX-FileCopyrightText: 2021 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Net;
using System.Threading;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Smdn.Net.MuninNode;
using Smdn.Net.MuninPlugin;

const string NodeHostName = "test.munin-node.localhost";
const int NodePort = 14949;

var startAt = DateTime.Now;

// Define plugins
var plugins = new[] {
  PluginFactory.CreatePlugin(
    name: "uptime",
    fieldLabel: NodeHostName,
    // Specify 'AREA' as the drawing style for values on the graph
    fieldGraphStyle: PluginFieldGraphStyle.Area,
    // Set the number of minutes elapsed from the start time of the process as the 'uptime' value.
    fetchFieldValue: () => (DateTime.Now - startAt).TotalMinutes,
    // Configure the drawing on the graph for this plugin.
    graphAttributes: new PluginGraphAttributes(
      // 'Well known categories' are defined by Munin.
      // See https://guide.munin-monitoring.org/en/latest/reference/graph-category.html
      category: "system",
      title: $"Uptime of {NodeHostName}",
      verticalLabel: "Uptime [minutes]",
      scale: false,
      // Specify arguments for graph drawing. See below for more information about graph arguments:
      //   https://guide.munin-monitoring.org/en/latest/reference/plugin.html#graph-args
      //   https://guide.munin-monitoring.org/en/latest/example/graph/graph_args.html
      arguments: "--base 1000 --lower-limit 0"
    )
  ),
  // You can also run multiple plugins for one single node.
  // PluginFactory.CreatePlugin(name: "sensor1", ...),
  // PluginFactory.CreatePlugin(name: "sensor2", ...),
};

// The LocalNode class supports logging by the ILogger interface.
// Console logger is used here.
var services = new ServiceCollection();

services.AddLogging(
  builder => builder
    .AddSimpleConsole(static options => {
      options.SingleLine = true;
      options.IncludeScopes = true;
    })
    .AddFilter(static level => LogLevel.Trace <= level)
);

// Create LocalNode
await using var node = LocalNode.Create(
  plugins: plugins,
  port: NodePort,
  hostName: NodeHostName,
  addressListAllowFrom: [IPAddress.Loopback, IPAddress.IPv6Loopback],
  serviceProvider: services.BuildServiceProvider()
);

// Keep the node running until Ctrl+C is pressed.
var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, args) => {
  cts.Cancel();
  args.Cancel = true;
};

// Start the node and accept connections.
//
// When the RunAsync method finishes processing one connection,
// it immediately waits for the next connection.
// Therefore, the method will not return until the cancellation is
// requested by CancellationToken.
try {
  await node.RunAsync(cts.Token);
}
catch (OperationCanceledException ex) when (ex.CancellationToken == cts.Token) {
  // CancellationToken is triggered
  Console.WriteLine("stopped");
}

// Instead of RunAsync(), StartAsync() and AcceptAsync() can be called
// separately to start the node.
//
// await node.StartAsync(cts.Token);
// await node.AcceptAsync(throwIfCancellationRequested: false, cts.Token);
// Console.WriteLine("stopped");
