// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;

namespace Smdn.Net.MuninPlugin;

#pragma warning disable IDE0040
partial class PluginGraphAttributesBuilder {
#pragma warning restore IDE0040
  private readonly List<string> graphArgs = new(capacity: 0);

  /// <summary>Adds an argument to the internal list for the <c>graph_args</c>.</summary>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/plugin.html#graph-args">Plugin reference - Global attributes - graph_args</seealso>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/example/graph/graph_args.html">Recommended graph_args</seealso>
  /// <exception cref="ArgumentNullException"><paramref name="argument"/> is <see langword="null"/>.</exception>
  /// <exception cref="ArgumentException"><paramref name="argument"/> is empty.</exception>
  public PluginGraphAttributesBuilder AddGraphArgument(string argument)
  {
    ArgumentExceptionShim.ThrowIfNullOrWhiteSpace(argument, nameof(argument));

    graphArgs.Add(argument);

    return this;
  }

  /// <summary>Clears the internal list for the <c>graph_args</c>.</summary>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/reference/plugin.html#graph-args">Plugin reference - Global attributes - graph_args</seealso>
  /// <seealso href="https://guide.munin-monitoring.org/en/latest/example/graph/graph_args.html">Recommended graph_args</seealso>
  public PluginGraphAttributesBuilder ClearGraphArguments()
  {
    graphArgs.Clear();

    return this;
  }

  /// <summary>Adds <c>--lower-limit</c> to the internal list for the <c>graph_args</c>.</summary>
  /// <seealso href="https://oss.oetiker.ch/rrdtool/doc/rrdgraph.en.html">rrdgraph - Options - <c>[-l|--lower-limit value]</c></seealso>
  public PluginGraphAttributesBuilder WithGraphLowerLimit(double value)
    => AddGraphArgument($"--lower-limit {value}"); // TODO: validation

  /// <summary>Adds <c>--upper-limit</c> to the internal list for the <c>graph_args</c>.</summary>
  /// <seealso href="https://oss.oetiker.ch/rrdtool/doc/rrdgraph.en.html">rrdgraph - Options - <c>[-u|--upper-limit value]</c></seealso>
  public PluginGraphAttributesBuilder WithGraphUpperLimit(double value)
    => AddGraphArgument($"--upper-limit {value}"); // TODO: validation

  /// <summary>Adds <c>--lower-limit</c> and <c>--upper-limit</c> to the internal list for the <c>graph_args</c>.</summary>
  /// <seealso href="https://oss.oetiker.ch/rrdtool/doc/rrdgraph.en.html">rrdgraph - Options - <c>[-l|--lower-limit value]</c></seealso>
  /// <seealso href="https://oss.oetiker.ch/rrdtool/doc/rrdgraph.en.html">rrdgraph - Options - <c>[-u|--upper-limit value]</c></seealso>
  public PluginGraphAttributesBuilder WithGraphLimit(double lowerLimitValue, double upperLimitValue)
    => WithGraphLowerLimit(lowerLimitValue).WithGraphUpperLimit(upperLimitValue);

  /// <summary>Adds <c>--rigid</c> to the internal list for the <c>graph_args</c>.</summary>
  /// <seealso href="https://oss.oetiker.ch/rrdtool/doc/rrdgraph.en.html">rrdgraph - Options - <c>[-r|--rigid]</c></seealso>
  public PluginGraphAttributesBuilder WithGraphRigid()
    => AddGraphArgument("--rigid");

  /// <summary>Adds <c>--base 1000</c> to the internal list for the <c>graph_args</c>.</summary>
  /// <seealso href="https://oss.oetiker.ch/rrdtool/doc/rrdgraph.en.html">rrdgraph - Options - <c>[-b|--base value]</c></seealso>
  public PluginGraphAttributesBuilder WithGraphDecimalBase()
    => AddGraphArgument("--base 1000");

  /// <summary>Adds <c>--base 1024</c> to the internal list for the <c>graph_args</c>.</summary>
  /// <seealso href="https://oss.oetiker.ch/rrdtool/doc/rrdgraph.en.html">rrdgraph - Options - <c>[-b|--base value]</c></seealso>
  public PluginGraphAttributesBuilder WithGraphBinaryBase()
    => AddGraphArgument("--base 1024");

  /// <summary>Adds <c>--logarithmic</c> to the internal list for the <c>graph_args</c>.</summary>
  /// <seealso href="https://oss.oetiker.ch/rrdtool/doc/rrdgraph.en.html">rrdgraph - Options - <c>[-o|--logarithmic]</c></seealso>
  public PluginGraphAttributesBuilder WithGraphLogarithmic()
    => AddGraphArgument("--logarithmic");
}
