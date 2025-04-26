// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

using Smdn.Net.MuninNode.AccessRules;

namespace Smdn.Net.MuninNode;

/// <summary>
/// Options to configure the <c>Munin-Node</c>.
/// </summary>
/// <see cref="DependencyInjection.IMuninServiceBuilderExtensions.AddNode(DependencyInjection.IMuninServiceBuilder, Action{MuninNodeOptions})"/>
public sealed class MuninNodeOptions {
  private static IPAddress LoopbackAddress => Socket.OSSupportsIPv6 ? IPAddress.IPv6Loopback : IPAddress.Loopback;
  private static IPAddress AnyAddress => Socket.OSSupportsIPv6 ? IPAddress.IPv6Any : IPAddress.Any;

  private static int ValidatePort(int port, string paramName)
  {
    if (port is < IPEndPoint.MinPort or > IPEndPoint.MaxPort)
      throw new ArgumentOutOfRangeException(paramName: paramName, actualValue: port, $"must be in range of {IPEndPoint.MinPort}~{IPEndPoint.MaxPort}");

    return port;
  }

  /// <value>
  /// <see cref="IPAddress.IPv6Loopback"/> if the operating system supports IPv6, otherwise <see cref="IPAddress.Loopback"/>.
  /// </value>
  public static IPAddress DefaultAddress => LoopbackAddress;

  public const string DefaultHostName = "munin-node.localhost";
  public const int DefaultPort = 4949;

  /// <summary>
  /// Gets or sets a value for the <c>host</c>.
  /// The <see cref="IPAddress"/> on which the <c>Munin-Node</c> listens.
  /// </summary>
  /// <seealso cref="DefaultAddress"/>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/munin-node.conf.html#cmdoption-arg-host">munin-node.conf - DIRECTIVES - Native - host</seealso>
  public IPAddress Address {
    get => address;
    set => address = value ?? throw new ArgumentNullException(nameof(Address));
  }

  private IPAddress address = DefaultAddress;

  /// <summary>
  /// Gets or sets a value for the <c>port</c>.
  /// The TCP port number on which the <c>Munin-Node</c> listens.
  /// </summary>
  /// <remarks>
  /// The default value is <c>4949</c>.
  /// </remarks>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/munin-node.conf.html#cmdoption-arg-port">munin-node.conf - DIRECTIVES - Inherited - port</seealso>
  public int Port {
    get => port;
    set => port = ValidatePort(value, nameof(Port));
  }

  private int port = DefaultPort;

  /// <summary>
  /// Gets or sets a value for the <c>host_name</c>.
  /// The hostname used by <c>Munin-Node</c> to advertise itself to the munin master.
  /// </summary>
  /// <remarks>
  /// The default value is <c>munin-node.localhost</c>.
  /// </remarks>
  /// <value>
  /// The hostname used by <c>Munin-Node</c>. The length of the string must be at least 1.
  /// </value>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/munin-node.conf.html#cmdoption-arg-host-name">munin-node.conf - DIRECTIVES - Native - host_name</seealso>
  public string HostName {
    get => hostName;
    set {
      if (value is null)
        throw new ArgumentNullException(nameof(HostName));
      if (value.Length == 0)
        throw ExceptionUtils.CreateArgumentMustBeNonEmptyString(nameof(HostName));

      hostName = value;
    }
  }

  private string hostName = DefaultHostName;

  /// <summary>
  /// Gets or sets an <see cref="IAccessRule"/> that determines whether to accept or reject a remote host connecting to munin-node.
  /// </summary>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/munin-node.conf.html#cmdoption-arg-allow">munin-node.conf - DIRECTIVES - Inherited - allow</seealso>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/munin-node.conf.html#cmdoption-arg-cidr-allow">munin-node.conf - DIRECTIVES - Inherited - cidr_allow</seealso>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/munin-node.conf.html#cmdoption-arg-cidr-deny">munin-node.conf - DIRECTIVES - Inherited - cidr_deny</seealso>
  public IAccessRule? AccessRule { get; set; }

#if false && TODO
  /// <summary>
  /// Gets or sets a value for the <c>timeout</c>.
  /// The timeout value for each plugin. If plugins take longer to run, this will disconnect the master.
  /// </summary>
  /// <remarks>
  /// The default value is 60 seconds.
  /// </remarks>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/munin-node.conf.html#cmdoption-arg-timeout">munin-node.conf - DIRECTIVES - Native - timeout</seealso>
  public TimeSpan TimeoutForPlugin { get; internal set; } = TimeSpan.FromSeconds(60);

  /// <summary>
  /// Gets or sets a value for the <c>global_timeout</c>.
  /// The timeout value for the entire session from the start to the end of the <c>munin-update</c> process.
  /// </summary>
  /// <remarks>
  /// The default value is 900 seconds.
  /// </remarks>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/munin-node.conf.html#cmdoption-arg-global-timeout">munin-node.conf - DIRECTIVES - Native - global_timeout</seealso>
  /// <seealso cref="Smdn.Net.MuninPlugin.INodeSessionCallback"/>
  public TimeSpan TimeoutForSession { get; internal set; } = TimeSpan.FromSeconds(900);
#endif

