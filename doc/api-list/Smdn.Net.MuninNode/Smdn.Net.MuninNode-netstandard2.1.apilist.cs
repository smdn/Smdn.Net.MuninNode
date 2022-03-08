// Smdn.Net.MuninNode.dll (Smdn.Net.MuninNode-1.0.0-beta4)
//   Name: Smdn.Net.MuninNode
//   AssemblyVersion: 1.0.0.0
//   InformationalVersion: 1.0.0-beta4+74ac45a2d17aefc7af0d7f2108fee3bf66ef7192
//   TargetFramework: .NETStandard,Version=v2.1
//   Configuration: Release

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Smdn.Net.MuninPlugin;

namespace Smdn.Net.MuninNode {
  public class LocalNode : IDisposable {
    public LocalNode(IReadOnlyList<Plugin> plugins, string hostName, TimeSpan timeout, int portNumber, Version nodeVersion = null, IServiceProvider serviceProvider = null) {}

    public string HostName { get; }
    public IPEndPoint LocalEndPoint { get; }
    public IReadOnlyList<Plugin> Plugins { get; }
    public TimeSpan Timeout { get; }

    [AsyncStateMachine]
    public Task AcceptClientAsync() {}
    public void Close() {}
    protected virtual void Dispose(bool disposing) {}
    public void Dispose() {}
    public void Start() {}
  }
}

namespace Smdn.Net.MuninPlugin {
  public class Plugin {
    public Plugin(string name, PluginGraphConfiguration graphConfiguration, PluginFieldConfiguration fieldConfiguration) {}

    public PluginFieldConfiguration FieldConfiguration { get; }
    public PluginGraphConfiguration GraphConfiguration { get; }
    public string Name { get; }
  }

  public abstract class PluginFieldConfiguration {
    protected PluginFieldConfiguration(string defaultGraphStyle, Range? warningValueRange = null, Range? criticalValueRange = null) {}

    public Range? CriticalValueRange { get; }
    public string DefaultGraphStyle { get; }
    public Range? WarningValueRange { get; }

    public abstract IEnumerable<PluginField> FetchFields();
  }

  public class PluginGraphConfiguration {
    public PluginGraphConfiguration(string title, string category, string verticalLabel, bool scale, string arguments, TimeSpan updateRate, int? width = null, int? height = null) {}

    public string Arguments { get; }
    public string Category { get; }
    public int? Height { get; }
    public bool Scale { get; }
    public string Title { get; }
    public TimeSpan UpdateRate { get; }
    public string VerticalLabel { get; }
    public int? Width { get; }
  }

  public readonly struct PluginField {
    public PluginField(string label, double @value, string graphStyle = null) {}
    public PluginField(string name, string label, double @value, string graphStyle = null) {}

    public string GraphStyle { get; }
    public string Label { get; }
    public string Name { get; }
    public double? Value { get; }

    public static PluginField CreateUnknownValueField(string label, string graphStyle = null) {}
    public static PluginField CreateUnknownValueField(string name, string label, string graphStyle = null) {}
  }
}

