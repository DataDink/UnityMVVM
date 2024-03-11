using UnityEngine;
using UnityMVVM.Base;
using System.Collections.Generic;
using System.Collections;
using System.Linq;



#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityMVVM
{
  /// <summary>
  /// A <see cref="Binding" /> that assigns values from a model to members on the <see cref="GameObject" /> and its <see cref="Behaviour" />s.
  /// </summary>
  /// <example>
  /// <para>NOTE: This is typically configured in the Unity editor.</para>
  /// <code>
  /// var gameobject = new GameObject();
  /// var behaviour = gameobject.AddComponent&lt;MyBehaviour&gt;();
  /// var binding = gameobject.AddComponent&lt;DataBinding&gt;();
  /// binding.Assignments = new DataBinding.Assignment[] {
  ///  new DataBinding.Assignment { BindFrom = "title", BindTo = "text", Behaviour = behaviour}
  /// };
  /// binding.Bind(model);
  /// </code>
  /// </example>
  public class DataBinding : Binding, IEnumerable<DataBinding.Assignment>
  {
    /// <summary>
    /// Configures <see cref="Assignment" />s applied on <see cref="Bind(object)" />.
    /// </summary>
    /// <example>
    /// <para>NOTE: This is typically configured in the Unity editor.</para>
    /// <code>
    /// var gameobject = new GameObject();
    /// var behaviour = gameobject.AddComponent&lt;MyBehaviour&gt;();
    /// var binding = gameobject.AddComponent&lt;DataBinding&gt;();
    /// binding.Assignments = new DataBinding.Assignment[] {
    ///  new DataBinding.Assignment { BindFrom = "title", BindTo = "text", Behaviour = behaviour}
    /// };
    /// binding.Bind(model);
    /// </code>
    /// </example>
    [SerializeField] public Assignment[] Assignments;
    /// <inheritdoc />
    public override void Bind(object model)
    {
      if (Assignments == null) { return; }
      foreach (var assignment in Assignments) { 
        assignment.BindTo?.Assign(
          assignment.Behaviour == null ? gameObject : assignment.Behaviour, 
          assignment.BindFrom?.Select(model)
        ); 
      }
    }
    /// <inheritdoc />
    public IEnumerator<Assignment> GetEnumerator() => Assignments?.GetEnumerator() as IEnumerator<Assignment> ?? Enumerable.Empty<Assignment>().GetEnumerator();
    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    /// <summary>
    /// An assignment from a model value to a <see cref="GameObject" />/<see cref="UnityEngine.Behaviour" /> member.
    /// </summary>
    /// <example>
    /// <para>NOTE: This is typically configured in the Unity editor.</para>
    /// <code>
    /// var gameobject = new GameObject();
    /// var behaviour = gameobject.AddComponent&lt;MyBehaviour&gt;();
    /// var binding = gameobject.AddComponent&lt;DataBinding&gt;();
    /// binding.Assignments = new DataBinding.Assignment[] {
    ///  new DataBinding.Assignment { BindFrom = "title", BindTo = "text", Behaviour = behaviour}
    /// };
    /// binding.Bind(model);
    /// </code>
    /// </example>
    public class Assignment {
      [SerializeField] private Selector map;
      /// <summary>
      /// The model value selector.
      /// </summary>
      public Selector BindFrom { get => map; set => map = value; }
      [SerializeField] private Selector to;
      /// <summary>
      /// The <see cref="GameObject" />/<see cref="UnityEngine.Behaviour" /> value assignment.
      /// </summary>
      public Selector BindTo { get => to; set => to = value; }
      [SerializeField] private Behaviour behaviour;
      /// <summary>
      /// The optional <see cref="UnityEngine.Behaviour" /> to map to.
      /// </summary>
      /// <remarks>
      /// <para>If not set, the <see cref="GameObject" /> is used.</para>
      /// </remarks>
      public Behaviour Behaviour { get => behaviour; set => behaviour = value; }
      #if UNITY_EDITOR
      [CustomPropertyDrawer(typeof(Assignment))]
      private class Editor : InlineEditor { public Editor() : base(
        "map",
        "to",
        "behaviour"
      ) {} }
      #endif
    }
  }
}

