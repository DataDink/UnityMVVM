using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityMVVM
{

  [Serializable]
  public class BehaviourBinding : Binding
  {
    [SerializeField] public Map[] Bindings;
    public override void Bind(object data)
    {
      foreach (var map in Bindings ?? new Map[0]) { 
        map.Target?.Assign(map.Behaviour, map.Source?.Select(data)); 
      }
      base.Bind(data);
    }

    [Serializable]
    public class Map {
      [SerializeField] private Selector map;
      public Selector Source { get => map; set => map = value; }
      [SerializeField] private Selector to;
      public Selector Target { get => to; set => to = value; }
      [SerializeField] private Behaviour behaviour;
      public Behaviour Behaviour { get => behaviour; set => behaviour = value; }
      
      [CustomPropertyDrawer(typeof(Map))]
      private class Editor : InlineEditor { public Editor() : base(
        "map",
        "to",
        "behaviour"
      ) {} }
    }
  }
}

