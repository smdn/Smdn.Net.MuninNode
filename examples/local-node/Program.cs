// SPDX-FileCopyrightText: 2021 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

using Smdn.Net.MuninNode;
using Smdn.Net.MuninPlugin;

const string hostName = "test.munin-node.localhost";

var plugins = new[] {
  new Plugin(
    name: "uptime",
    graphConfiguration: new PluginGraphConfiguration(
      category: "system",
      title: "Uptime of local-node",
      verticalLabel: "Uptime [minutes]",
      scale: false,
      updateRate: TimeSpan.FromMinutes(1.0),
      arguments: "--base 1000 --lower-limit 0"
    ),
    fieldConfiguration: new UptimeFieldConfiguration(
      label: hostName,
      startAt: DateTime.Now
    )
  ),
};

var services = new ServiceCollection();

services.AddLogging(
  builder => builder
    .AddSimpleConsole(static options => options.SingleLine = true)
    .AddFilter(static level => LogLevel.Trace <= level)
);

using var localNode = new LocalNode(
  plugins: plugins,
  hostName: hostName,
  portNumber: 14949,
  timeout: TimeSpan.FromSeconds(10),
  serviceProvider: services.BuildServiceProvider()
);

localNode.Start();

for (;;) {
  await localNode.AcceptClientAsync();
}