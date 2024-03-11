using System;
using UnityEngine;
using UnityMVVM.Base;

namespace UnityMVVM
{
  /// <summary>
  /// A <see cref="View"> that scopes this <see cref="GameObject"> to a descendant model using a <see cref="Binding.Selector">.
  /// </summary>
  /// <example>
  /// <para>NOTE: This is typically configured in the Unity editor.</para>
  /// <code>
  /// var gameobject = new GameObject();
  /// var view = gameobject.AddComponent&lt;ViewBinding&gt;();
  /// view.Binding = "model.application.main";
  /// view.Bind(model);
  /// </code>
  /// </example>
  public class ViewBinding : View
  {
    /// <summary>
    /// The <see cref="Binding.Selector"> that selects the model to be bound to this <see cref="View">.
    /// </summary>
    /// <example>
    /// <para>NOTE: This is typically configured in the Unity editor.</para>
    /// <code>
    /// var gameobject = new GameObject();
    /// var view = gameobject.AddComponent&lt;ViewBinding&gt;();
    /// view.Binding = "model.application.main";
    /// view.Bind(model);
    /// </code>
    /// </example>
    [SerializeField] public Selector Binding;
    /// <inheritdoc />
    public override void Bind(object model) { base.Bind(Binding.Select(model)); }
  }
}