// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.MuninPlugin;

/// <summary>
/// Provides an interface that abstracts the fields of the plugin.
/// This interface represents the 'field name attributes'.
/// </summary>
/// <seealso href="http://guide.munin-monitoring.org/en/latest/reference/plugin.html#field-name-attributes">Plugin reference - Field name attributes</seealso>
public interface IPluginField {
  /// <summary>Gets a value for the <c>fieldname</c>. This value represents the <c>fieldname</c> itself for the attribute <c>{fieldname}.xxx</c>.</summary>
  /// <seealso href="http://guide.munin-monitoring.org/en/latest/reference/plugin.html#field-name-attributes">Plugin reference - Field name attributes</seealso>
  string Name { get; }

  /// <summary>Gets a collection of attributes that describes the <c>fieldname</c>.</summary>
  PluginFieldAttributes Attributes { get; }

  /// <summary>
  /// Gets a current value corresponding to the field, in its string representation.
  /// </summary>
  /// <param name="cancellationToken">The <see cref="CancellationToken" /> to monitor for cancellation requests.</param>
  /// <returns>
  /// A <see cref="ValueTask{string?}"/> representing the current value of the field.
  /// Returns a field's numeric value in its string representation.
  /// By returning <c>"U"</c> instead of numeric value, the field can also be reported as having a value of 'UNKNOWN'.
  /// </returns>
  ValueTask<string> GetFormattedValueStringAsync(CancellationToken cancellationToken);
}