  public MuninNodeOptions()
  {
  }

#if false
  public MuninNodeOptions Clone()
    => (MuninNodeOptions)MemberwiseClone();
#endif

  /// <summary>
  /// Set the value of the <see cref="Address"/> property to use the address of
  /// <see cref="IPAddress.Any"/> or <see cref="IPAddress.IPv6Any"/>.
  /// </summary>
  /// <remarks>
  /// If <see cref="Socket.OSSupportsIPv6"/> is <see langword="true"/>, <see cref="IPAddress.IPv6Any"/>
  /// will be used, otherwise <see cref="IPAddress.Any"/> will be used.
  /// </remarks>
  /// <returns>
  /// The current <see cref="MuninNodeOptions"/> so that additional calls can be chained.
  /// </returns>
  public MuninNodeOptions UseAnyAddress()
  {
    Address = AnyAddress;

    return this;
  }

  /// <summary>
  /// Set the value of the <see cref="Address"/> property to use the address of
  /// <see cref="IPAddress.Any"/> or <see cref="IPAddress.IPv6Any"/>.
  /// </summary>
  /// <param name="port">
  /// The port number to be used as the endpoint along with the address.
  /// This value will be set to the value of the <see cref="Port"/> property.
  /// </param>
  /// <remarks>
  /// If <see cref="Socket.OSSupportsIPv6"/> is <see langword="true"/>, <see cref="IPAddress.IPv6Any"/>
  /// will be used, otherwise <see cref="IPAddress.Any"/> will be used.
  /// </remarks>
  /// <returns>
  /// The current <see cref="MuninNodeOptions"/> so that additional calls can be chained.
  /// </returns>
  public MuninNodeOptions UseAnyAddress(int port)
  {
    Port = ValidatePort(port, nameof(port));
    Address = AnyAddress;

    return this;
  }

  /// <summary>
  /// Set the value of the <see cref="Address"/> property to use the loopback address.
  /// </summary>
  /// <remarks>
  /// If <see cref="Socket.OSSupportsIPv6"/> is <see langword="true"/>, <see cref="IPAddress.IPv6Loopback"/>
  /// will be used, otherwise <see cref="IPAddress.Loopback"/> will be used.
  /// </remarks>
  /// <returns>
  /// The current <see cref="MuninNodeOptions"/> so that additional calls can be chained.
  /// </returns>
  public MuninNodeOptions UseLoopbackAddress()
  {
    Address = LoopbackAddress;

    return this;
  }

  /// <summary>
  /// Set the value of the <see cref="Address"/> property to use the loopback address.
  /// </summary>
  /// <param name="port">
  /// The port number to be used as the endpoint along with the address.
  /// This value will be set to the value of the <see cref="Port"/> property.
  /// </param>
  /// <remarks>
  /// If <see cref="Socket.OSSupportsIPv6"/> is <see langword="true"/>, <see cref="IPAddress.IPv6Loopback"/>
  /// will be used, otherwise <see cref="IPAddress.Loopback"/> will be used.
  /// </remarks>
  /// <returns>
  /// The current <see cref="MuninNodeOptions"/> so that additional calls can be chained.
  /// </returns>
  public MuninNodeOptions UseLoopbackAddress(int port)
  {
    Port = ValidatePort(port, nameof(port));
    Address = LoopbackAddress;

    return this;
  }

  /// <summary>
  /// Set the <see cref="AccessRule"/> property to allow access only from the specified address.
  /// </summary>
  /// <param name="addresses">
  /// The <see cref="IReadOnlyList{IPAddress}"/> indicates the read-only list of addresses
  /// allowed to access <c>Munin-Node</c>.
  /// </param>
  /// <param name="shouldConsiderIPv4MappedIPv6Address">
  /// Specifies whether or not to be aware that the IP address to be an IPv4-mapped IPv6 address or not
  /// when comparing IP addresses.
  /// </param>
  /// <returns>
  /// The current <see cref="MuninNodeOptions"/> so that additional calls can be chained.
  /// </returns>
  public MuninNodeOptions AllowFrom(
    IReadOnlyList<IPAddress> addresses,
    bool shouldConsiderIPv4MappedIPv6Address = true
  )
  {
    AccessRule = new AddressListAccessRule(
      addresses ?? throw new ArgumentNullException(nameof(addresses)),
      shouldConsiderIPv4MappedIPv6Address
    );

    return this;
  }

  /// <summary>
  /// Set the <see cref="AccessRule"/> property to allow access only from the loopback address.
  /// </summary>
  /// <returns>
  /// The current <see cref="MuninNodeOptions"/> so that additional calls can be chained.
  /// </returns>
  public MuninNodeOptions AllowFromLoopbackOnly()
  {
    AccessRule = LoopbackOnlyAccessRule.Instance;

    return this;
  }
}
