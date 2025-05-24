[![GitHub license](https://img.shields.io/github/license/smdn/Smdn.Net.MuninNode)](https://github.com/smdn/Smdn.Net.MuninNode/blob/main/LICENSE.txt)
[![GitHub issues](https://img.shields.io/github/issues/smdn/Smdn.Net.MuninNode)](https://github.com/smdn/Smdn.Net.MuninNode/issues)
[![tests/main](https://img.shields.io/github/actions/workflow/status/smdn/Smdn.Net.MuninNode/test.yml?branch=main&label=tests%2Fmain)](https://github.com/smdn/Smdn.Net.MuninNode/actions/workflows/test.yml)
[![CodeQL](https://github.com/smdn/Smdn.Net.MuninNode/actions/workflows/codeql-analysis.yml/badge.svg?branch=main)](https://github.com/smdn/Smdn.Net.MuninNode/actions/workflows/codeql-analysis.yml)
[![NuGet Smdn.Net.MuninNode](https://img.shields.io/nuget/v/Smdn.Net.MuninNode.svg)](https://www.nuget.org/packages/Smdn.Net.MuninNode/)

# Smdn.Net.MuninNode
[![NuGet Smdn.Net.MuninNode](https://img.shields.io/nuget/v/Smdn.Net.MuninNode.svg)](https://www.nuget.org/packages/Smdn.Net.MuninNode/)

Smdn.Net.MuninNode is a .NET implementation of [Munin-Node](https://guide.munin-monitoring.org/en/latest/node/index.html) and [Munin-Plugin](https://guide.munin-monitoring.org/en/latest/plugin/index.html).

This library provides Munin-Node implementation for .NET, which enables to you to create custom Munin-Node using the .NET languages and libraries.

This library also provides abstraction APIs for implementing Munin-Plugin. By using Munin-Plugin APIs in combination with the Munin-Node implementation, you can implement the function of collecting various kind of telemetry data using Munin, with .NET.

### `Smdn.Net.MuninNode` namespace
This library has two major namespaces. In the [Smdn.Net.MuninNode](./src/Smdn.Net.MuninNode/Smdn.Net.MuninNode/) namespace, there is a `NodeBase` class, which provides abstract Munin-Node implementation.

You can use the extension methods from [Smdn.Net.MuninNode.DependencyInjection](./src/Smdn.Net.MuninNode/Smdn.Net.MuninNode.DependencyInjection/) namespace to configure and register the Munin-Node to the `ServiceCollection`. This would support most purposes and use cases. See [this example](./examples/Smdn.Net.MuninNode/getting-started/) for detail.

### `Smdn.Net.MuninPlugin` namespace
In the [Smdn.Net.MuninPlugin](./src/Smdn.Net.MuninNode/Smdn.Net.MuninPlugin/) namespace, there is a `IPlugin` interfaces, which represents the functionality that should be implemented as Munin-Plugin. By properly implementing `IPlugin` and its relevant interfaces, you can compose the Munin-Plugin which aggregates telemetry data using .NET.

# Smdn.Net.MuninNode.Hosting
[![NuGet Smdn.Net.MuninNode.Hosting](https://img.shields.io/nuget/v/Smdn.Net.MuninNode.Hosting.svg)](https://www.nuget.org/packages/Smdn.Net.MuninNode.Hosting/)

This library provides APIs to run Munin-Node as a background service integrated with .NET Generic Host.

If you want to integrate with .NET Generic Host, especially if you want to implement Munin-Node running as a **Windows Services** or **systemd unit**, you can use this extension library. See [this example](./examples/Smdn.Net.MuninNode.Hosting/getting-started/) for detail.

# Usage
To use the released package, add `<PackageReference>` to the project file.

```xml
  <ItemGroup>
    <PackageReference Include="Smdn.Net.MuninNode" Version="2.*" />
    <!-- Or -->
    <PackageReference Include="Smdn.Net.MuninNode.Hosting" Version="3.*" />
  </ItemGroup>
```

Then write the your code. See [examples](examples/) to use APIs.

### Configure Munin master (`munin.conf`)
If you want `munin-update` process to gather the telemetry data from the Munin-Node you have created and started, you have to add entry defines your node to configuration file `/etc/munin/munin.conf`. The following is an example:

```conf
[your-node.localdomain]
    address 127.0.0.1 # address of your node
    port 4949 # port number that your node uses
    use_node_name yes # (optional) let Munin to use the node name advertised by your node
```

Multiple instances can also be started by defining multiple nodes with different port numbers if you want.

For more information about node definitions, please refer to the [Munin documentation for munin.conf](https://guide.munin-monitoring.org/en/latest/reference/munin.conf.html).


### Test the node
To test the node you have created, run the node first, and connect to the node.

The following is an example of testing a node using the `telnet` command. Here, the port number should be the one your node is using.

```
$ telnet 127.0.0.1 4949
Trying 127.0.0.1...
Connected to localhost.
Escape character is '^]'.
# munin node at your-node.localhost
list                                  <-- type `list` to list plugin names
uptime sensor1 sensor2
fetch uptime                          <-- type `fetch <plugin-name>` to fetch
                                          the values of the specified plugin
uptime.value 123.4567
quit                                  <-- type `quit` to close connection
Connection closed by foreign host.
```

# For contributors
Contributions are appreciated!

If there's a feature you would like to add or a bug you would like to fix, please read [Contribution guidelines](./CONTRIBUTING.md) and create an Issue or Pull Request.

IssueやPull Requestを送る際は、[Contribution guidelines](./CONTRIBUTING.md)をご覧頂ください。　可能なら英語が望ましいですが、日本語で構いません。

# PseudoMuninNode
[PseudoMuninNode](https://github.com/smdn/PseudoMuninNode), the C++ implementation for ESP32 (Arduino IDE) is also available.
