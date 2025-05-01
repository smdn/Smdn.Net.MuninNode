using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Smdn.Net.MuninNode;
using Smdn.Net.MuninNode.DependencyInjection;
using Smdn.Net.MuninNode.Hosting;
using Smdn.Net.MuninPlugin;

// A interface/class that gets the measurements from some kind of sensor.
interface ITemperatureSensor {
  double GetMeasurementValue();
}

class TemperatureSensor : ITemperatureSensor {
  public double GetMeasurementValue() => Random.Shared.Next(20, 30); // 20~30 degree Celsius
}

class Program {
  static async Task Main(string[] args)
  {
    var builder = Host.CreateApplicationBuilder(args);

    builder
      .Services
        // Add sensor devices.
        .AddKeyedSingleton<ITemperatureSensor>("sensor1", new TemperatureSensor())
        .AddKeyedSingleton<ITemperatureSensor>("sensor2", new TemperatureSensor())
        // Add a Munin-Node background service.
        .AddHostedMuninNodeService(
          // Configure the Munin-Node options.
          options => {
            options.HostName = "munin-node.localhost";
            options.UseAnyAddress(port: 14949);
            options.AllowFromLoopbackOnly();
          },
          // Build the Munin-Node.
          nodeBuilder => {
            // Create and add a Munin-Plugin to report measurements via the Munin-Node.
            nodeBuilder.AddPlugin(
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
                graphAttributes: new PluginGraphAttributes(
                  category: "sensors",
                  title: "Temperature",
                  verticalLabel: "Degree Celsius",
                  scale: false,
                  arguments: "--base 1000 --lower-limit 0 --upper-limit 50"
                )
              )
            );
            // One or more plug-ins can be added.
            // nodeBuilder.AddPlugin(...);
            // nodeBuilder.AddPlugin(...);
          }
        )
        // Add other services.
        .AddLogging(builder => builder
          .AddSimpleConsole(static options => options.SingleLine = true)
          .AddFilter(static level => LogLevel.Trace <= level)
        );

    // Build and run app.
    using var app = builder.Build();

    await app.RunAsync();
  }
}