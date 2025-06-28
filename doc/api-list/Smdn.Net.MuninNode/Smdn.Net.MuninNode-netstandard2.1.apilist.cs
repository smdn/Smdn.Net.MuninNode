// Smdn.Net.MuninNode.dll (Smdn.Net.MuninNode-2.5.0)
//   Name: Smdn.Net.MuninNode
//   AssemblyVersion: 2.5.0.0
//   InformationalVersion: 2.5.0+41ff114bf69b864033a05a389896010d3eefe4d5
//   TargetFramework: .NETStandard,Version=v2.1
//   Configuration: Release
//   Referenced assemblies:
//     Microsoft.Extensions.DependencyInjection.Abstractions, Version=8.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
//     Microsoft.Extensions.Logging.Abstractions, Version=6.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
//     Microsoft.Extensions.Options, Version=8.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
//     Smdn.Fundamental.Encoding.Buffer, Version=3.0.0.0, Culture=neutral
//     Smdn.Fundamental.Exception, Version=3.0.0.0, Culture=neutral
//     System.IO.Pipelines, Version=6.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
//     netstandard, Version=2.1.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
#nullable enable annotations

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Smdn.Net.MuninNode;
using Smdn.Net.MuninNode.DependencyInjection;
using Smdn.Net.MuninNode.Protocol;
using Smdn.Net.MuninNode.Transport;
using Smdn.Net.MuninPlugin;

namespace Smdn.Net.MuninNode {
  public interface IAccessRule {
    bool IsAcceptable(IPEndPoint remoteEndPoint);
  }

  public interface IMuninNode {
    EndPoint EndPoint { get; }
    string HostName { get; }

    Task RunAsync(CancellationToken cancellationToken);
  }

  public static class IAccessRuleServiceCollectionExtensions {
    public static IServiceCollection AddMuninNodeAccessRule(this IServiceCollection services, IAccessRule accessRule) {}
    public static IServiceCollection AddMuninNodeAccessRule(this IServiceCollection services, IReadOnlyList<IPAddress> addressListAllowFrom) {}
    public static IServiceCollection AddMuninNodeAccessRule(this IServiceCollection services, IReadOnlyList<IPAddress> addressListAllowFrom, bool shouldConsiderIPv4MappedIPv6Address) {}
    public static IServiceCollection AddMuninNodeLoopbackOnlyAccessRule(this IServiceCollection services) {}
  }

  public abstract class LocalNode : NodeBase {
    public static LocalNode Create(IPluginProvider pluginProvider, int port, string? hostName = null, IReadOnlyList<IPAddress>? addressListAllowFrom = null, IServiceProvider? serviceProvider = null) {}
    public static LocalNode Create(IReadOnlyCollection<IPlugin> plugins, int port, string? hostName = null, IReadOnlyList<IPAddress>? addressListAllowFrom = null, IServiceProvider? serviceProvider = null) {}

    [Obsolete("Use a constructor overload that takes IMuninNodeListenerFactory as an argument.")]
    protected LocalNode(IAccessRule? accessRule, ILogger? logger = null) {}
    protected LocalNode(IMuninNodeListenerFactory? listenerFactory, IAccessRule? accessRule, ILogger? logger) {}

    [Obsolete("Use IMuninNodeListenerFactory and StartAsync instead.")]
    protected override Socket CreateServerSocket() {}
  }

  public class MuninNodeOptions {
    public const string DefaultHostName = "munin-node.localhost";
    public const int DefaultPort = 4949;

    public static IPAddress DefaultAddress { get; }

    public MuninNodeOptions() {}

    public IAccessRule? AccessRule { get; set; }
    public IPAddress Address { get; set; }
    public string HostName { get; set; }
    public int Port { get; set; }

