using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
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
  public abstract class Binding : MonoBehaviour
  {
    /// <summary>
    /// Calling this method executes this binding.
    /// </summary>
    /// <param name="model">The model to be used for binding.</param>
    /// <remarks>
    /// <para><see cref="Bind(object)" /> should be called each time an update is required.</para>
    /// <para>Responsive designs will bind handlers to events in order to respond to changes in the model or interactions with the view.</para>
    public abstract void Bind(object model);
    /// <summary>
    /// Represents an object that should be explicitly treated as a model.
    /// </summary>
    public interface IModel : IDictionary<string, object> { }
    /// <summary>
    /// Represents an enumerable that should be explicitly treated as a collection of models.
    /// </summary>
    public interface ISet : IList<object> { } 

    /// <summary>
    /// Specifies a path to a member of a hierarchical data structure.
    /// </summary>
    /// <remarks>
    /// <para>Can be used interchangeably with delimited-<see cref="string" />s and <see cref="string[]" />s</para>
    /// <para>Recognizes <see cref="IModel" />s and <see cref="ISet" />s and treats them accordingly.</para>
    /// </remarks>
    /// <example>
    ///   <code>
    ///   Selector selector = "foo.bar.baz";
    ///   var members = selector.ToArray();
    ///   var oldValue = selector.Select(model);
    ///   selector.Assign(model, newValue);
    ///   </code>
    /// </example>
    [Serializable]
    public class Selector : IEnumerable<string>
    {
      private const string Delimiter = "."; // NOTE: This might be made configurable in the future.
      private const BindingFlags MemberFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
      /// <summary>
      /// The value that will be cached in your Unity project.
      /// </summary>
      [SerializeField] private string Path;
      /// <summary> 
      /// The number of members in this <see cref="Selector" />.
      /// </summary>
      public int Length => ToArray().Length;
      /// <summary>
      /// The model portion of this <see cref="Selector" />.
      /// </summary>
      public Selector Model => new(ToArray().SkipLast(1).ToArray());
      /// <summary>
      /// The index portion of this <see cref="Selector" />.
      /// </summary>
      public Selector Index => new(ToArray().LastOrDefault());
      /// <summary>
      /// Creates a new <see cref="Selector" /> from a delimited <see cref="string" />.
      /// </summary>
      /// <param name="value">The delimited string.</param>
      public Selector(string value) { Path = value ?? ""; }
      /// <summary>
      /// Creates a new <see cref="Selector" /> from an array of member names.
      /// </summary>
      /// <param name="value">The array of member names.</param> 
      public Selector(string[] value) { Path = string.Join(Delimiter, (value ?? new string[0]).Select(v => v ?? "").ToArray()); }
      /// <summary>
      /// The delimited <see cref="string" /> representation of this <see cref="Selector" />.
      /// </summary>
      public override string ToString() => Path;
      /// <summary>
      /// The array of member names of this <see cref="Selector" />.
      /// </summary>
      public string[] ToArray() => Path.Split(Delimiter);

      public static implicit operator Selector(string value) => new(value);
      public static implicit operator Selector(string[] value) => new(value);
      public static implicit operator string(Selector value) => value.ToString();
      public static implicit operator string[](Selector value) => value.ToArray();
      /// <inheritdoc />
      public IEnumerator<string> GetEnumerator() => ((IEnumerable<string>)ToArray()).GetEnumerator();
      /// <inheritdoc />
      IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<string>)ToArray()).GetEnumerator();
      /// <summary>
      /// Executes this <see cref="Selector" /> on the given model and returns the referenced value or <see cref="null" />.
      /// </summary>
      /// <param name="model">The model to be traversed.</param>
      /// <returns>
      /// If the path exists, the referenced value. Otherwise, <see cref="null" />.
      /// </returns>
      public object Select(object model) => ToArray().Aggregate(model, (value, index) =>
      {
        if (value == null) { return null; }
        if (string.IsNullOrEmpty(index)) { return value; }
        if (value is IModel vm) { return vm.ContainsKey(index) ? vm[index] : null; }
        if (value is ISet va) { return int.TryParse(index, out var i) && i >= 0 && i < va.Count ? va[i] : null; }
        var member = value.GetType().GetMember(index, MemberFlags).FirstOrDefault();
        return member is FieldInfo field ? field.GetValue(value) :
                member is PropertyInfo property ? property.GetValue(value) :
                null;
      });
      /// <summary>
      /// If the path exists; assigns the given value to the member referenced by the path from the given data structure.
      /// </summary>
      /// <param name="model">The data structure to be traversed.</param>
      /// <param name="value">The value to be assigned</param>
      /// <returns>A <see cref="boolean" /> indicating if the path was found and the value could be assigned.</returns>
      public bool Assign(object model, object value)
      {
        model = Model.Select(model);
        var index = Index;
        if (model == null) { return false; }
        if (model is IModel vm) { vm[index] = value; return true; }
        if (model is ISet va) { if (int.TryParse(index, out var i) && i >= 0 && i < va.Count) { va[i] = value; return true; } return false; }
        var type = model.GetType();
        var member = type.GetMember(index, MemberFlags).FirstOrDefault();
        if (member is FieldInfo field && !field.IsInitOnly) { field.SetValue(model, GetAssignmentValue(value, field.FieldType)); return true; }
        if (member is PropertyInfo property && property.CanWrite) { property.SetValue(model, GetAssignmentValue(value, property.PropertyType)); return true; }
        return false;
      }
      /// <summary>
      /// Attempts to cast, map or convert the given value to the given type.
      /// </summary>
      /// <param name="value">The value to be resolved</param>
      /// <param name="type">The type to resolve</param>
      /// <returns>
      /// <para>If a cast, map or conversion is possible, the resolved value. Otherwise, <see cref="default" />.</para>
      /// </returns>
      /// <remarks>
      /// <para>Custom conversions can be configured via <see cref="TypeConverterAttribute" />s.</para>
      /// <para>See <see cref="TypeDescriptor.AddAttributes(Type, params Attribute[])" /> for more information.</para>
      /// </remarks>
      public static object GetAssignmentValue(object value, Type type)
      {
        if (value == null) { return type.IsValueType ? Activator.CreateInstance(type) : null; }
        var fromType = value.GetType();
        if (type.IsAssignableFrom(fromType)) { return value; }
        if (value is IModel m) { return MapAssignmentValue(m, type); }
        try {
          var descriptor = TypeDescriptor.GetConverter(type);
          if (descriptor?.CanConvertFrom(fromType) == true) { return descriptor.ConvertFrom(value); }
        } catch (Exception ex) { Debug.Log(new TypeConversionException(value, fromType, type, ex)); }
        if (type.IsValueType) { return Activator.CreateInstance(type); }
        return default;
      }
      /// <summary>
      /// Maps the <see cref="IModel" /> to a new instance of the type.
      /// </summary>
      /// <param name="value">The model to be resolved</param>
      /// <param name="type">The type to resolve</param>
      /// <returns></returns>
      public static object MapAssignmentValue(IModel value, Type type)
      {
        if (type.GetConstructor(new Type[0]) == null) { return default; }
        var instance = Activator.CreateInstance(type);
        foreach (var member in type.GetMembers(MemberFlags)) {
          if (!value.ContainsKey(member.Name)) { continue; }
          var modelValue = value[member.Name];
          if (member is FieldInfo field && !field.IsInitOnly) { field.SetValue(instance, GetAssignmentValue(modelValue, field.FieldType)); }
          if (member is PropertyInfo property && property.CanWrite) { property.SetValue(instance, GetAssignmentValue(modelValue, property.PropertyType)); }
        }
        return instance;
      }
      private class TypeConversionException : Exception { public TypeConversionException(object value, Type from, Type to, Exception inner) : base($"Failed to convert `{value}` from {from} to {to}.", inner) { } }

      #if UNITY_EDITOR
      [CustomPropertyDrawer(typeof(Selector))]
      private class Editor : InlineEditor { public Editor() : base(
        "Path"
      ) { } }
      #endif
    }
    #if UNITY_EDITOR
    /// <summary>
    /// An experimental base property drawer used by bindings to display multiple fields horizontally in the designer.
    /// </summary>
    /// <remarks>
    /// Caution: This is experimental and may not work as intended.
    /// </remarks>
    protected abstract class InlineEditor : PropertyDrawer
    {
      private readonly string[] _fields;
      protected InlineEditor(params string[] fields) { _fields = fields.ToArray(); }
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
  }
}