using System;
using UnityEngine;
using UnityMVVM.Base;

namespace UnityMVVM 
{
  /// <summary>
  /// A <see cref="Template"> that instantiates a static prefab by name.
  /// </summary>
  [Serializable]
  public class NamedPrefabTemplate : Template
  {
    /// <summary>
    /// Selects the name of the prefab to be instantiated from each model instance.
    /// </summary>
    [SerializeField] Selector Name;
    /// <summary>
    /// If not null, the member selected from the model when binding.
    /// </summary>
    /// <remarks>
    ///   <para>If the model or selected member is an <see cref="Binding.ISet"/> the value will be intterated into multiple prefab instances.</para>
    /// </remarks>
    [SerializeField] Selector Source;
    /// <inheritdoc />
    public override void Bind(object model) { base.Bind(Source == null ? model : Source.Select(model)); }
    /// <inheritdoc />
    protected override GameObject Instantiate(object model, GameObject recycled = null)
    {
      var name = Name?.Select(model)?.ToString();
      if (model == null || name == null) { return CreateGenericPrefab(name); }
      if (recycled != null && recycled.GetComponent<PrefabInfo>()?.ResourceName == name) { return recycled; }
      var instance = GameObject.Instantiate(Resources.Load<GameObject>(name) ?? CreateGenericPrefab(name)).AddComponent<PrefabInfo>();
      instance.ResourceName = name;
      return instance.gameObject;
    }
    private GameObject CreateGenericPrefab(string name) => new GameObject($"{Source ?? "Source"}->{name ?? Name ?? "Name"}");
    private class PrefabInfo : MonoBehaviour { public string ResourceName; }
  }
}