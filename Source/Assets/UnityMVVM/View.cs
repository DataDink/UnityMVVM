using System.Collections.Generic;
using System.Linq;

namespace UnityMVVM
{
  public abstract class View : Binding
  {
    public override void Bind(object data) { Populate(data); base.Bind(data); }
    protected virtual IEnumerable<Binding> Populate(object data)
    {
      var scope = GetComponentsInChildren<Binding>(includeInactive: true).Where(b => b != this).ToArray();
      var auxiliary = scope
        .OfType<View>()
        .SelectMany(b => b.Populate(data))
        .ToArray();
      foreach (var binding in scope.Except(auxiliary)) { binding.Bind(data); }
      return new[] { this }.Concat(scope);
    }
  }
}