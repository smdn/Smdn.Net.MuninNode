// Smdn.Net.MuninNode.dll (Smdn.Net.MuninNode-1.3.0)
//   Name: Smdn.Net.MuninNode
//   AssemblyVersion: 1.3.0.0
//   InformationalVersion: 1.3.0+191d215fe57392cb544e2ffea221644a1007cfc0
//   TargetFramework: .NETCoreApp,Version=v8.0
//   Configuration: Release
//   Referenced assemblies:
//     Microsoft.Extensions.DependencyInjection.Abstractions, Version=6.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
//     Microsoft.Extensions.Logging.Abstractions, Version=6.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
//     Smdn.Fundamental.Exception, Version=3.0.0.0, Culture=neutral
//     System.Collections, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.ComponentModel, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.IO.Pipelines, Version=6.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
//     System.Linq, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Memory, Version=8.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
//     System.Net.Primitives, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Net.Sockets, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Runtime, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Security.Cryptography, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Text.RegularExpressions, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
#nullable enable annotations

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Smdn.Net.MuninNode;
using Smdn.Net.MuninPlugin;

namespace Smdn.Net.MuninNode {
  public class LocalNode : NodeBase {
    public LocalNode(IPluginProvider pluginProvider, string hostName, int port, ILogger? logger = null) {}
    public LocalNode(IPluginProvider pluginProvider, string hostName, int port, IServiceProvider? serviceProvider = null) {}
    public LocalNode(IReadOnlyCollection<IPlugin> plugins, int port, IServiceProvider? serviceProvider = null) {}
    public LocalNode(IReadOnlyCollection<IPlugin> plugins, string hostName, int port, IServiceProvider? serviceProvider = null) {}

    public IPEndPoint LocalEndPoint { get; }

    protected override Socket CreateServerSocket() {}
    protected override bool IsClientAcceptable(IPEndPoint remoteEndPoint) {}
  }

  public abstract class NodeBase :
    IAsyncDisposable,
    IDisposable
  {
    private protected class PluginProvider : IPluginProvider {
      public PluginProvider(IReadOnlyCollection<IPlugin> plugins) {}

      public IReadOnlyCollection<IPlugin> Plugins { get; }
      public INodeSessionCallback? SessionCallback { get; }
    }

    protected NodeBase(IPluginProvider pluginProvider, string hostName, ILogger? logger) {}
    protected NodeBase(IReadOnlyCollection<IPlugin> plugins, string hostName, ILogger? logger) {}

    public virtual Encoding Encoding { get; }
    public string HostName { get; }
    protected ILogger? Logger { get; }
    public virtual Version NodeVersion { get; }
    [Obsolete("This member will be deprecated in future version.")]
    public IReadOnlyCollection<IPlugin> Plugins { get; }

    public async ValueTask AcceptAsync(bool throwIfCancellationRequested, CancellationToken cancellationToken) {}
    public async ValueTask AcceptSingleSessionAsync(CancellationToken cancellationToken = default) {}
    protected abstract Socket CreateServerSocket();
    protected virtual void Dispose(bool disposing) {}
    public void Dispose() {}
    public async ValueTask DisposeAsync() {}
    protected virtual async ValueTask DisposeAsyncCore() {}
    protected abstract bool IsClientAcceptable(IPEndPoint remoteEndPoint);
    public void Start() {}
  }
}

namespace Smdn.Net.MuninPlugin {
  public interface INodeSessionCallback {
    ValueTask ReportSessionClosedAsync(string sessionId, CancellationToken cancellationToken);
    ValueTask ReportSessionStartedAsync(string sessionId, CancellationToken cancellationToken);
  }

  public interface IPlugin {
    IPluginDataSource DataSource { get; }
    PluginGraphAttributes GraphAttributes { get; }
    string Name { get; }
    INodeSessionCallback? SessionCallback { get; }
  }

  public interface IPluginDataSource {
    IReadOnlyCollection<IPluginField> Fields { get; }
  }

  public interface IPluginField {
    PluginFieldAttributes Attributes { get; }
    string Name { get; }

    ValueTask<string> GetFormattedValueStringAsync(CancellationToken cancellationToken);
  }

  public interface IPluginProvider {
    IReadOnlyCollection<IPlugin> Plugins { get; }
    INodeSessionCallback? SessionCallback { get; }
  }

  public enum PluginFieldGraphStyle : int {
    Area = 1,
    AreaStack = 3,
    Default = 0,
    Line = 100,
    LineStack = 200,
    LineStackWidth1 = 201,
    LineStackWidth2 = 202,
    LineStackWidth3 = 203,
    LineWidth1 = 101,
    LineWidth2 = 102,
    LineWidth3 = 103,
    Stack = 2,
  }

  public class Plugin :
    INodeSessionCallback,
    IPlugin,
    IPluginDataSource
  {
    public Plugin(string name, PluginGraphAttributes graphAttributes, IReadOnlyCollection<IPluginField> fields) {}

    public IReadOnlyCollection<IPluginField> Fields { get; }
    public PluginGraphAttributes GraphAttributes { get; }
    public string Name { get; }
    IPluginDataSource IPlugin.DataSource { get; }
    INodeSessionCallback? IPlugin.SessionCallback { get; }
    IReadOnlyCollection<IPluginField> IPluginDataSource.Fields { get; }

    protected virtual ValueTask ReportSessionClosedAsync(string sessionId, CancellationToken cancellationToken) {}
    protected virtual ValueTask ReportSessionStartedAsync(string sessionId, CancellationToken cancellationToken) {}
    ValueTask INodeSessionCallback.ReportSessionClosedAsync(string sessionId, CancellationToken cancellationToken) {}
    ValueTask INodeSessionCallback.ReportSessionStartedAsync(string sessionId, CancellationToken cancellationToken) {}
  }

