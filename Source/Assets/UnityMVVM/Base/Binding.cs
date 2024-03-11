using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityMVVM.Base
{
  /// <summary>
  /// A common base that defines the standard interfacing and functionality for all Unity data bindings.
  /// </summary>
  /// <remarks>
  /// <para>A <see cref="Binding" /> defines a relationship between a view-element and a data-element.</para>
  /// <para>Subclasses of <see cref="Binding" /> are one-way bindings that map a value from a model to a view or vice versa.</para>
  /// <para>Responsive designs will bind handlers to events in order to respond to changes in the model or interactions with the view.</para>
  /// </remarks>
  /// <example>
  ///  <code>
  ///  public class MyBinding : Binding
  ///  {
  ///    [SerializeField] private Selector Binding;
  ///    public override void Bind(object model) => this.value = Binding.Select(model);
  ///  }
  ///  </code>
  /// </example>   
  public abstract partial class Binding : MonoBehaviour
  {
    /// <summary>
    /// Calling this method executes this binding.
    /// </summary>
    /// <param name="model">The model to be used for binding.</param>
    /// <remarks>
    /// <para><see cref="Bind(object)" /> should be called each time an update is required.</para>
    /// <para>Responsive designs will bind handlers to events in order to respond to changes in the model or interactions with the view.</para>
    /// </remarks>
    /// <example>
    ///  <code>
    ///  var binding = GetComponent&lt;Binding&gt;();
    ///  binding.Bind(model);
    ///  </code>
    /// </example>
    public abstract void Bind(object model);
    #if UNITY_EDITOR
    /// <summary>
    /// An experimental base property drawer used by bindings to display multiple fields horizontally in the designer.
    /// </summary>
    /// <remarks>
    /// <para>Caution: This is experimental and may not work as intended.</para>
    /// </remarks>
    /// <example>
    ///  <code>
    ///   public class MyBinding : Binding {
    ///    [SerializeField] private valueA;
    ///    [SerializeField] private valueB;
    ///    [SerializeField] private valueC;
    ///    #if UNITY_EDITOR
    ///    [CustomPropertyDrawer(typeof(MyBinding))]
    ///    private class MyEditor : InlineEditor { public MyEditor() : base(
    ///     "valueA",
    ///     "valueB",
    ///     "valueC"
    ///    ) {} }
    ///    #endif
    ///   }
    ///  </code>
    /// </example> 
    protected abstract class InlineEditor : PropertyDrawer
    {
      /// <summary>
      /// The ordered fields to be exposed to the designer.
      /// </summary>
      private readonly string[] _fields;
      /// <summary>
      /// Creates a custom inline-designer that displays multiple fields horizontally when configured in a designer array.
      /// </summary>
      /// <param name="fields">The ordered fields the designer should expose.</param>
      /// <example>
      ///  <code>
      ///   public class MyBinding : Binding {
      ///    [SerializeField] private valueA;
      ///    [SerializeField] private valueB;
      ///    [SerializeField] private valueC;
      ///    #if UNITY_EDITOR
      ///    [CustomPropertyDrawer(typeof(MyBinding))]
      ///    private class MyEditor : InlineEditor { public MyEditor() : base(
      ///     "valueA",
      ///     "valueB",
      ///     "valueC"
      ///    ) {} }
      ///    #endif
      ///   }
      ///  </code>
      /// </example> 
      protected InlineEditor(params string[] fields) { _fields = fields.ToArray(); }
      /// <inheritdoc />
      public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
      {
        var index = 0;
        var title = "  " + (string.IsNullOrWhiteSpace(label.text) ? property.displayName : label.text);
        var titleWidth = GUI.skin.label.CalcSize(new GUIContent(title)).x;
        var fields = _fields.Select(name => property.FindPropertyRelative(name)).ToArray();
        var fieldWidth = (position.width - titleWidth) / fields.Length;
        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        var titleRect = new Rect(position.x, position.y, titleWidth, position.height);
        EditorGUI.LabelField(titleRect, title);
        foreach (var field in fields)
        {
          var rect = new Rect(position.x + titleWidth + fieldWidth * index++, position.y, fieldWidth, position.height);
          EditorGUI.PropertyField(rect, field, GUIContent.none);
        }
        EditorGUI.indentLevel = indent;
      }
    }
    #endif
    /// <summary>
    /// Enumerates a single value.
    /// </summary>
    /// <param name="item">The value to yield.</param>
    /// <typeparam name="T">The value's generic type</typeparam>
    /// <returns>Yields the value as an <see cref="IEnumerable{T}" />.</returns>
    /// <remarks>
    /// <para>CAUTION: This is experimental and may be removed in the future.</para>
    /// <para>Can be used to avoid creating an array when yielding a single value.</para>
    /// </remarks>
    protected static IEnumerable<T> Yield<T>(T item) { yield return item; } // TODO: Compare benchmarks with .Union(new[] { item })
  }
}