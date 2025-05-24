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

  private class CapCommandMuninProtocolHandler(IMuninNodeProfile profile) : MuninProtocolHandler(profile) {
    public new bool IsDirtyConfigEnabled => base.IsDirtyConfigEnabled;
    public new bool IsMultigraphEnabled => base.IsMultigraphEnabled;
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
    var handler = new CapCommandMuninProtocolHandler(
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

  [TestCase("cap multigraph")]
  [TestCase("cap  multigraph")]
  [TestCase("cap multigraph ")]
  [TestCase("cap multigraph x-cap")]
  [TestCase("cap multigraph  x-cap")]
  [TestCase("cap x-cap multigraph")]
  [TestCase("cap x-cap  multigraph")]
  public void HandleCommandAsync_CapCommand_Multigraph(string capCommandLine)
  {
    var handler = new CapCommandMuninProtocolHandler(
      profile: new MuninNodeProfile()
    );
    var client = new PseudoMuninNodeClient();

    Assert.That(handler.IsMultigraphEnabled, Is.False);

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
      Is.EqualTo("cap multigraph\n")
    );

    Assert.That(handler.IsMultigraphEnabled, Is.True);
  }

  [TestCase("cap dirtyconfig multigraph", true, true, "cap dirtyconfig multigraph\n")]
  [TestCase("cap multigraph dirtyconfig", true, true, "cap dirtyconfig multigraph\n")]
  [TestCase("cap x-cap multigraph dirtyconfig", true, true, "cap dirtyconfig multigraph\n")]
  [TestCase("cap multigraph x-cap dirtyconfig", true, true, "cap dirtyconfig multigraph\n")]
  [TestCase("cap multigraph dirtyconfig x-cap", true, true, "cap dirtyconfig multigraph\n")]
  public void HandleCommandAsync_CapCommand_CombinedCapacities(
    string capCommandLine,
    bool expectDirtyConfigToBeEnabled,
    bool expectMultigraphToBeEnabled,
    string expectedResponseLine
  )
  {
    var handler = new CapCommandMuninProtocolHandler(
      profile: new MuninNodeProfile()
    );
    var client = new PseudoMuninNodeClient();

    Assert.That(handler.IsDirtyConfigEnabled, Is.False);
    Assert.That(handler.IsMultigraphEnabled, Is.False);

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
      Is.EqualTo(expectedResponseLine)
    );

    Assert.That(handler.IsDirtyConfigEnabled, Is.EqualTo(expectDirtyConfigToBeEnabled));
    Assert.That(handler.IsMultigraphEnabled, Is.EqualTo(expectMultigraphToBeEnabled));
  }
}
