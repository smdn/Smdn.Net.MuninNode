// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net;

using NUnit.Framework;

namespace Smdn.Net.MuninNode;

[TestFixture]
public class MuninNodeOptionsTests {
  private static System.Collections.IEnumerable YieldTestCases_ValidPorts()
  {
    yield return 0;
    yield return 1;
    yield return 65535;
  }

  private static System.Collections.IEnumerable YieldTestCases_InvalidPorts()
  {
    yield return -1;
    yield return 65536;
  }

  [Test]
  public void Address()
  {
    var options = new MuninNodeOptions();

    Assert.That(() => options.Address = IPAddress.Any, Throws.Nothing);
    Assert.That(options.Address, Is.EqualTo(IPAddress.Any));
  }

  [Test]
  public void Address_ArgumentNull()
  {
    var options = new MuninNodeOptions();

    Assert.That(
      () => options.Address = null!,
      Throws.ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName)).EqualTo(nameof(options.Address))
    );
  }

  [TestCaseSource(nameof(YieldTestCases_ValidPorts))]
  public void Port(int port)
  {
    var options = new MuninNodeOptions();

    Assert.That(() => options.Port = port, Throws.Nothing);
    Assert.That(options.Port, Is.EqualTo(port));
  }

  [TestCaseSource(nameof(YieldTestCases_InvalidPorts))]
  public void Port_ArgumentOutOfRange(int port)
  {
    var options = new MuninNodeOptions();

    Assert.That(
      () => options.Port = port,
      Throws
        .TypeOf<ArgumentOutOfRangeException>()
        .With
        .Property(nameof(ArgumentOutOfRangeException.ParamName))
        .EqualTo(nameof(options.Port))
        .And
        .Property(nameof(ArgumentOutOfRangeException.ActualValue))
        .EqualTo(port)
    );
  }

  [TestCase("_")]
  [TestCase("munin-node.localhost")]
  public void HostName(string hostName)
  {
    var options = new MuninNodeOptions();

    Assert.That(() => options.HostName = hostName, Throws.Nothing);
    Assert.That(options.HostName, Is.EqualTo(hostName));
  }

  [Test]
  public void HostName_ArgumentNull()
  {
    var options = new MuninNodeOptions();

    Assert.That(
      () => options.HostName = null!,
      Throws.ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName)).EqualTo(nameof(options.HostName))
    );
  }

  [Test]
  public void HostName_ArgumentEmpty()
  {
    var options = new MuninNodeOptions();

    Assert.That(
      () => options.HostName = "",
      Throws.ArgumentException.With.Property(nameof(ArgumentException.ParamName)).EqualTo(nameof(options.HostName))
    );
  }

  [Test]
  public void UseAnyAddress()
  {
    var options = new MuninNodeOptions();

    Assert.That(
      () => Assert.That(options.UseAnyAddress(), Is.SameAs(options)),
      Throws.Nothing
    );
    Assert.That(
      options.Address,
      Is.SameAs(IPAddress.Any).Or.SameAs(IPAddress.IPv6Any)
    );
  }

  [TestCaseSource(nameof(YieldTestCases_ValidPorts))]
  public void UseAnyAddress_WithPort(int port)
  {
    var options = new MuninNodeOptions();

    Assert.That(
      () => Assert.That(options.UseAnyAddress(port), Is.SameAs(options)),
      Throws.Nothing
    );
    Assert.That(
      options.Address,
      Is.SameAs(IPAddress.Any).Or.SameAs(IPAddress.IPv6Any)
    );
    Assert.That(
      options.Port,
      Is.EqualTo(port)
    );
  }

  [TestCaseSource(nameof(YieldTestCases_InvalidPorts))]
  public void UseAnyAddress_WithPort_InvalidPort(int port)
  {
    var initialAddress = IPAddress.Parse("192.0.2.0");
    var options = new MuninNodeOptions() {
      Address = initialAddress
    };

    Assert.That(
      () => options.UseAnyAddress(port: port),
      Throws
        .TypeOf<ArgumentOutOfRangeException>()
        .With
        .Property(nameof(ArgumentOutOfRangeException.ParamName))
        .EqualTo("port")
        .And
        .Property(nameof(ArgumentOutOfRangeException.ActualValue))
        .EqualTo(port)
    );

    Assert.That(options.Address, Is.SameAs(initialAddress));
  }

  [Test]
  public void UseLoopbackAddress()
  {
    var options = new MuninNodeOptions();

    Assert.That(
      () => Assert.That(options.UseLoopbackAddress(), Is.SameAs(options)),
      Throws.Nothing
    );
    Assert.That(
      options.Address,
      Is.SameAs(IPAddress.Loopback).Or.SameAs(IPAddress.IPv6Loopback)
    );
  }

  [TestCaseSource(nameof(YieldTestCases_ValidPorts))]
  public void UseLoopbackAddress_WithPort(int port)
  {
    var options = new MuninNodeOptions();

    Assert.That(
      () => Assert.That(options.UseLoopbackAddress(port), Is.SameAs(options)),
      Throws.Nothing
    );
    Assert.That(
      options.Address,
      Is.SameAs(IPAddress.Loopback).Or.SameAs(IPAddress.IPv6Loopback)
    );
    Assert.That(
      options.Port,
      Is.EqualTo(port)
    );
  }

  [TestCaseSource(nameof(YieldTestCases_InvalidPorts))]
  public void UseLoopbackAddress_WithPort_InvalidPort(int port)
  {
    var initialAddress = IPAddress.Parse("192.0.2.0");
    var options = new MuninNodeOptions() {
      Address = initialAddress
    };

    Assert.That(
      () => options.UseLoopbackAddress(port: port),
      Throws
        .TypeOf<ArgumentOutOfRangeException>()
        .With
        .Property(nameof(ArgumentOutOfRangeException.ParamName))
        .EqualTo("port")
        .And
        .Property(nameof(ArgumentOutOfRangeException.ActualValue))
        .EqualTo(port)
    );

    Assert.That(options.Address, Is.SameAs(initialAddress));
  }

  [Test]
  public void AllowFrom()
  {
    var options = new MuninNodeOptions() {
      AccessRule = null
    };
    var addresses = new[] {
      IPAddress.Parse("192.0.2.0"),
      IPAddress.Parse("192.0.2.1"),
      IPAddress.Parse("192.0.2.2")
    };

    Assert.That(
      () => Assert.That(options.AllowFrom(addresses: addresses), Is.SameAs(options)),
      Throws.Nothing
    );

    Assert.That(options.AccessRule, Is.Not.Null);

    using var scope = Assert.EnterMultipleScope();

    foreach (var port in new[] { 0, 1, 6553 }) {
      foreach (var address in addresses) {
        var endPoint = new IPEndPoint(address, port);

        Assert.That(options.AccessRule.IsAcceptable(endPoint), Is.True);
      }

      foreach (var address in new[] { IPAddress.Loopback, IPAddress.IPv6Loopback }) {
        var endPoint = new IPEndPoint(address, port);

        Assert.That(options.AccessRule.IsAcceptable(endPoint), Is.False);
      }
    }
  }

  [Test]
  public void AllowFrom_ShouldConsiderIPv4MappedIPv6Address([Values] bool shouldConsiderIPv4MappedIPv6Address)
  {
    var options = new MuninNodeOptions() {
      AccessRule = null
    };
    var ipv4Address = IPAddress.Parse("192.0.2.0");
    var addresses = new[] {
      ipv4Address,
    };

    Assert.That(
      () => Assert.That(
        options.AllowFrom(
          addresses: addresses,
          shouldConsiderIPv4MappedIPv6Address: shouldConsiderIPv4MappedIPv6Address
        ),
        Is.SameAs(options)
      ),
      Throws.Nothing
    );

    Assert.That(options.AccessRule, Is.Not.Null);

    Assert.That(
      options.AccessRule.IsAcceptable(new IPEndPoint(ipv4Address, 0)),
      Is.True
    );
    Assert.That(
      options.AccessRule.IsAcceptable(new IPEndPoint(ipv4Address.MapToIPv6(), 0)),
      Is.EqualTo(shouldConsiderIPv4MappedIPv6Address)
    );
  }

  [Test]
  public void AllowFrom_ArgumentNull()
  {
    var options = new MuninNodeOptions() {
      AccessRule = null
    };

    Assert.That(
      () => Assert.That(options.AllowFrom(addresses: null!), Is.SameAs(options)),
      Throws
        .ArgumentNullException
        .With
        .Property(nameof(ArgumentNullException.ParamName))
        .EqualTo("addresses")
    );

    Assert.That(options.AccessRule, Is.Null);
  }

  [Test]
  public void AllowFromLoopbackOnly()
  {
    var options = new MuninNodeOptions() {
      AccessRule = null
    };

    Assert.That(
      () => Assert.That(options.AllowFromLoopbackOnly(), Is.SameAs(options)),
      Throws.Nothing
    );

    Assert.That(options.AccessRule, Is.Not.Null);

    Assert.That(options.AccessRule.IsAcceptable(new IPEndPoint(IPAddress.Loopback, 0)), Is.True);
    Assert.That(options.AccessRule.IsAcceptable(new IPEndPoint(IPAddress.Loopback.MapToIPv6(), 0)), Is.True);
    Assert.That(options.AccessRule.IsAcceptable(new IPEndPoint(IPAddress.IPv6Loopback, 0)), Is.True);
    Assert.That(options.AccessRule.IsAcceptable(new IPEndPoint(IPAddress.Any, 0)), Is.False);
    Assert.That(options.AccessRule.IsAcceptable(new IPEndPoint(IPAddress.IPv6Any, 0)), Is.False);
  }
}
