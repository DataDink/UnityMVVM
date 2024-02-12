using System;
using UnityEngine;
using UnityMVVM.Base;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityMVVM
{
  /// <summary>
  /// A <see cref="Binding" /> that maps values to members of the <see cref="GameObject" />.
  /// </summary>
  [Serializable]
  public class GameObjectBindings : Binding
  {
    /// <summary>
    /// The <see cref="GameObjectMap" />s to applied on <see cref="Bind(object)" />.
    /// </summary>
    [SerializeField] public GameObjectMap[] Bindings;
    /// <inheritdoc />
    public override void Bind(object model)
    {
      foreach (var map in Bindings ?? new GameObjectMap[0]) {
        map.Target?.Assign(gameObject, map.Source?.Select(model));
      }
    }
    /// <summary>
    /// A mapping from a model member to a <see cref="GameObject" /> member.
    /// </summary>
    [Serializable]
    public class GameObjectMap
    {
      [SerializeField] public Selector map;
      /// <summary>
      /// Selects a member from the model.
      /// </summary>
      public Selector Source { get => map; set => map = value; }
      [SerializeField] public Selector to;
      /// <summary>
      /// Assigns a member on the <see cref="GameObject" />.
      /// </summary> 
      public Selector Target { get => to; set => to = value; }
      #if UNITY_EDITOR
      [CustomPropertyDrawer(typeof(GameObjectMap))]
      private class Editor : InlineEditor { public Editor() : base(
        "map",
        "to"
      ) { } }
      #endif
    }
  }
}