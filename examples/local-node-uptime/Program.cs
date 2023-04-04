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

var plugins = new[] {
  PluginFactory.CreatePlugin(
    name: "uptime",
    fieldLabel: nodeHostName,
    fieldGraphStyle: PluginFieldGraphStyle.Area,
    fetchFieldValue: () => (DateTime.Now - startAt).TotalMinutes,
    graphAttributes: new PluginGraphAttributes(
      category: "system",
      title: "Uptime of local-node",
      verticalLabel: "Uptime [minutes]",
      scale: false,
      updateRate: TimeSpan.FromMinutes(1.0),
      arguments: "--base 1000 --lower-limit 0"
    )
  ),
};

var services = new ServiceCollection();

services.AddLogging(
  builder => builder
    .AddSimpleConsole(static options => options.SingleLine = true)
    .AddFilter(static level => LogLevel.Trace <= level)
);

await using var node = new LocalNode(
  plugins: plugins,
  hostName: nodeHostName,
  port: nodePort,
  serviceProvider: services.BuildServiceProvider()
);

var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, args) => {
  cts.Cancel();
  args.Cancel = true;
};

node.Start();

await node.AcceptAsync(throwIfCancellationRequested: false, cts.Token);

Console.WriteLine("stopped");
