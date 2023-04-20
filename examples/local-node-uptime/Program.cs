// SPDX-FileCopyrightText: 2021 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Threading;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Smdn.Net.MuninNode;
using Smdn.Net.MuninPlugin;

const string nodeHostName = "test.munin-node.localhost";
const int nodePort = 14949;

var startAt = DateTime.Now;

// Define plugins
var plugins = new[] {
  PluginFactory.CreatePlugin(
    name: "uptime",
    fieldLabel: nodeHostName,
    // Specify 'AREA' as the drawing style for values on the graph
    fieldGraphStyle: PluginFieldGraphStyle.Area,
    // Set the number of minutes elapsed from the start time of the process as the 'uptime' value.
    fetchFieldValue: () => (DateTime.Now - startAt).TotalMinutes,
    // Configure the drawing on the graph for this plugin.
    graphAttributes: new PluginGraphAttributes(
      // 'Well known categories' are defined by Munin.
      // See http://guide.munin-monitoring.org/en/latest/reference/graph-category.html
      category: "system",
      title: $"Uptime of {nodeHostName}",
      verticalLabel: "Uptime [minutes]",
      scale: false,
      updateRate: TimeSpan.FromMinutes(1.0),
      // Specify arguments for graph drawing. See below for more information about graph arguments:
      //   http://guide.munin-monitoring.org/en/latest/reference/plugin.html#graph-args
      //   http://guide.munin-monitoring.org/en/latest/example/graph/graph_args.html
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
    .AddSimpleConsole(static options => options.SingleLine = true)
    .AddFilter(static level => LogLevel.Trace <= level)
);

// Create LocalNode
await using var node = new LocalNode(
  plugins: plugins,
  hostName: nodeHostName,
  port: nodePort,
  serviceProvider: services.BuildServiceProvider()
);

// Keep the node running until Ctrl+C is pressed.
var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, args) => {
  cts.Cancel();
  args.Cancel = true;
};

// Start node and accept connections.
node.Start();

await node.AcceptAsync(throwIfCancellationRequested: false, cts.Token);

// When the AcceptAsync method finishes processing one connection,
// it immediately waits for the next connection.
// Therefore, the method will not return until the cancellation is
// requested by CancellationToken.
//
// By specifying false to the throwIfCancellationRequested, you can
// let the AcceptAsync method to return without throwing
// OperationCanceledException when a cancellation is requested.

Console.WriteLine("stopped");