    public MuninNodeOptions AllowFrom(IReadOnlyList<IPAddress> addresses, bool shouldConsiderIPv4MappedIPv6Address = true) {}
    public MuninNodeOptions AllowFromLoopbackOnly() {}
    internal protected virtual void Configure(MuninNodeOptions baseOptions) {}
    public MuninNodeOptions UseAnyAddress() {}
    public MuninNodeOptions UseAnyAddress(int port) {}
    public MuninNodeOptions UseLoopbackAddress() {}
    public MuninNodeOptions UseLoopbackAddress(int port) {}
  }

  public abstract class NodeBase :
    IAsyncDisposable,
    IDisposable,
    IMuninNode,
    IMuninNodeProfile
  {
    [Obsolete("Use a constructor overload that takes IMuninNodeListenerFactory as an argument.")]
    protected NodeBase(IAccessRule? accessRule, ILogger? logger) {}
    protected NodeBase(IMuninNodeListenerFactory listenerFactory, IAccessRule? accessRule, ILogger? logger) {}
    protected NodeBase(IMuninProtocolHandlerFactory protocolHandlerFactory, IMuninNodeListenerFactory listenerFactory, IAccessRule? accessRule, ILogger? logger) {}

    public virtual Encoding Encoding { get; }
    public EndPoint EndPoint { get; }
    public abstract string HostName { get; }
    protected IMuninNodeListener? Listener { get; }
    [Obsolete("Use EndPoint instead.")]
    public EndPoint LocalEndPoint { get; }
    protected ILogger? Logger { get; }
    public virtual Version NodeVersion { get; }
    public abstract IPluginProvider PluginProvider { get; }
    string IMuninNodeProfile.Version { get; }

    public async ValueTask AcceptAsync(bool throwIfCancellationRequested, CancellationToken cancellationToken) {}
    public async ValueTask AcceptSingleSessionAsync(CancellationToken cancellationToken = default) {}
    [Obsolete("Use IMuninNodeListenerFactory and StartAsync instead.")]
    protected virtual Socket CreateServerSocket() {}
    protected virtual void Dispose(bool disposing) {}
    public void Dispose() {}
    public async ValueTask DisposeAsync() {}
    protected virtual async ValueTask DisposeAsyncCore() {}
    protected virtual EndPoint GetLocalEndPointToBind() {}
    protected virtual IMuninNodeProfile GetNodeProfile() {}
    public Task RunAsync(CancellationToken cancellationToken) {}
    [Obsolete("This method will be deprecated in the future.Use IMuninNodeListenerFactory and StartAsync instead.Make sure to override CreateServerSocket if you need to use this method.")]
    public void Start() {}
    public ValueTask StartAsync(CancellationToken cancellationToken = default) {}
    protected virtual ValueTask StartedAsync(CancellationToken cancellationToken) {}
    protected virtual ValueTask StartingAsync(CancellationToken cancellationToken) {}
    public ValueTask StopAsync(CancellationToken cancellationToken = default) {}
    protected virtual ValueTask StoppedAsync(CancellationToken cancellationToken) {}
    protected virtual ValueTask StoppingAsync(CancellationToken cancellationToken) {}
    protected void ThrowIfDisposed() {}
    protected void ThrowIfPluginProviderIsNull() {}
  }
}

namespace Smdn.Net.MuninNode.DependencyInjection {
  [Obsolete("Use or inherit MuninNodeBuilder instead.")]
  public interface IMuninNodeBuilder {
    string ServiceKey { get; }
    IServiceCollection Services { get; }

    IMuninNode Build(IServiceProvider serviceProvider);
  }

  public interface IMuninServiceBuilder {
    IServiceCollection Services { get; }
  }

