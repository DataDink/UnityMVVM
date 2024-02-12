using System;
using System.Collections.Generic;
using UnityEngine;
using UnityMVVM.Base;

namespace UnityMVVM
{
  /// <summary>
  /// A <see cref="View"> that scopes this <see cref="GameObject"> to a descendant model using a <see cref="Selector">.
  /// </summary>
  [Serializable]
  public class ViewBinding : View
  {
    /// <summary>
    /// The <see cref="Selector"> that selects the model to be bound to this <see cref="View">.
    /// </summary>
    [SerializeField] public Selector Source;
    /// <inheritdoc />
    protected override object Select(object model) { return Source == null ? model : Source.Select(model); }
  }
}