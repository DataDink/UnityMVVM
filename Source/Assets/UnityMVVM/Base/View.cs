using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityMVVM.Base
{
  /// <summary>
  /// A common base that defines the standard interfacing and functionality for all Unity view-bindings.
  /// </summary>
  /// <remarks>
  /// <para>A <see cref="View" /> is a binding that (re)scopes a <see cref="GameObject" /> and all of its descendants.</para>
  /// <para>NOTE: Only one view should be attached to a single <see cref="GameObject" />.</para>
  /// </remarks>
  public abstract class View : Binding
  {
    /// <summary>
    /// When overridden, selects the model to be bound to this view from the given model structure.
    /// </summary>
    protected virtual object Select(object model) => model;
    /// <inheritdoc />
    /// <remarks>
    ///   NOTE: A <see cref="View" /> binding a descendant <see cref="View" /> will not call this method for performance reasons.
    /// </remarks>
    public override void Bind(object model) => ViewBind(this, Select(model), transform);
    /// <summary>
    /// Traverses and executes descendant bindings.
    /// </summary>
    /// <param name="view">The owning view for the current binding scope.</param>
    /// <param name="model">The current model structure being bound</param>
    /// <param name="node">The node being traversed.</param>
    /// <param name="performanceCache">A performance cache containing a pool of bindings to be removed when they are handled</param>
    /// <remarks>
    ///   NOTE: A <see cref="View" /> binding a descendant <see cref="View" /> will call this method without calling <see cref="Bind(object)" /> for performance reasons. 
    /// </remarks>
    protected virtual void ViewBind(View view, object model, Transform node, Dictionary<Transform, Binding[]> performanceCache = null)
    {
      // TODO: Performance pass - Find a way to cache gameobject hierarchy and stay in sync with fewer calls to GetComponentsInChildren
      if ((performanceCache??=GetComponentsInChildren<Binding>(includeInactive: true).GroupBy(b => b?.transform).ToDictionary(g => g.Key, g => g.ToArray()))
        .TryGetValue(node, out var bindings)) 
      { 
        var notme = bindings.Where(b => b != view).ToArray();
        var branch = notme.OfType<View>().FirstOrDefault();
        if (branch != null) { branch.ViewBind(branch, branch.Select(model), node, performanceCache); return; }
        foreach (var binding in notme) { binding.Bind(model); } 
        performanceCache.Remove(node);
      }
      foreach (var child in node.transform.Cast<Transform>().ToArray()) { ViewBind(view, model, child, performanceCache); } 
    }
  }
}