  [Obsolete("Use MuninNodeBuilderExtensions instead.")]
  public static class IMuninNodeBuilderExtensions {
    public static IMuninNodeBuilder AddPlugin(this IMuninNodeBuilder builder, Func<IServiceProvider, IPlugin> buildPlugin) {}
    public static IMuninNodeBuilder AddPlugin(this IMuninNodeBuilder builder, IPlugin plugin) {}
    public static IMuninNodeBuilder UseListenerFactory(this IMuninNodeBuilder builder, Func<IServiceProvider, EndPoint, IMuninNode, CancellationToken, ValueTask<IMuninNodeListener>> createListenerAsyncFunc) {}
    public static IMuninNodeBuilder UseListenerFactory(this IMuninNodeBuilder builder, Func<IServiceProvider, IMuninNodeListenerFactory> buildListenerFactory) {}
    public static IMuninNodeBuilder UseListenerFactory(this IMuninNodeBuilder builder, IMuninNodeListenerFactory listenerFactory) {}
    public static IMuninNodeBuilder UsePluginProvider(this IMuninNodeBuilder builder, Func<IServiceProvider, IPluginProvider> buildPluginProvider) {}
    public static IMuninNodeBuilder UsePluginProvider(this IMuninNodeBuilder builder, IPluginProvider pluginProvider) {}
    public static IMuninNodeBuilder UseSessionCallback(this IMuninNodeBuilder builder, Func<IServiceProvider, INodeSessionCallback> buildSessionCallback) {}
    public static IMuninNodeBuilder UseSessionCallback(this IMuninNodeBuilder builder, Func<string, CancellationToken, ValueTask>? reportSessionStartedAsyncFunc, Func<string, CancellationToken, ValueTask>? reportSessionClosedAsyncFunc) {}
    public static IMuninNodeBuilder UseSessionCallback(this IMuninNodeBuilder builder, INodeSessionCallback sessionCallback) {}
  }

  public static class IMuninServiceBuilderExtensions {
    public static IMuninNodeBuilder AddNode(this IMuninServiceBuilder builder) {}
    public static IMuninNodeBuilder AddNode(this IMuninServiceBuilder builder, Action<MuninNodeOptions> configure) {}
    public static TMuninNodeBuilder AddNode<TMuninNode, TMuninNodeOptions, TMuninNodeBuilder>(this IMuninServiceBuilder builder, Action<TMuninNodeOptions> configure, Func<IMuninServiceBuilder, string, TMuninNodeBuilder> createBuilder) where TMuninNode : class, IMuninNode where TMuninNodeOptions : MuninNodeOptions, new() where TMuninNodeBuilder : MuninNodeBuilder {}
    public static TMuninNodeBuilder AddNode<TMuninNodeOptions, TMuninNodeBuilder>(this IMuninServiceBuilder builder, Action<TMuninNodeOptions> configure, Func<IMuninServiceBuilder, string, TMuninNodeBuilder> createBuilder) where TMuninNodeOptions : MuninNodeOptions, new() where TMuninNodeBuilder : MuninNodeBuilder {}
    public static TMuninNodeBuilder AddNode<TMuninNodeService, TMuninNodeImplementation, TMuninNodeOptions, TMuninNodeBuilder>(this IMuninServiceBuilder builder, Action<TMuninNodeOptions> configure, Func<IMuninServiceBuilder, string, TMuninNodeBuilder> createBuilder) where TMuninNodeService : class, IMuninNode where TMuninNodeImplementation : class, TMuninNodeService where TMuninNodeOptions : MuninNodeOptions, new() where TMuninNodeBuilder : MuninNodeBuilder {}
  }

  public static class IServiceCollectionExtensions {
    public static IServiceCollection AddMunin(this IServiceCollection services, Action<IMuninServiceBuilder> configure) {}
  }

  public class MuninNodeBuilder : IMuninNodeBuilder {
    internal protected MuninNodeBuilder(IMuninServiceBuilder serviceBuilder, string serviceKey) {}

    public string ServiceKey { get; }
    public IServiceCollection Services { get; }

    protected virtual IMuninNode Build(IPluginProvider pluginProvider, IMuninNodeListenerFactory? listenerFactory, IServiceProvider serviceProvider) {}
    public IMuninNode Build(IServiceProvider serviceProvider) {}
    protected TMuninNodeOptions GetConfiguredOptions<TMuninNodeOptions>(IServiceProvider serviceProvider) where TMuninNodeOptions : MuninNodeOptions {}
  }

