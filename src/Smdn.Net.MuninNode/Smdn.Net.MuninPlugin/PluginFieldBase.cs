// SPDX-FileCopyrightText: 2021 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.MuninPlugin;

public abstract class PluginFieldBase : IPluginField {
  public string Name { get; }
  public string Label { get; }
  public PluginFieldGraphStyle GraphStyle { get; }

  PluginFieldAttributes IPluginField.Attributes => new(
    label: Label,
    graphStyle: GraphStyle
  );

  protected PluginFieldBase(
    string label,
    string? name,
    PluginFieldGraphStyle graphStyle = default
  )
  {
    if (label is null)
      throw new ArgumentNullException(nameof(label));
    if (label.Length == 0)
      throw ExceptionUtils.CreateArgumentMustBeNonEmptyString(nameof(label));
    if (!regexValidFieldLabel.IsMatch(label))
      throw new ArgumentException($"'{label}' is invalid for field name. The value of {nameof(label)} must match the following regular expression: '{regexValidFieldLabel}'", nameof(label));

    name ??= GetDefaultNameFromLabel(label);

    if (name.Length == 0)
      throw ExceptionUtils.CreateArgumentMustBeNonEmptyString(nameof(name));
    if (!regexValidFieldName.IsMatch(name))
      throw new ArgumentException($"'{name}' is invalid for field name. The value of {nameof(name)} must match the following regular expression: '{regexValidFieldName}'", nameof(name));

    Label = label;
    Name = name;
    GraphStyle = graphStyle;
  }

  /// <summary>Gets a value for plugin field.</summary>
  /// <remarks>Reports 'UNKNOWN' as a plugin field value if the return value is <see langword="null"/>.</remarks>
  protected abstract ValueTask<double?> FetchValueAsync(CancellationToken cancellationToken);

  async ValueTask<string> IPluginField.GetFormattedValueStringAsync(CancellationToken cancellationToken)
  {
    const string unknownValueString = "U";

    var value = await FetchValueAsync(cancellationToken).ConfigureAwait(false);

    return value?.ToString(provider: null) ?? unknownValueString; // TODO: format specifier
  }

  // http://guide.munin-monitoring.org/en/latest/reference/plugin.html#field-name-attributes
  // Field name attributes
  //   Attribute: {fieldname}.label
  //   Value: anything except # and \
  private static readonly Regex regexValidFieldLabel = new(
    pattern: $@"^[^{Regex.Escape("#\\")}]+$",
    options: RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant
  );

  // http://guide.munin-monitoring.org/en/latest/reference/plugin.html#notes-on-field-names
  // Notes on field names
  //   The characters must be [a-zA-Z0-9_], while the first character must be [a-zA-Z_].
  private static readonly Regex regexValidFieldName = new(
    pattern: $@"^[a-zA-Z_][a-zA-Z0-9_]*$",
    options: RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant
  );

  private static readonly Regex regexInvalidFieldNamePrefix = new(
    pattern: $@"^[0-9_]+",
    options: RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant
  );
  private static readonly Regex regexInvalidFieldNameChars = new(
    pattern: $@"[^a-zA-Z0-9_]",
    options: RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant
  );

  private static string GetDefaultNameFromLabel(string label)
  {
    if (label == null)
      throw new ArgumentNullException(nameof(label));
    if (label.Length == 0)
      throw ExceptionUtils.CreateArgumentMustBeNonEmptyString(nameof(label));

    return regexInvalidFieldNameChars.Replace(
      regexInvalidFieldNamePrefix.Replace(label, string.Empty),
      string.Empty
    );
  }
}
