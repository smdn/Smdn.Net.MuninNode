// SPDX-FileCopyrightText: 2021 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#if !SYSTEM_TEXT_STRINGBUILDER_APPEND_IFORMATPROVIDER_APPENDINTERPOLATEDSTRINGHANDLER
using System;
using System.Text;

namespace Smdn.Net.MuninNode.Protocol;

internal static class StringBuilderExtensions {
#pragma warning disable IDE0060
  public static StringBuilder Append(this StringBuilder builder, IFormatProvider? provider, string value)
    => builder.Append(value);
#pragma warning restore IDE0060
}
#endif
