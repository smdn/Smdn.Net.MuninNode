using System.Linq;
using System.Net;

namespace Smdn.Net.MuninNode.AccessRules;

public sealed class AccessRuleFromConfig(MuninConfiguration config) : IAccessRule {
  public bool IsAcceptable(IPEndPoint remoteEndPoint) =>
    config.AllowFrom.Any(
      ip => ip.ToString() == remoteEndPoint.Address.ToString());
}
