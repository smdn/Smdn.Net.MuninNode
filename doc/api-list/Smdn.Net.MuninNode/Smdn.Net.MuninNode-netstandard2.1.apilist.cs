// Smdn.Net.MuninNode.dll (Smdn.Net.MuninNode-1.0.0-beta5)
//   Name: Smdn.Net.MuninNode
//   AssemblyVersion: 1.0.0.0
//   InformationalVersion: 1.0.0-beta5+9c35db93d2d4c37becbd2bed7ecdb74bc254a9ad
//   TargetFramework: .NETStandard,Version=v2.1
//   Configuration: Release
//   Referenced assemblies:
//     Microsoft.Extensions.DependencyInjection.Abstractions, Version=6.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
//     Microsoft.Extensions.Logging.Abstractions, Version=6.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
//     Smdn.Fundamental.Encoding.Buffer, Version=3.0.0.0, Culture=neutral
//     Smdn.Fundamental.Exception, Version=3.0.0.0, Culture=neutral
//     System.IO.Pipelines, Version=6.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
//     netstandard, Version=2.1.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
#nullable enable annotations

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Smdn.Net.MuninPlugin;

namespace Smdn.Net.MuninNode {
  public class LocalNode : IDisposable {
    public LocalNode(IReadOnlyList<Plugin> plugins, string hostName, TimeSpan timeout, int portNumber, Version? nodeVersion = null, IServiceProvider? serviceProvider = null) {}

    public string HostName { get; }
    public IPEndPoint LocalEndPoint { get; }
    public IReadOnlyList<Plugin> Plugins { get; }
    public TimeSpan Timeout { get; }

    public async Task AcceptClientAsync() {}
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
    public static PluginField CreateUnknownValueField(string label, string? graphStyle = null) {}
    public static PluginField CreateUnknownValueField(string name, string label, string? graphStyle = null) {}

    public PluginField(string label, double @value, string? graphStyle = null) {}
    public PluginField(string name, string label, double @value, string? graphStyle = null) {}

    public string? GraphStyle { get; }
    public string Label { get; }
    public string Name { get; }
    public double? Value { get; }
  }
}
// API list generated by Smdn.Reflection.ReverseGenerating.ListApi.MSBuild.Tasks v1.2.1.0.
// Smdn.Reflection.ReverseGenerating.ListApi.Core v1.2.0.0 (https://github.com/smdn/Smdn.Reflection.ReverseGenerating)