  public static class PluginFactory {
    public static IPluginField CreateField(string label, Func<double?> fetchValue) {}
    public static IPluginField CreateField(string label, PluginFieldGraphStyle graphStyle, Func<double?> fetchValue) {}
    public static IPluginField CreateField(string label, PluginFieldGraphStyle graphStyle, PluginFieldNormalValueRange normalRangeForWarning, PluginFieldNormalValueRange normalRangeForCritical, Func<double?> fetchValue) {}
    public static IPluginField CreateField(string name, string label, PluginFieldGraphStyle graphStyle, PluginFieldNormalValueRange normalRangeForWarning, PluginFieldNormalValueRange normalRangeForCritical, Func<double?> fetchValue) {}
    public static IPluginField CreateField(string name, string label, PluginFieldGraphStyle graphStyle, PluginFieldNormalValueRange normalRangeForWarning, PluginFieldNormalValueRange normalRangeForCritical, string? negativeFieldName, Func<double?> fetchValue) {}
    public static IPlugin CreatePlugin(string name, PluginGraphAttributes graphAttributes, IReadOnlyCollection<IPluginField> fields) {}
    public static IPlugin CreatePlugin(string name, PluginGraphAttributes graphAttributes, IReadOnlyCollection<PluginFieldBase> fields) {}
    public static IPlugin CreatePlugin(string name, PluginGraphAttributes graphAttributes, PluginFieldBase field) {}
    public static IPlugin CreatePlugin(string name, string fieldLabel, Func<double?> fetchFieldValue, PluginGraphAttributes graphAttributes) {}
    public static IPlugin CreatePlugin(string name, string fieldLabel, PluginFieldGraphStyle fieldGraphStyle, Func<double?> fetchFieldValue, PluginGraphAttributes graphAttributes) {}
  }

  public abstract class PluginFieldBase : IPluginField {
    protected PluginFieldBase(string label, string? name, PluginFieldGraphStyle graphStyle = PluginFieldGraphStyle.Default, PluginFieldNormalValueRange normalRangeForWarning = default, PluginFieldNormalValueRange normalRangeForCritical = default) {}
    protected PluginFieldBase(string label, string? name, PluginFieldGraphStyle graphStyle, PluginFieldNormalValueRange normalRangeForWarning, PluginFieldNormalValueRange normalRangeForCritical, string? negativeFieldName) {}

    public PluginFieldGraphStyle GraphStyle { get; }
    public string Label { get; }
    public string Name { get; }
    public string? NegativeFieldName { get; }
    public PluginFieldNormalValueRange NormalRangeForCritical { get; }
    public PluginFieldNormalValueRange NormalRangeForWarning { get; }
    PluginFieldAttributes IPluginField.Attributes { get; }

    protected abstract ValueTask<double?> FetchValueAsync(CancellationToken cancellationToken);
    async ValueTask<string> IPluginField.GetFormattedValueStringAsync(CancellationToken cancellationToken) {}
  }

  public sealed class PluginGraphAttributes {
    [Obsolete("This member will be deprecated in future version.")]
    public PluginGraphAttributes(string title, string category, string verticalLabel, bool scale, string arguments, TimeSpan updateRate, int? width = null, int? height = null) {}
    public PluginGraphAttributes(string title, string category, string verticalLabel, bool scale, string arguments, TimeSpan? updateRate = null, int? width = null, int? height = null) {}
    public PluginGraphAttributes(string title, string category, string verticalLabel, bool scale, string arguments, TimeSpan? updateRate, int? width, int? height, IEnumerable<string>? order) {}

    public string Arguments { get; }
    public string Category { get; }
    public int? Height { get; }
    public string? Order { get; }
    public bool Scale { get; }
    public string Title { get; }
    public TimeSpan? UpdateRate { get; }
    public string VerticalLabel { get; }
    public int? Width { get; }
  }

  public readonly struct PluginFieldAttributes {
    public PluginFieldAttributes(string label, PluginFieldGraphStyle graphStyle = PluginFieldGraphStyle.Default) {}
    public PluginFieldAttributes(string label, PluginFieldGraphStyle graphStyle = PluginFieldGraphStyle.Default, PluginFieldNormalValueRange normalRangeForWarning = default, PluginFieldNormalValueRange normalRangeForCritical = default) {}
    public PluginFieldAttributes(string label, PluginFieldGraphStyle graphStyle, PluginFieldNormalValueRange normalRangeForWarning, PluginFieldNormalValueRange normalRangeForCritical, string? negativeFieldName) {}

    public PluginFieldGraphStyle GraphStyle { get; }
    public string Label { get; }
    public string? NegativeFieldName { get; }
    public PluginFieldNormalValueRange NormalRangeForCritical { get; }
    public PluginFieldNormalValueRange NormalRangeForWarning { get; }
  }

  public readonly struct PluginFieldNormalValueRange {
    public static readonly PluginFieldNormalValueRange None; // = "Smdn.Net.MuninPlugin.PluginFieldNormalValueRange"

    public static PluginFieldNormalValueRange CreateMax(double max) {}
    public static PluginFieldNormalValueRange CreateMin(double min) {}
    public static PluginFieldNormalValueRange CreateRange(double min, double max) {}

    public bool HasValue { get; }
    public double? Max { get; }
    public double? Min { get; }
  }
}
// API list generated by Smdn.Reflection.ReverseGenerating.ListApi.MSBuild.Tasks v1.4.1.0.
// Smdn.Reflection.ReverseGenerating.ListApi.Core v1.3.1.0 (https://github.com/smdn/Smdn.Reflection.ReverseGenerating)
