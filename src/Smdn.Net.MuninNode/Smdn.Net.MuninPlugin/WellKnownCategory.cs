// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

namespace Smdn.Net.MuninPlugin;

/// <summary>
/// Enumerates the categories used for the graph categories, defined as
/// <see href="https://guide.munin-monitoring.org/en/latest/reference/graph-category.html#well-known-categories">'well known categories'</see>.
/// Categories are used by Munin master to classify plugin graphs.
/// </summary>
/// <seealso cref="PluginGraphAttributes.Category"/>
/// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/graph-category.html">Plugin graph categories</seealso>
/// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/graph-category.html#well-known-categories">Well known categories</seealso>
public enum WellKnownCategory {
  OneSec,

  /// <remarks>
  /// Description of Category: "Anti virus tools".
  /// </remarks>
  AntiVirus,

  /// <remarks>
  /// Description of Category: "Application servers".
  /// </remarks>
  ApplicationServer,

  /// <remarks>
  /// Description of Category: "Authentication servers and services".
  /// </remarks>
  AuthenticationServer,

  /// <remarks>
  /// Description of Category: "All measurements around backup creation".
  /// </remarks>
  Backup,

  /// <remarks>
  /// Description of Category: "Messaging servers".
  /// </remarks>
  MessagingServer,

  /// <remarks>
  /// Description of Category: "Cloud providers and cloud components".
  /// </remarks>
  Cloud,

  /// <remarks>
  /// Description of Category: "Content Management Systems".
  /// </remarks>
  ContentManagementSystem,

  /// <remarks>
  /// Description of Category: "CPU measurements".
  /// </remarks>
  Cpu,

  /// <remarks>
  /// Description of Category: "Database servers".
  /// </remarks>
  DatabaseServer,

  /// <remarks>
  /// Description of Category: "(Software) Development Tools".
  /// </remarks>
  DevelopmentTool,

  /// <remarks>
  /// Description of Category: "Disk and other storage measurements".
  /// </remarks>
  Disk,

  /// <remarks>
  /// Description of Category: "Domain Name Server".
  /// </remarks>
  Dns,

  /// <remarks>
  /// Description of Category: "Filetransfer tools and servers".
  /// </remarks>
  FileTransfer,

  /// <remarks>
  /// Description of Category: "Forum applications".
  /// </remarks>
  Forum,

  /// <remarks>
  /// Description of Category: "(Network) Filesystem activities, includes also monitoring of distributed storage appliances".
  /// </remarks>
  FileSystem,

  /// <remarks>
  /// Description of Category: "All measurements around network filtering".
  /// </remarks>
  NetworkFiltering,

  /// <remarks>
  /// Description of Category: "Game-Server".
  /// </remarks>
  GameServer,

  /// <remarks>
  /// Description of Category: "High-throughput computing".
  /// </remarks>
  HighThroughputComputing,

  /// <remarks>
  /// Description of Category: "Load balancing and proxy servers..".
  /// </remarks>
  LoadBalancer,

  /// <remarks>
  /// Description of Category: "Mail throughput, mail queues, etc.".
  /// </remarks>
  Mail,

  /// <remarks>
  /// Description of Category: "Listserver".
  /// </remarks>
  MailingList,

  /// <remarks>
  /// Description of Category: "All kind of memory measurements. Note that info about memory caching servers is also placed here".
  /// </remarks>
  Memory,

  /// <remarks>
  /// Description of Category: "Monitoring the monitoring.. (includes other monitoring servers also)".
  /// </remarks>
  Munin,

  /// <remarks>
  /// Description of Category: "General networking metrics.".
  /// </remarks>
  Network,

  /// <remarks>
  /// Description of Category: "Plugins that address seldom used products. Category /other/ is the default, so if the plugin doesn’t declare a category, it is also shown here.".
  /// </remarks>
  Other,

  /// <remarks>
  /// Description of Category: "Monitor printers and print jobs".
  /// </remarks>
  Printing,

  /// <remarks>
  /// Description of Category: "Process and kernel related measurements".
  /// </remarks>
  Process,

  /// <remarks>
  /// Description of Category: "Receivers, signal quality, recording, ..".
  /// </remarks>
  Radio,

  /// <remarks>
  /// Description of Category: "Storage Area Network".
  /// </remarks>
  StorageAreaNetwork,

  /// <remarks>
  /// Description of Category: "All kinds of measurement around search engines".
  /// </remarks>
  Search,

  /// <remarks>
  /// Description of Category: "Security information".
  /// </remarks>
  Security,

  /// <remarks>
  /// Description of Category: "Sensor measurements of device and environment".
  /// </remarks>
  Sensor,

  /// <remarks>
  /// Description of Category: "Spam fighters at work".
  /// </remarks>
  SpamFilter,

  Streaming,

  /// <remarks>
  /// Description of Category: "General operating system metrics.".
  /// </remarks>
  System,

  /// <remarks>
  /// Description of Category: "Time synchronization".
  /// </remarks>
  TimeSynchronization,

  /// <remarks>
  /// Description of Category: "Video devices and servers".
  /// </remarks>
  Video,

  /// <remarks>
  /// Description of Category: "All kind of measurements about server virtualization. Includes also Operating-system-level virtualization".
  /// </remarks>
  Virtualization,

  /// <remarks>
  /// Description of Category: "Voice over IP servers".
  /// </remarks>
  VoIP,

  /// <remarks>
  /// Description of Category: "All kinds of webserver measurements and also for related components".
  /// </remarks>
  WebServer,

  /// <remarks>
  /// Description of Category: "wiki applications".
  /// </remarks>
  Wiki,

  Wireless,
}
