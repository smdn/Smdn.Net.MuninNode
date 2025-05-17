// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
namespace System;

internal static class ArgumentOutOfRangeExceptionShim {
  public static void ThrowIfLessThanOrEqual<T>(T value, T other, string? paramName)
    where T : IComparable<T>
  {
#if SYSTEM_ARGUMENTOUTOFRANGEEXCEPTION_THROWIFLESSTHANOREQUAL
    ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, other, paramName);
#else
    if (value.CompareTo(other) <= 0)
      throw Smdn.ExceptionUtils.CreateArgumentMustBeGreaterThan(other, paramName: paramName, actualValue: value);
#endif
  }
}
