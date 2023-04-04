// SPDX-FileCopyrightText: 2021 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Smdn.Net.MuninPlugin;

[TestFixture]
public class PluginFieldBaseTests {
  private class ConcretePluginField : PluginFieldBase {
    public ConcretePluginField(string label)
      : base(name: null, label: label)
    {
    }

    public ConcretePluginField(string label, string name)
      : base(label: label, name: name)
    {
    }

    protected override ValueTask<double?> FetchValueAsync(CancellationToken cancellationToken)
      => throw new NotImplementedException();
  }

  [TestCase("AZaz09_")]
  [TestCase("_NAME")]
  [TestCase("_name")]
  [TestCase("xNAME")]
  [TestCase("XNAME")]
  [TestCase("_0")]
  [TestCase("F0")]
  [TestCase("f0")]
  public void Name_Valid(string name)
    => Assert.DoesNotThrow(() => new ConcretePluginField(name: name, label: "label"));

  [TestCase("")]
  [TestCase("0")]
  [TestCase("0123456789")]
  [TestCase("9")]
  [TestCase("9abc")]
  [TestCase("あ")]
  public void Name_Invalid(string name)
    => Assert.Throws<ArgumentException>(() => new ConcretePluginField(name: name, label: "label"));

  [TestCase("LABEL")]
  [TestCase("label")]
  [TestCase("ラベル")]
  [TestCase("<$Label>")]
  [TestCase("_LABEL")]
  [TestCase("LABEL_")]
  [TestCase("0LABEL")]
  [TestCase("LABEL0")]
  public void Lavel_Valid(string label)
    => Assert.DoesNotThrow(() => new ConcretePluginField(name: "field", label: label));

  [TestCase("")]
  [TestCase("\\LABEL")]
  [TestCase("LABEL\\")]
  [TestCase("#LABEL")]
  [TestCase("LABEL#")]
  public void Lavel_Invalid(string label)
    => Assert.Throws<ArgumentException>(() => new ConcretePluginField(name: "field", label: label));

  [TestCase("LABEL", "LABEL")]
  [TestCase("_LABEL", "LABEL")]
  [TestCase("LABEL_", "LABEL_")]
  [TestCase("0LABEL", "LABEL")]
  [TestCase("09LABEL", "LABEL")]
  [TestCase("LABEL0", "LABEL0")]
  [TestCase("LABEL09", "LABEL09")]
  [TestCase("<$Label>", "Label")]
  [TestCase("ラベルX", "X")]
  public void DefaultNameFromLabel_Valid(string label, string expectedDefaultName)
  {
    ConcretePluginField? f = default;

    Assert.DoesNotThrow(() => f = new ConcretePluginField(label: label));

    Assert.AreEqual(expectedDefaultName, f!.Name, nameof(f.Name));
  }

  [TestCase("")]
  [TestCase("ラベル")]
  [TestCase("0ラベル")]
  [TestCase("ラベル0")]
  [TestCase("\\LABEL")]
  [TestCase("LABEL\\")]
  [TestCase("#LABEL")]
  [TestCase("LABEL#")]
  public void DefaultNameFromLabel_Invalid(string label)
    => Assert.Throws<ArgumentException>(() => new ConcretePluginField(label: label));
}
