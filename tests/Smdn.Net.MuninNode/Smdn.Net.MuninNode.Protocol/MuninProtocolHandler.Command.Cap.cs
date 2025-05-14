// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;

using NUnit.Framework;

namespace Smdn.Net.MuninNode.Protocol;

#pragma warning disable IDE0040
partial class MuninProtocolHandlerTests {
#pragma warning restore IDE0040
  [Test]
  public void HandleCommandAsync_CapCommand()
  {
    var handler = new MuninProtocolHandler(
      profile: new MuninNodeProfile()
    );
    var client = new PseudoMuninNodeClient();

    Assert.That(
      async () => await handler.HandleCommandAsync(
        client,
        commandLine: CreateCommandLineSequence("cap")
      ),
      Throws.Nothing
    );

    Assert.That(client.Responses.Count, Is.EqualTo(1));
    Assert.That(
      client.Responses[0],
      Is.EqualTo("cap\n")
    );
  }

  [TestCase("cap x-cap1")]
  [TestCase("cap x-cap1 x-cap2")]
  [TestCase("cap x-cap1 x-cap2 x-cap3")]
  public void HandleCommandAsync_CapCommand_UnsupportedCapabilities(string capCommandLine)
  {
    var handler = new MuninProtocolHandler(
      profile: new MuninNodeProfile()
    );
    var client = new PseudoMuninNodeClient();

    Assert.That(
      async () => await handler.HandleCommandAsync(
        client,
        commandLine: CreateCommandLineSequence(capCommandLine)
      ),
      Throws.Nothing
    );

    Assert.That(client.Responses.Count, Is.EqualTo(1));
    Assert.That(
      client.Responses[0],
      Is.EqualTo("cap\n")
    );
  }

  private class DirtyConfigMuninProtocolHandler(IMuninNodeProfile profile) : MuninProtocolHandler(profile) {
    public new bool IsDirtyConfigEnabled => base.IsDirtyConfigEnabled;
  }

  [TestCase("cap dirtyconfig")]
  [TestCase("cap  dirtyconfig")]
  [TestCase("cap dirtyconfig ")]
  [TestCase("cap dirtyconfig x-cap")]
  [TestCase("cap dirtyconfig  x-cap")]
  [TestCase("cap x-cap dirtyconfig")]
  [TestCase("cap x-cap  dirtyconfig")]
  public void HandleCommandAsync_CapCommand_DirtyConfig(string capCommandLine)
  {
    var handler = new DirtyConfigMuninProtocolHandler(
      profile: new MuninNodeProfile()
    );
    var client = new PseudoMuninNodeClient();

    Assert.That(handler.IsDirtyConfigEnabled, Is.False);

    Assert.That(
      async () => await handler.HandleCommandAsync(
        client,
        commandLine: CreateCommandLineSequence(capCommandLine)
      ),
      Throws.Nothing
    );

    Assert.That(client.Responses.Count, Is.EqualTo(1));
    Assert.That(
      client.Responses[0],
      Is.EqualTo("cap dirtyconfig\n")
    );

    Assert.That(handler.IsDirtyConfigEnabled, Is.True);
  }
}
