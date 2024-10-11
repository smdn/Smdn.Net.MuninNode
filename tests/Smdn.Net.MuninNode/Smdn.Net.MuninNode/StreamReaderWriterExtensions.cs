// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#if !SYSTEM_THREADING_TASKS_TASK_WAITASYNC
#error "System.Threading.Tasks.WaitAsync is unavailable."
#endif

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.MuninNode;

internal static class StreamReaderWriterExtensions {
#if !SYSTEM_IO_TEXTREADER_READLINEASYNC_CANCELLATIONTOKEN
  public static async Task<string?> ReadLineAsync(this StreamReader reader, CancellationToken cancellationToken)
    => await reader.ReadLineAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
#endif

  public static async Task WriteLineAsync(this StreamWriter writer, string value, CancellationToken cancellationToken)
#if SYSTEM_IO_TEXTWRITER_WRITELINEASYNC_CANCELLATIONTOKEN
    => await writer.WriteLineAsync(value.AsMemory(), cancellationToken).ConfigureAwait(false);
#else
    => await writer.WriteLineAsync(value.AsMemory()).WaitAsync(cancellationToken).ConfigureAwait(false);
#endif

#if !SYSTEM_IO_TEXTWRITER_FLUSHASYNC_CANCELLATIONTOKEN
  public static async Task FlushAsync(this StreamWriter writer, CancellationToken cancellationToken)
    => await writer.FlushAsync(cancellationToken).WaitAsync(cancellationToken).ConfigureAwait(false);
#endif
}
