using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityMVVM
{
  [Serializable]
  public class TransformBinding : Binding
  {
    [SerializeField] public VectorMap Position;
    [SerializeField] public VectorMap Rotation;
    [SerializeField] public VectorMap Scale;
    public override void Bind(object data)
    {
      if (Position != null) { transform.position = Position.Select(data); }
      if (Rotation != null) { transform.rotation = Quaternion.Euler(Rotation.Select(data)); }
      if (Scale != null) { transform.localScale = Scale.Select(data, new Vector3(1,1,1)); }
      base.Bind(data);
    }

    [Serializable]
    public class VectorMap {
      [SerializeField] public Selector X;
      [SerializeField] public Selector Y;
      [SerializeField] public Selector Z;
      public Vector3 Select(object data, Vector3 fallback = default) {
        var x = X?.Select(data);
        var y = Y?.Select(data);
        var z = Z?.Select(data);
        return new Vector3(
          (float)Selector.GetAssignmentValue(x ?? fallback.x, typeof(float)),
          (float)Selector.GetAssignmentValue(y ?? fallback.y, typeof(float)),
          (float)Selector.GetAssignmentValue(z ?? fallback.z, typeof(float))
        );
      }

      [CustomPropertyDrawer(typeof(VectorMap))]
      private class Editor : InlineEditor { public Editor() : base(
        "X",
        "Y",
        "Z"
      ) { } }
    }
  }
}