  public static class MuninNodeBuilderExtensions {
    public static TMuninNodeBuilder AddPlugin<TMuninNodeBuilder>(this TMuninNodeBuilder builder, Func<IServiceProvider, IPlugin> buildPlugin) where TMuninNodeBuilder : MuninNodeBuilder {}
    public static TMuninNodeBuilder AddPlugin<TMuninNodeBuilder>(this TMuninNodeBuilder builder, IPlugin plugin) where TMuninNodeBuilder : MuninNodeBuilder {}
    public static TMuninNode Build<TMuninNode>(this MuninNodeBuilder builder, IServiceProvider serviceProvider) where TMuninNode : IMuninNode {}
    public static TMuninNodeBuilder UseListenerFactory<TMuninNodeBuilder>(this TMuninNodeBuilder builder, Func<IServiceProvider, EndPoint, IMuninNode, CancellationToken, ValueTask<IMuninNodeListener>> createListenerAsyncFunc) where TMuninNodeBuilder : MuninNodeBuilder {}
    public static TMuninNodeBuilder UseListenerFactory<TMuninNodeBuilder>(this TMuninNodeBuilder builder, Func<IServiceProvider, IMuninNodeListenerFactory> buildListenerFactory) where TMuninNodeBuilder : MuninNodeBuilder {}
    public static TMuninNodeBuilder UseListenerFactory<TMuninNodeBuilder>(this TMuninNodeBuilder builder, IMuninNodeListenerFactory listenerFactory) where TMuninNodeBuilder : MuninNodeBuilder {}
    public static TMuninNodeBuilder UsePluginProvider<TMuninNodeBuilder>(this TMuninNodeBuilder builder, Func<IServiceProvider, IPluginProvider> buildPluginProvider) where TMuninNodeBuilder : MuninNodeBuilder {}
    public static TMuninNodeBuilder UsePluginProvider<TMuninNodeBuilder>(this TMuninNodeBuilder builder, IPluginProvider pluginProvider) where TMuninNodeBuilder : MuninNodeBuilder {}
    public static TMuninNodeBuilder UseSessionCallback<TMuninNodeBuilder>(this TMuninNodeBuilder builder, Func<IServiceProvider, INodeSessionCallback> buildSessionCallback) where TMuninNodeBuilder : MuninNodeBuilder {}
    public static TMuninNodeBuilder UseSessionCallback<TMuninNodeBuilder>(this TMuninNodeBuilder builder, Func<string, CancellationToken, ValueTask>? reportSessionStartedAsyncFunc, Func<string, CancellationToken, ValueTask>? reportSessionClosedAsyncFunc) where TMuninNodeBuilder : MuninNodeBuilder {}
    public static TMuninNodeBuilder UseSessionCallback<TMuninNodeBuilder>(this TMuninNodeBuilder builder, INodeSessionCallback sessionCallback) where TMuninNodeBuilder : MuninNodeBuilder {}
  }
}

namespace Smdn.Net.MuninNode.Protocol {
  public interface IMuninNodeProfile {
    Encoding Encoding { get; }
    string HostName { get; }
    IPluginProvider PluginProvider { get; }
    string Version { get; }
  }

  public interface IMuninProtocolHandler {
    ValueTask HandleCommandAsync(IMuninNodeClient client, ReadOnlySequence<byte> commandLine, CancellationToken cancellationToken);
    ValueTask HandleTransactionEndAsync(IMuninNodeClient client, CancellationToken cancellationToken);
    ValueTask HandleTransactionStartAsync(IMuninNodeClient client, CancellationToken cancellationToken);
  }

  public interface IMuninProtocolHandlerFactory {
    ValueTask<IMuninProtocolHandler> CreateAsync(IMuninNodeProfile profile, CancellationToken cancellationToken);
  }

  public class MuninProtocolHandler : IMuninProtocolHandler {
    public MuninProtocolHandler(IMuninNodeProfile profile) {}

