using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityMVVM.Unity
{
  [Serializable]
  public class GameObjectBinding : Binding
  {
    [SerializeField] public Map[] Bindings;
    public override void Bind(object data)
    {
      foreach (var map in Bindings ?? new Map[0])
      {
        map.Target?.Assign(gameObject, map.Source?.Select(data));
      }
      base.Bind(data);
    }

    [Serializable]
    public class Map
    {
      [SerializeField] public Selector Source;
      [SerializeField] public Selector Target;

      [CustomPropertyDrawer(typeof(Map))]
      private class Editor : InlineEditor { public Editor() : base(new Dictionary<string, string> {
        { "Source", "map" },
        { "Target", "to" }
      }) { } }
    }
  }
}