// SPDX-FileCopyrightText: 2025 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;

using Microsoft.Extensions.DependencyInjection;

namespace Smdn.Net.MuninNode.DependencyInjection;

internal sealed class MuninServiceBuilder : IMuninServiceBuilder {
  public IServiceCollection Services { get; }

  public MuninServiceBuilder(IServiceCollection services)
  {
    Services = services ?? throw new ArgumentNullException(nameof(services));
  }
}
