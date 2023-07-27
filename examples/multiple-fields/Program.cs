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

double? FetchValue1() => Random.Shared.Next(0, 10);
double? FetchValue2() => Random.Shared.Next(0, 10);
double? FetchValue3() => Random.Shared.Next(0, 10);

var plugins = new[] {
  PluginFactory.CreatePlugin(
    name: "fields",
    fields: new[] {
      PluginFactory.CreateField(label: "value1", graphStyle: PluginFieldGraphStyle.LineStack, fetchValue: FetchValue1),
      PluginFactory.CreateField(label: "value2", graphStyle: PluginFieldGraphStyle.LineStack, fetchValue: FetchValue2),
      PluginFactory.CreateField(label: "value3", graphStyle: PluginFieldGraphStyle.LineWidth2, fetchValue: FetchValue3),
    },
    graphAttributes: new PluginGraphAttributes(
      category: "system",
      title: "random numbers",
      verticalLabel: "number",
      scale: false,
      arguments: "--base 1000 --lower-limit 0 --upper-limit 10"
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
