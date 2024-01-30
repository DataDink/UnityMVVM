using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityMVVM.Unity
{
  [Serializable]
  public class ViewBinding : View
  {
    [SerializeField] public Selector Source;
    protected override IEnumerable<Binding> Populate(object data) { return base.Populate(Source == null ? data : Source.Select(data)); }
  }
}