    protected bool IsDirtyConfigEnabled { get; }
    protected bool IsMultigraphEnabled { get; }

    protected virtual ValueTask HandleCapCommandAsync(IMuninNodeClient client, ReadOnlySequence<byte> arguments, CancellationToken cancellationToken) {}
    public ValueTask HandleCommandAsync(IMuninNodeClient client, ReadOnlySequence<byte> commandLine, CancellationToken cancellationToken = default) {}
    protected virtual ValueTask HandleCommandAsyncCore(IMuninNodeClient client, ReadOnlySequence<byte> commandLine, CancellationToken cancellationToken) {}
    protected virtual ValueTask HandleConfigCommandAsync(IMuninNodeClient client, ReadOnlySequence<byte> arguments, CancellationToken cancellationToken) {}
    protected virtual async ValueTask HandleFetchCommandAsync(IMuninNodeClient client, ReadOnlySequence<byte> arguments, CancellationToken cancellationToken) {}
    protected virtual ValueTask HandleListCommandAsync(IMuninNodeClient client, ReadOnlySequence<byte> arguments, CancellationToken cancellationToken) {}
    protected virtual ValueTask HandleNodesCommandAsync(IMuninNodeClient client, CancellationToken cancellationToken) {}
    protected virtual ValueTask HandleQuitCommandAsync(IMuninNodeClient client, CancellationToken cancellationToken) {}
    public ValueTask HandleTransactionEndAsync(IMuninNodeClient client, CancellationToken cancellationToken = default) {}
    protected virtual ValueTask HandleTransactionEndAsyncCore(IMuninNodeClient client, CancellationToken cancellationToken) {}
    public ValueTask HandleTransactionStartAsync(IMuninNodeClient client, CancellationToken cancellationToken = default) {}
    protected virtual ValueTask HandleTransactionStartAsyncCore(IMuninNodeClient client, CancellationToken cancellationToken) {}
    protected virtual ValueTask HandleVersionCommandAsync(IMuninNodeClient client, CancellationToken cancellationToken) {}
    protected ValueTask SendResponseAsync(IMuninNodeClient client, IEnumerable<string> responseLines, CancellationToken cancellationToken) {}
  }

  public static class MuninProtocolHandlerFactory {
    public static IMuninProtocolHandlerFactory Default { get; }
  }
}

namespace Smdn.Net.MuninNode.Transport {
  public interface IMuninNodeClient :
    IAsyncDisposable,
    IDisposable
  {
    EndPoint? EndPoint { get; }

    ValueTask DisconnectAsync(CancellationToken cancellationToken);
    ValueTask<int> ReceiveAsync(IBufferWriter<byte> buffer, CancellationToken cancellationToken);
    ValueTask SendAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);
  }

  public interface IMuninNodeListener :
    IAsyncDisposable,
    IDisposable
  {
    EndPoint? EndPoint { get; }

    ValueTask<IMuninNodeClient> AcceptAsync(CancellationToken cancellationToken);
    ValueTask StartAsync(CancellationToken cancellationToken);
  }

  public interface IMuninNodeListenerFactory {
    ValueTask<IMuninNodeListener> CreateAsync(EndPoint endPoint, IMuninNode node, CancellationToken cancellationToken);
  }

  public sealed class MuninNodeClientDisconnectedException : InvalidOperationException {
    public MuninNodeClientDisconnectedException() {}
    public MuninNodeClientDisconnectedException(string message) {}
    public MuninNodeClientDisconnectedException(string message, Exception innerException) {}
  }
}

namespace Smdn.Net.MuninPlugin {
  public interface IMultigraphPlugin : IPlugin {
    IReadOnlyCollection<IPlugin> Plugins { get; }
  }

  public interface INodeSessionCallback {
    ValueTask ReportSessionClosedAsync(string sessionId, CancellationToken cancellationToken);
    ValueTask ReportSessionStartedAsync(string sessionId, CancellationToken cancellationToken);
  }

