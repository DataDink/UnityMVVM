using UnityEngine;
namespace UnityMVVM.Base
{
  public abstract partial class Template
  {
    /// <summary>
    /// The <see cref="View" /> generated for a prefab instance by the <see cref="Template" />.
    /// </summary>
    /// <remarks>
    /// <para>Added to a prefab's root <see cref="GameObject" /> when binding.</para>
    /// </remarks>
    public class Instance : View 
    {
      /// <summary>
      /// Gets/Sets the <see cref="Template" />-<see cref="Binding" /> value assigned to this <see cref="View" />.
      /// </summary>
      public object TemplateBinding;
      /// <inheritdoc />
      public override void Bind(object model) => base.Bind(TemplateBinding);
    }
  }
}