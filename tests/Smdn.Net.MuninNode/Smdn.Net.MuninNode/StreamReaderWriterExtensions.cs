// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.MuninNode;

internal static class StreamReaderWriterExtensions {
#if !NET7_0_OR_GREATER
  public static async Task<string?> ReadLineAsync(this StreamReader reader, CancellationToken cancellationToken)
    => await reader.ReadLineAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
#endif

  public static async Task WriteLineAsync(this StreamWriter writer, string value, CancellationToken cancellationToken)
    => await writer.WriteLineAsync(value).WaitAsync(cancellationToken).ConfigureAwait(false);

  public static async Task FlushAsync(this StreamWriter writer, CancellationToken cancellationToken)
    => await writer.FlushAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
}
