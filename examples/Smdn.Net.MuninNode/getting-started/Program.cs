using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Smdn.Net.MuninNode;
using Smdn.Net.MuninNode.DependencyInjection;
using Smdn.Net.MuninPlugin;

// A interface/class that gets the measurements from some kind of sensor.
interface ITemperatureSensor {
  double GetMeasurementValue();
}

class TemperatureSensor : ITemperatureSensor {
  public double GetMeasurementValue() => Random.Shared.Next(20, 30); // 20~30 degree Celsius
}

class Program {
  static async Task Main()
  {
    var services = new ServiceCollection();

    services
      // Add sensor devices.
      .AddKeyedSingleton<ITemperatureSensor>("sensor1", new TemperatureSensor())
      .AddKeyedSingleton<ITemperatureSensor>("sensor2", new TemperatureSensor())
      // Add a Munin service.
      .AddMunin(muninBuilder => muninBuilder
        // Add a Munin-Node and configure the options.
        .AddNode(options => {
          options.HostName = "munin-node.localhost";
          options.UseAnyAddress(port: 14949);
          options.AllowFromLoopbackOnly();
        })
        // Create and add a Munin-Plugin to report measurements via the Munin-Node.
        .AddPlugin(
          serviceProvider => PluginFactory.CreatePlugin(
            name: "temperature",
            // Configure the 'fields' that identify the data source.
            // See: https://guide.munin-monitoring.org/en/latest/reference/plugin.html#data-source-attributes
            fields: [
              PluginFactory.CreateField(
                label: "sensor1",
                fetchValue: () => serviceProvider
                  .GetRequiredKeyedService<ITemperatureSensor>("sensor1")
                  .GetMeasurementValue()
              ),
              PluginFactory.CreateField(
                label: "sensor2",
                fetchValue: () => serviceProvider
                  .GetRequiredKeyedService<ITemperatureSensor>("sensor2")
                  .GetMeasurementValue()
              ),
            ],
            // Configures the 'attributes' of the graph when drawn data as a graph.
            // See: https://guide.munin-monitoring.org/en/latest/reference/plugin.html#global-attributes
            graphAttributes: new PluginGraphAttributesBuilder(title: "Temperature")
              .WithCategory(WellKnownCategory.Sensor)
              .WithVerticalLabel("Degree Celsius")
              .WithGraphLimit(0, 50)
              .Build()
          )
        )
        // One or more plug-ins can be added.
        //.AddPlugin(...)
        //.AddPlugin(...)
      )
      // Add other services.
      .AddLogging(builder => builder
        .AddSimpleConsole(static options => {
          options.SingleLine = true;
          options.IncludeScopes = true;
        })
        .AddFilter(static level => LogLevel.Trace <= level)
      );

    // Keep the node running until Ctrl+C is pressed.
    var cts = new CancellationTokenSource();

    Console.CancelKeyPress += (_, args) => {
      cts.Cancel();
      args.Cancel = true;
    };

    // Build services and run munin-node service.
    var node = services.BuildServiceProvider().GetRequiredService<IMuninNode>();

    try {
      // When the RunAsync method finishes processing one connection,
      // it immediately waits for the next connection.
      // Therefore, the method will not return until the cancellation is
      // requested by CancellationToken.
      await node.RunAsync(cts.Token);
    }
    catch (OperationCanceledException ex) when (ex.CancellationToken == cts.Token) {
      Console.WriteLine("stopped");
    }
  }
}
