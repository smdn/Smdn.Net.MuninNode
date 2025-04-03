using System.Collections.Generic;
using System.Linq;
using System.Net;

using Microsoft.Extensions.Configuration;

namespace Smdn.Net.MuninNode;

public sealed class MuninConfiguration {

  public MuninConfiguration(IConfiguration? configuration = null)
  {
    Port = configuration?["MuninNode:Port"]?.ToInt() ?? 4949;
    Listen = configuration?["MuninNode:Listen"]?.ToIPAddress() ?? IPAddress.Loopback;
    Hostname = configuration?["MuninNode:Hostname"] ?? "localhost";
    AllowFrom = configuration?["MuninNode:AllowFrom"]?.ToNetworkList() ?? [IPAddress.Loopback, IPAddress.IPv6Loopback];
  }

  public IPAddress Listen { get; set; }
  public int Port { get; set; }
  public string Hostname { get; set; }
  public List<IPAddress> AllowFrom { get; set; }
}

public static class StringExt {
  public static int? ToInt(this string s)
  {
    int.TryParse(s, out var value);
    return value;
  }

  public static List<IPAddress> ToNetworkList(this string s)
  {
    var result = s.Split([' ', ',', ';'])
        .Select(s => {
          bool parsed = IPAddress.TryParse(s, out var ipAddress);
          return new {
            IP = ipAddress,
            Parsed = parsed,
          };
        })
        .Where(p => p.Parsed)
        .Select(p => p.IP)
        .ToList()
      ;
    return result;
  }

  public static IPAddress? ToIPAddress(this string s)
  {
    IPAddress.TryParse(s, out var ipAddress);
    return ipAddress;
  }
}