  public interface IPlugin {
    IPluginDataSource DataSource { get; }
    IPluginGraphAttributes GraphAttributes { get; }
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

  public interface IPluginGraphAttributes {
    IEnumerable<string> EnumerateAttributes();
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

  public enum WellKnownCategory : int {
    AntiVirus = 2,
    ApplicationServer = 3,
    AuthenticationServer = 4,
    Backup = 5,
    Cloud = 7,
    ContentManagementSystem = 8,
    Cpu = 9,
    DatabaseServer = 10,
    DevelopmentTool = 11,
    Disk = 12,
    Dns = 13,
    FileSystem = 16,
    FileTransfer = 14,
    Forum = 15,
    GameServer = 18,
    HighThroughputComputing = 19,
    LoadBalancer = 20,
    Mail = 21,
    MailingList = 22,
    Memory = 23,
    MessagingServer = 6,
    Munin = 24,
    Network = 25,
    NetworkFiltering = 17,
    OneSec = 1,
    Other = 0,
    Printing = 26,
    Process = 27,
    Radio = 28,
    Search = 30,
    Security = 31,
    Sensor = 32,
    SpamFilter = 33,
    StorageAreaNetwork = 29,
    Streaming = 34,
    System = 35,
    TimeSynchronization = 36,
    Video = 37,
    Virtualization = 38,
    VoIP = 39,
    WebServer = 40,
    Wiki = 41,
    Wireless = 42,
  }

  public sealed class AggregatePluginProvider :
    ReadOnlyCollection<IPluginProvider>,
    INodeSessionCallback,
    IPluginProvider
  {
    public AggregatePluginProvider(IList<IPluginProvider> pluginProviders) {}

    public IReadOnlyCollection<IPlugin> Plugins { get; }
    INodeSessionCallback? IPluginProvider.SessionCallback { get; }

    async ValueTask INodeSessionCallback.ReportSessionClosedAsync(string sessionId, CancellationToken cancellationToken) {}
    async ValueTask INodeSessionCallback.ReportSessionStartedAsync(string sessionId, CancellationToken cancellationToken) {}
  }

  public class MultigraphPlugin : IMultigraphPlugin {
    public MultigraphPlugin(string name, IReadOnlyCollection<IPlugin> plugins) {}

    public IPluginDataSource DataSource { get; }
    public IPluginGraphAttributes GraphAttributes { get; }
    public string Name { get; }
    public IReadOnlyCollection<IPlugin> Plugins { get; }
    public INodeSessionCallback? SessionCallback { get; }
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
    IPluginGraphAttributes IPlugin.GraphAttributes { get; }
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
    public static IPlugin CreatePlugin(string name, IPluginGraphAttributes graphAttributes, IReadOnlyCollection<IPluginField> fields) {}
    public static IPlugin CreatePlugin(string name, IPluginGraphAttributes graphAttributes, IReadOnlyCollection<PluginFieldBase> fields) {}
    public static IPlugin CreatePlugin(string name, IPluginGraphAttributes graphAttributes, PluginFieldBase field) {}
    [Obsolete("Use overloads that accept IPluginGraphAttributes instead.")]
    public static IPlugin CreatePlugin(string name, PluginGraphAttributes graphAttributes, IReadOnlyCollection<IPluginField> fields) {}
    [Obsolete("Use overloads that accept IPluginGraphAttributes instead.")]
    public static IPlugin CreatePlugin(string name, PluginGraphAttributes graphAttributes, IReadOnlyCollection<PluginFieldBase> fields) {}
    [Obsolete("Use overloads that accept IPluginGraphAttributes instead.")]
    public static IPlugin CreatePlugin(string name, PluginGraphAttributes graphAttributes, PluginFieldBase field) {}
    public static IPlugin CreatePlugin(string name, string fieldLabel, Func<double?> fetchFieldValue, IPluginGraphAttributes graphAttributes) {}
    [Obsolete("Use overloads that accept IPluginGraphAttributes instead.")]
    public static IPlugin CreatePlugin(string name, string fieldLabel, Func<double?> fetchFieldValue, PluginGraphAttributes graphAttributes) {}
    public static IPlugin CreatePlugin(string name, string fieldLabel, PluginFieldGraphStyle fieldGraphStyle, Func<double?> fetchFieldValue, IPluginGraphAttributes graphAttributes) {}
    [Obsolete("Use overloads that accept IPluginGraphAttributes instead.")]
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

