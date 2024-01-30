using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UnityMVVM.Unity
{
  public abstract class Binding : MonoBehaviour
  {
    public virtual void Bind(object data) { }

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
      private object GetAssignmentValue(object value, Type type)
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
      private class Editor : InlineEditor { public Editor() : base(new Dictionary<string, string> {
        { "_path", null }
      }) { } }
    }
    protected abstract class InlineEditor : PropertyDrawer
    {
      private readonly Dictionary<string, string> _fields;
      protected InlineEditor(Dictionary<string, string> fields) { _fields = fields.ToDictionary(p => p.Key, p => p.Value); }
      public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
      {
        var fields = _fields.ToDictionary(p => p.Key, p => property.FindPropertyRelative(p.Key));
        var width = position.width / fields.Count;
        var index = 0;
        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        foreach (var pair in fields)
        {
          var name = _fields[pair.Key] == null ? label.text : _fields[pair.Key];
          var tag = string.IsNullOrWhiteSpace(name) ? name : index > 0 ? $"  {name} " : $"{name} ";
          var rect = new Rect(position.x + width * index++, position.y, width, position.height);
          var labelWidth = GUI.skin.label.CalcSize(new GUIContent(tag)).x;
          var labelRect = new Rect(rect.x, rect.y, labelWidth, rect.height);
          var inputRect = new Rect(rect.x + labelWidth, rect.y, rect.width - labelWidth, rect.height);
          EditorGUI.LabelField(labelRect, tag);
          EditorGUI.PropertyField(inputRect, pair.Value, GUIContent.none);
        }
        EditorGUI.indentLevel = indent;
      }
    }
  }
}