using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityMVVM.Base
{
  /// <summary>
  /// A common base that defines the standard interfacing and functionality for all Unity view-bindings.
  /// </summary>
  /// <remarks>
  /// <para>A <see cref="View" /> is a binding that is responsible for managing a scope of descendant <see cref="Binding" />s.</para>
  /// <para>NOTE: Only one view should be attached to a single <see cref="GameObject" />.</para>
  /// </remarks>
  /// <example>
  /// <code>
  /// public class MyView : View
  /// {
  ///   [SerializeField] public Selector Binding;
  ///   public override void Bind(object model) => base.Bind(Binding.Select(model));
  /// }
  /// var view = gameObject.AddComponent<MyView>();
  /// view.ModelSelector = "accounts.active.profile";
  /// view.Bind(application);
  /// </code> 
  /// </example>
  public abstract class View : Binding, IEnumerable<Binding>
  {
    /// <summary>
    /// An established scope of <see cref="Binding" />s this view is responsible for.
    /// </summary>
    private Binding[] _scope = new Binding[0];
    /// <summary>
    /// The established ancestor view.
    /// </ summary>
    private View _parent;
    /// <summary>
    /// The established ancestor view.
    /// </ summary>
    public View Parent => _parent;
    /// <inheritdoc />
    public override void Bind(object model) { foreach (var binding in _scope) { binding.Bind(model); } }
    /// <inheritdoc />
    public IEnumerator<Binding> GetEnumerator() => _scope.AsEnumerable().GetEnumerator();
    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => _scope.GetEnumerator();
    /// <summary>
    /// Triggers view management tasks.
    /// </summary>
    /// <remarks>
    /// <para>NOTE: When overridden, this base method should be called first.</para>
    /// </remarks>
    protected virtual void OnTransformParentChanged() 
    {
      _parent?.Detatch(this);
      (_parent = transform.parent?.GetComponentInParent<View>())?.Branch(this);
    }
    /// <summary>
    /// Triggers view management tasks.
    /// </summary>
    /// <remarks>
    /// <para>NOTE: When overridden, this base method should be called first.</para>
    /// </remarks>
    protected virtual void Awake() 
    {
      InitializeScope();
      (_parent = transform.parent?.GetComponentInParent<View>())?.Branch(this);
    }
    /// <summary>
    /// Scans the <see cref="View" />s descendants for <see cref="Binding" />s and initializes the scope.
    /// </summary>
    /// <param name="view">The view to be configured.</param>
    /// <returns>The new scope of this <see cref="View" /></returns>
    protected IEnumerable<Binding> InitializeScope() 
    {
      var descendants = GetComponentsInChildren<Binding>(includeInactive: true).Where(v => v != this);
      return _scope = descendants.Except(descendants.OfType<View>().SelectMany(v => v)).ToArray();
    }
    /// <summary>
    /// Removes the branch and its scope from this <see cref="View" />.
    /// </summary>
    /// <param name="branch">The <see cref="View" /> to remove.</param>
    /// <returns>The new scope of this <see cref="View" /></returns>
    protected IEnumerable<Binding> Detatch(View branch) => _scope = _scope.Except(Yield(branch)).ToArray();
    /// <summary>
    /// Removes the branch's scope and adds the branch to this <see cref="View" />s scope.
    /// </summary>
    /// <param name="branch">The <see cref="View" /> to branch</param>
    /// <returns>The new scope of this <see cref="View" /></returns>
    protected IEnumerable<Binding> Branch(View branch) => _scope = _scope.Except(branch).Union(Yield(branch)).ToArray();
  }
}