using System;
using UnityEngine;
using UnityMVVM.Base;

namespace UnityMVVM 
{
  /// <summary>
  /// A <see cref="Template"> that instantiates a static prefab by name.
  /// </summary>
  /// <example>
  /// <para>NOTE: This is typically configured in the Unity editor.</para>
  /// <code>
  /// var gameobject = new GameObject();
  /// var template = gameobject.AddComponent&lt;PrefabTemplate&gt;();
  /// template.Name = "prefab";
  /// template.Binding = "model.items";
  /// template.Bind(model);
  /// </code>
  /// </example>
  public class PrefabTemplate : Template
  {
    /// <summary>
    /// Selector for the local prefab name.
    /// </summary>
    [SerializeField] Selector Name;
    /// <summary>
    /// Selector for the model value to be templated.
    /// </summary>
    /// <remarks>
    /// <para>If the model or selected member is an <see cref="IViewList"/> the value will be itterated into multiple prefab instances.</para>
    /// </remarks>
    [SerializeField] Selector Binding;
    /// <inheritdoc />
    public override void Bind(object model) { base.Bind(Binding == null ? model : Binding.Select(model)); }
    /// <inheritdoc />
    protected override GameObject Instantiate(object model, GameObject recycled)
    {
      var name = Name?.Select(model)?.ToString() ?? Name;
      if (model == null || string.IsNullOrWhiteSpace(name)) { return CreateGenericPrefab(name); }
      if (recycled != null && recycled.GetComponent<PrefabInfo>()?.ResourceName == name) { return recycled; }
      var info = Instantiate(Resources.Load<GameObject>(name) ?? CreateGenericPrefab(name)).AddComponent<PrefabInfo>();
      info.ResourceName = name;
      return info.gameObject;
    }
    private GameObject CreateGenericPrefab(string name) => new($"{Binding ?? "Source"}->{name ?? Name ?? "Name"}");
    private class PrefabInfo : MonoBehaviour { public string ResourceName; }
  }
}