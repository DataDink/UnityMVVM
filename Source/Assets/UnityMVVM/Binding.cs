using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UnityMVVM
{
  public abstract class Binding : MonoBehaviour
  {
    public virtual void Bind(object data) {}

    [Serializable]
    public class Selector : IEnumerable<string>
    {
      private const string Delimiter = ".";
      private const BindingFlags MemberFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
      [SerializeField] private string _path;

      public Selector(string value) { _path = value ?? ""; }
      public Selector(string[] value) { _path = string.Join(Delimiter, (value ?? new string[0]).Select(v => v ?? "").ToArray()); }
      public override string ToString() => _path;
      public string[] ToArray() => _path.Split(Delimiter);

      public static implicit operator Selector(string value) => new(value);
      public static implicit operator Selector(string[] value) => new(value);
      public static implicit operator string(Selector value) => value.ToString();
      public static implicit operator string[](Selector value) => value.ToArray();

      public IEnumerator<string> GetEnumerator() => ((IEnumerable<string>)ToArray()).GetEnumerator();
      IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<string>)ToArray()).GetEnumerator();

      public object Select(object data) => ToArray().Aggregate(data, (value, index) =>
      {
        if (value == null) { return null; }
        if (string.IsNullOrEmpty(index)) { return value; }
        if (value is Model vm) { return vm.ContainsKey(index) ? vm[index] : null; }
        if (value is Model.Set va) { return int.TryParse(index, out var i) && i >= 0 && i < va.Count ? va[i] : null; }
        var member = value.GetType().GetMember(index, MemberFlags).FirstOrDefault();
        return member is FieldInfo field ? field.GetValue(value) :
                member is PropertyInfo property ? property.GetValue(value) :
                null;
      });
      public bool Assign(object data, object value)
      {
        var members = ToArray();
        var selector = (Selector)members.Take(members.Length - 1).ToArray();
        var model = selector.Select(data);
        if (model == null) { return false; }
        if (model is Model vm) { vm[members.Last()] = value; return true; }
        if (model is Model.Set va) { if (int.TryParse(members.Last(), out var i) && i >= 0 && i < va.Count) { va[i] = value; return true; } }
        var type = model.GetType();
        var member = type.GetMember(members.Last(), MemberFlags).FirstOrDefault();
        if (member is FieldInfo field) { field.SetValue(model, GetAssignmentValue(value, field.FieldType)); return true; }
        if (member is PropertyInfo property) { property.SetValue(model, GetAssignmentValue(value, property.PropertyType)); return true; }
        return false;
      }
      public static object GetAssignmentValue(object value, Type type)
      {
        var valueType = value?.GetType() ?? typeof(object);
        if (type.IsAssignableFrom(valueType)) { return value; }
        if (type.IsEnum && value is string @string) { return Enum.Parse(type, @string); }
        if (typeof(IConvertible).IsAssignableFrom(type) && typeof(IConvertible).IsAssignableFrom(valueType)) { return Convert.ChangeType(value, type); }
        var descriptor = TypeDescriptor.GetConverter(type);
        if (descriptor.CanConvertFrom(valueType)) { return descriptor.ConvertFrom(value); }
        if (type.IsValueType) { return Activator.CreateInstance(type); }
        return default;
      }

      [CustomPropertyDrawer(typeof(Selector))]
      private class Editor : InlineEditor { public Editor() : base(
        "_path"
      ) { } }
    }
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
  }
}