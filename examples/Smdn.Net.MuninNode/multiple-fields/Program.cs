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
    graphAttributes: new PluginGraphAttributesBuilder(title: "random numbers")
      .WithCategory(WellKnownCategory.System)
      .WithVerticalLabel("number")
      .WithGraphLimit(0, 10)
      .Build()
  ),
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