  public sealed class PluginGraphAttributes : IPluginGraphAttributes {
    public PluginGraphAttributes(string title, string category, string verticalLabel, bool scale, string arguments) {}
    public PluginGraphAttributes(string title, string category, string verticalLabel, bool scale, string arguments, TimeSpan? updateRate, int? width, int? height, IEnumerable<string>? order, string? totalValueLabel) {}

    public string Arguments { get; }
    public string Category { get; }
    public int? Height { get; }
    public string? Order { get; }
    public bool Scale { get; }
    public string Title { get; }
    public string? TotalValueLabel { get; }
    public TimeSpan? UpdateRate { get; }
    public string VerticalLabel { get; }
    public int? Width { get; }

    public IEnumerable<string> EnumerateAttributes() {}
  }

  public class PluginGraphAttributesBuilder {
    public static Regex RegexCategory { get; }
    public static Regex RegexTitle { get; }

    public PluginGraphAttributesBuilder(string title) {}
    public PluginGraphAttributesBuilder(string title, PluginGraphAttributesBuilder baseBuilder) {}

    public PluginGraphAttributesBuilder AddGraphArgument(string argument) {}
    public IPluginGraphAttributes Build() {}
    public PluginGraphAttributesBuilder ClearGraphArguments() {}
    public PluginGraphAttributesBuilder DisableUnitScaling() {}
    public PluginGraphAttributesBuilder EnableUnitScaling() {}
    public PluginGraphAttributesBuilder HideGraph() {}
    public PluginGraphAttributesBuilder ShowGraph() {}
    public PluginGraphAttributesBuilder WithCategory(WellKnownCategory category) {}
    public PluginGraphAttributesBuilder WithCategory(string category) {}
    public PluginGraphAttributesBuilder WithCategoryOther() {}
    public PluginGraphAttributesBuilder WithFieldOrder(IEnumerable<string> order) {}
    public PluginGraphAttributesBuilder WithFormatString(string printf) {}
    public PluginGraphAttributesBuilder WithGraphBinaryBase() {}
    public PluginGraphAttributesBuilder WithGraphDecimalBase() {}
    public PluginGraphAttributesBuilder WithGraphLimit(double lowerLimitValue, double upperLimitValue) {}
    public PluginGraphAttributesBuilder WithGraphLogarithmic() {}
    public PluginGraphAttributesBuilder WithGraphLowerLimit(double @value) {}
    public PluginGraphAttributesBuilder WithGraphRigid() {}
    public PluginGraphAttributesBuilder WithGraphUpperLimit(double @value) {}
    public PluginGraphAttributesBuilder WithHeight(int height) {}
    public PluginGraphAttributesBuilder WithSize(int width, int height) {}
    public PluginGraphAttributesBuilder WithTitle(string title) {}
    public PluginGraphAttributesBuilder WithTotal(string labelForTotal) {}
    public PluginGraphAttributesBuilder WithUpdateRate(TimeSpan updateRate) {}
    public PluginGraphAttributesBuilder WithVerticalLabel(string verticalLabel) {}
    public PluginGraphAttributesBuilder WithWidth(int width) {}
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
// API list generated by Smdn.Reflection.ReverseGenerating.ListApi.MSBuild.Tasks v1.6.0.0.
// Smdn.Reflection.ReverseGenerating.ListApi.Core v1.4.0.0 (https://github.com/smdn/Smdn.Reflection.ReverseGenerating)
