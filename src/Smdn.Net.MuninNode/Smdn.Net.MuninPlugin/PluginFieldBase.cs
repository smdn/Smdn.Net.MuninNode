// SPDX-FileCopyrightText: 2021 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.MuninPlugin;

public abstract class PluginFieldBase : IPluginField {
  public string Name { get; }
  public string Label { get; }
  public PluginFieldGraphStyle GraphStyle { get; }
  public PluginFieldNormalValueRange NormalRangeForWarning { get; }
  public PluginFieldNormalValueRange NormalRangeForCritical { get; }
  public string? NegativeFieldName { get; }

#pragma warning disable CA1033
  PluginFieldAttributes IPluginField.Attributes => new(
    label: Label,
    graphStyle: GraphStyle,
    normalRangeForWarning: NormalRangeForWarning,
    normalRangeForCritical: NormalRangeForCritical,
    negativeFieldName: NegativeFieldName
  );
#pragma warning restore CA1033

  protected PluginFieldBase(
    string label,
    string? name,
    PluginFieldGraphStyle graphStyle = default,
    PluginFieldNormalValueRange normalRangeForWarning = default,
    PluginFieldNormalValueRange normalRangeForCritical = default
  )
    : this(
      label: label,
      name: name,
      graphStyle: graphStyle,
      normalRangeForWarning: normalRangeForWarning,
      normalRangeForCritical: normalRangeForCritical,
      negativeFieldName: null
    )
  {
  }

  protected PluginFieldBase(
    string label,
    string? name,
    PluginFieldGraphStyle graphStyle,
    PluginFieldNormalValueRange normalRangeForWarning,
    PluginFieldNormalValueRange normalRangeForCritical,
    string? negativeFieldName
  )
  {
    if (label is null)
      throw new ArgumentNullException(nameof(label));
    if (label.Length == 0)
      throw ExceptionUtils.CreateArgumentMustBeNonEmptyString(nameof(label));
    if (!RegexValidFieldLabel.IsMatch(label))
      throw new ArgumentException($"'{label}' is invalid for field name. The value of {nameof(label)} must match the following regular expression: '{RegexValidFieldLabel}'", nameof(label));

    name ??= GetDefaultNameFromLabel(label);

    if (name.Length == 0)
      throw ExceptionUtils.CreateArgumentMustBeNonEmptyString(nameof(name));
    if (!RegexValidFieldName.IsMatch(name))
      throw new ArgumentException($"'{name}' is invalid for field name. The value of {nameof(name)} must match the following regular expression: '{RegexValidFieldName}'", nameof(name));

    Label = label;
    Name = name;
    GraphStyle = graphStyle;
    NormalRangeForWarning = normalRangeForWarning;
    NormalRangeForCritical = normalRangeForCritical;
    NegativeFieldName = negativeFieldName;
  }

  /// <summary>Gets a value for plugin field.</summary>
  /// <remarks>Reports 'UNKNOWN' as a plugin field value if the return value is <see langword="null"/>.</remarks>
  protected abstract ValueTask<double?> FetchValueAsync(CancellationToken cancellationToken);

#pragma warning disable CA1033
  async ValueTask<string> IPluginField.GetFormattedValueStringAsync(CancellationToken cancellationToken)
  {
    const string UnknownValueString = "U";

    var value = await FetchValueAsync(cancellationToken).ConfigureAwait(false);

    return value?.ToString(provider: CultureInfo.InvariantCulture) ?? UnknownValueString;
  }
#pragma warning restore CA1033

  // https://guide.munin-monitoring.org/en/latest/reference/plugin.html#field-name-attributes
  // Field name attributes
  //   Attribute: {fieldname}.label
  //   Value: anything except # and \
  private static readonly Regex RegexValidFieldLabel = new(
    pattern: $@"^[^{Regex.Escape("#\\")}]+$",
    options: RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant
  );

  // https://guide.munin-monitoring.org/en/latest/reference/plugin.html#notes-on-field-names
  // Notes on field names
  //   The characters must be [a-zA-Z0-9_], while the first character must be [a-zA-Z_].
#pragma warning disable SYSLIB1045
  private static readonly Regex RegexValidFieldName = new(
    pattern: $@"^[a-zA-Z_][a-zA-Z0-9_]*$",
    options: RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant
  );

  private static readonly Regex RegexInvalidFieldNamePrefix = new(
    pattern: $@"^[0-9_]+",
    options: RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant
  );
  private static readonly Regex RegexInvalidFieldNameChars = new(
    pattern: $@"[^a-zA-Z0-9_]",
    options: RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant
  );
#pragma warning restore SYSLIB1045

  private static string GetDefaultNameFromLabel(string label)
  {
    if (label is null)
      throw new ArgumentNullException(nameof(label));
    if (label.Length == 0)
      throw ExceptionUtils.CreateArgumentMustBeNonEmptyString(nameof(label));

    return RegexInvalidFieldNameChars.Replace(
      RegexInvalidFieldNamePrefix.Replace(label, string.Empty),
      string.Empty
    );
  }
}
