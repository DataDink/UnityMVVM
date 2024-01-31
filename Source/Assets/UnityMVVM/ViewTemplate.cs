using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityMVVM
{
  [Serializable]
  public class Template : View, IEnumerable<View>
  {
    private readonly List<Child> _children = new();
    [SerializeField] public Selector Source;
    [SerializeField] private Selector Prefab;

    protected override IEnumerable<Binding> Populate(object data)
    {
      data = string.IsNullOrEmpty(Source) ? data : Source.Select(data);
      var values = data is Model.Set array ? array.ToArray() : new[] { data };
      for (var i = 0; i < values.Length; i++) {
        var value = values[i];
        var prefab = Prefab.Select(value).ToString();
        if (prefab == null) { continue; }
        var existing = _children.ElementAtOrDefault(i);
        var child = existing?.Prefab == prefab ? existing : Instantiate(Resources.Load<GameObject>(prefab), transform).AddComponent<Child>();
        if (child != existing && existing != null) { Destroy(existing.gameObject); }
        child.Prefab = prefab;
        if (i < _children.Count) { _children[i] = child; } else { _children.Add(child); }
      }
      if (_children.Count > values.Length) {
        var extra = _children.Count - values.Length;
        var trash = _children.GetRange(values.Length, extra);
        _children.RemoveRange(values.Length, extra);
        foreach (var child in trash) { Destroy(child.gameObject); }
      }
      var scope = _children.SelectMany(child => child.PopulateChild(values[_children.IndexOf(child)]));
      return scope.ToArray();
    }
    private class Child : View { 
      private string _prefab;
      public string Prefab { get => _prefab; set => _prefab = _prefab ?? value; }
      public IEnumerable<Binding> PopulateChild(object data) => Populate(data); 
    }
    public IEnumerator<View> GetEnumerator() => _children.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
  }
}