using System;
using UnityEngine;
using UnityMVVM.Base;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityMVVM
{
  /// <summary>
  /// A <see cref="Binding" /> that maps values to members of <see cref="Behaviour" />s on the <see cref="GameObject" />.
  /// </summary>
  [Serializable]
  public class BehaviourBindings : Binding
  {
    /// <summary>
    /// The <see cref="BehaviourMap" />s to applied on <see cref="Bind(object)" />.
    /// </summary>
    [SerializeField] public BehaviourMap[] Bindings;
    /// <inheritdoc />
    public override void Bind(object model)
    {
      foreach (var map in Bindings ?? new BehaviourMap[0]) { 
        map.Target?.Assign(map.Behaviour, map.Source?.Select(model)); 
      }
    }
    /// <summary>
    /// A mapping from a model member to a <see cref="Behaviour" /> member.
    /// </summary>
    [Serializable]
    public class BehaviourMap {
      [SerializeField] private Selector map;
      /// <summary>
      /// Selects a member from the model.
      /// </summary>
      public Selector Source { get => map; set => map = value; }
      [SerializeField] private Selector to;
      /// <summary>
      /// Assigns a member on the <see cref="Behaviour" />.
      /// </summary>
      public Selector Target { get => to; set => to = value; }
      [SerializeField] private Behaviour behaviour;
      /// <summary>
      /// The <see cref="Behaviour" /> to map to.
      /// </summary>
      public Behaviour Behaviour { get => behaviour; set => behaviour = value; }
      #if UNITY_EDITOR
      
      [CustomPropertyDrawer(typeof(BehaviourMap))]
      private class Editor : InlineEditor { public Editor() : base(
        "map",
        "to",
        "behaviour"
      ) {} }
      #endif
    }
  }
}

