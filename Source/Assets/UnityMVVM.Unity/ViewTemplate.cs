using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityMVVM.Unity
{
  [Serializable]
  public class Template : View, IEnumerable<View>
  {
    private readonly Stack<Child> _children = new();
    [SerializeField] public Selector Source;
    [SerializeField] public GameObject Prefab;
    protected override IEnumerable<Binding> Populate(object data)
    {
      var values = new Queue<object>(data is Model.Set array ? array.ToArray() : new[] { data });
      var scope = new List<Binding>();
      while (_children.Count > values.Count) { Destroy(_children.Pop()); }
      foreach (var child in _children) { scope.AddRange(child.PopulateChild(values.Dequeue())); }
      while (values.Any())
      {
        var child = Instantiate(Prefab, transform).AddComponent<Child>();
        scope.AddRange(child.PopulateChild(values.Dequeue()));
        _children.Push(child);
        child.transform.SetParent(transform);
      }
      foreach (var binding in GetComponents<Binding>().Where(b => !(b is View)))
      {
        scope.Add(binding);
        binding.Bind(data);
      }
      return scope.ToArray();
    }
    private class Child : View { public IEnumerable<Binding> PopulateChild(object data) => Populate(data); }
    public IEnumerator<View> GetEnumerator() => _children.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    [CustomPropertyDrawer(typeof(ViewBinding))]
    private class Editor : InlineEditor { public Editor() : base(new Dictionary<string, string> {
      { "Source", "map" },
      { "Prefab", "to" }
    }) { } }
  }
}