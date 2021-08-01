// SPDX-FileCopyrightText: 2021 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;

namespace Smdn.Template.Namespace {
  public class Class {
    public int Method(int n)
    {
      if (n == 0)
        throw new ArgumentOutOfRangeException(nameof(n), n, "must be non zero value");

      return n;
    }
  }
}
