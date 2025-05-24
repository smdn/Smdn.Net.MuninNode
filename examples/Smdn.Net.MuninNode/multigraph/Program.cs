// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
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

var plugins = new IPlugin[] {
  new MultigraphPlugin(
    name: "multigraph_root",
    plugins: [
      PluginFactory.CreatePlugin(
        name: "subgraph_1",
        fieldLabel: "value",
        fetchFieldValue: () => Random.Shared.Next(0, 10),
        graphAttributes: new PluginGraphAttributesBuilder(title: "Sub-graph 1")
          .WithCategoryOther()
          .WithVerticalLabel("number")
          .WithGraphLimit(0, 10)
          .Build()
      ),
      PluginFactory.CreatePlugin(
        name: "subgraph_2",
        fieldLabel: "value",
        fetchFieldValue: () => Random.Shared.Next(0, 10),
        graphAttributes: new PluginGraphAttributesBuilder(title: "Sub-graph 2")
          .WithCategoryOther()
          .WithVerticalLabel("number")
          .WithGraphLimit(0, 10)
          .Build()
      ),
      PluginFactory.CreatePlugin(
        name: "subgraph_3",
        fieldLabel: "value",
        fetchFieldValue: () => Random.Shared.Next(0, 10),
        graphAttributes: new PluginGraphAttributesBuilder(title: "Sub-graph 3")
          .WithCategoryOther()
          .WithVerticalLabel("number")
          .WithGraphLimit(0, 10)
          .Build()
      ),
    ]
  )
};

var services = new ServiceCollection();

services.AddLogging(
  builder => builder
    .AddSimpleConsole(static options => {
      options.SingleLine = true;
      options.IncludeScopes = true;
    })
    .AddFilter(static level => LogLevel.Trace <= level)
);

await using var node = LocalNode.Create(
  plugins: plugins,
  port: NodePort,
  hostName: NodeHostName,
  addressListAllowFrom: [IPAddress.Loopback, IPAddress.IPv6Loopback],
  serviceProvider: services.BuildServiceProvider()
);

var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, args) => {
  cts.Cancel();
  args.Cancel = true;
};

try {
  await node.RunAsync(cts.Token);
}
catch (OperationCanceledException ex) when (ex.CancellationToken == cts.Token) {
  Console.WriteLine("stopped");
}
