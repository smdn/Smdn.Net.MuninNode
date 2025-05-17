// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
namespace System;

internal static class ArgumentExceptionShim {
  public static void ThrowIfNullOrEmpty(string? argument, string? paramName)
  {
#if SYSTEM_ARGUMENTEXCEPTION_THROWIFNULLOREMPTY
    ArgumentException.ThrowIfNullOrEmpty(argument, paramName);
#else
    if (argument is null)
      throw new ArgumentNullException(paramName: paramName);
    if (argument.Length == 0)
      throw Smdn.ExceptionUtils.CreateArgumentMustBeNonEmptyString(paramName: paramName);
#endif
  }

  public static void ThrowIfNullOrWhiteSpace(string? argument, string? paramName)
  {
#if SYSTEM_ARGUMENTEXCEPTION_THROWIFNULLORWHITESPACE
    ArgumentException.ThrowIfNullOrWhiteSpace(argument, paramName);
#else
    if (string.IsNullOrWhiteSpace(argument)) {
      if (argument is null)
        throw new ArgumentNullException(paramName: paramName);
      else
        throw new ArgumentException($"{paramName} is empty or consists only of white-space characters", paramName: paramName);
    }
#endif
  }
}
