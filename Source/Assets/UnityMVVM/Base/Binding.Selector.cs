using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Globalization;



#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityMVVM.Base
{
  public abstract partial class Binding
  {
    /// <summary>
    /// Specifies a path to a member of a hierarchical data structure.
    /// </summary>
    /// <remarks>
    /// <para>Can be used interchangeably with delimited-<see cref="string" />s and <see cref="string[]" />s</para>
    /// <para>Recognizes <see cref="IViewModel" />s and <see cref="IViewList" />s and treats them accordingly.</para>
    /// </remarks>
    /// <example>
    ///   <code>
    ///   Binding.Selector selector = "foo.bar.baz";
    ///   var members = selector.ToArray();
    ///   var oldValue = selector.Select(model);
    ///   selector.Assign(model, newValue);
    ///   </code>
    /// </example>
    [Serializable] public class Selector : IEnumerable<string>
    {
      private const string Delimiter = "."; // NOTE: This might be made configurable in the future.
      private const BindingFlags MemberFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
      /// <summary>
      /// The value that will be cached by Unity.
      /// </summary>
      [SerializeField] private string Path;
      /// <summary> 
      /// The number of members in this <see cref="Selector" />.
      /// </summary>
      /// <example>
      ///  <code>
      ///  Binding.Selector selector = "foo.bar.baz";
      ///  var length = selector.Length; // 3
      ///  </code>
      /// </example>
      public int Length => ToArray().Length;
      /// <summary>
      /// The model portion of this <see cref="Selector" />.
      /// </summary>
      /// <example>
      ///  <code>
      ///  Binding.Selector selector = "foo.bar.baz";
      ///  var modelPath = selector.Model; // "foo.bar"
      ///  </code>
      /// </example>
      public Selector Model => new(ToArray().SkipLast(1).ToArray());
      /// <summary>
      /// The index portion of this <see cref="Selector" />.
      /// </summary>
      /// <example>
      ///  <code>
      ///  Binding.Selector selector = "foo.bar.baz";
      ///  var indexName = selector.Index; // "baz"
      ///  </code>
      /// </example>
      public Selector Index => new(ToArray().LastOrDefault());
      /// <summary>
      /// Creates a new <see cref="Selector" /> from a delimited <see cref="string" />.
      /// </summary>
      /// <param name="path">The delimited string.</param>
      /// <example>
      ///  <code>
      ///  var selector = new Binding.Selector("foo.bar.baz");
      ///  var path = selector.ToArray(); // ["foo", "bar", "baz"]
      ///  </code>
      /// </example>
      public Selector(string path) { Path = path ?? ""; }
      /// <summary>
      /// Creates a new <see cref="Selector" /> from an array of member names.
      /// </summary>
      /// <param name="path">The array of member names.</param> 
      /// <example>
      ///  <code>
      ///  var selector = new Binding.Selector(new[] { "foo", "bar", "baz" });
      ///  var path = selector.ToString(); // "foo.bar.baz"
      ///  </code>
      /// </example>
      public Selector(string[] path) { Path = string.Join(Delimiter, (path ?? new string[0]).Select(v => v ?? "").ToArray()); }
      /// <summary>
      /// The delimited <see cref="string" /> representation of this <see cref="Selector" />.
      /// </summary>
      /// <example>
      ///  <code>
      ///  Binding.Selector selector = new[] { "foo", "bar", "baz" };
      ///  var path = selector.ToString(); // "foo.bar.baz"
      ///  </code>
      /// </example>
      public override string ToString() => Path;
      /// <summary>
      /// The array of member names of this <see cref="Selector" />.
      /// </summary>
      /// <example>
      ///  <code>
      ///  Binding.Selector selector = "foo.bar.baz";
      ///  var path = selector.ToArray(); // ["foo", "bar", "baz"]
      ///  </code>
      /// </example>
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
      /// <example>
      ///  <code>
      ///   // Traversing an IViewModel with a Selector
      ///   var model = new ViewModel { { "foo", new ViewModel { { "bar", new ViewModel { { "baz", "qux" } } } } } }; 
      ///   Binding.Selector selector = "foo.bar.baz";
      ///   var value = selector.Select(model); // "qux"
      ///  </code>
      /// </example>
      /// <example>
      ///  <code>
      ///   // Using reflection to traverse an object with a Selector
      ///   var model = new { foo = new { bar = new { baz = "qux" } } };
      ///   Binding.Selector selector = "foo.bar.baz";
      ///   var value = selector.Select(model); // "qux"
      ///  </code>
      /// </example>
      public object Select(object model) => ToArray().Aggregate(model, (value, index) =>
      {
        if (value == null) { return null; }
        if (string.IsNullOrEmpty(index)) { return value; }
        if (value is IViewModel vm) { return vm.ContainsKey(index) ? vm[index] : null; }
        if (value is IViewList va) { return int.TryParse(index, out var i) && i >= 0 && i < va.Count ? va[i] : null; }
        var member = value.GetType().GetMember(index, MemberFlags).FirstOrDefault();
        return member is FieldInfo field ? field.GetValue(value) :
                member is PropertyInfo property ? property.GetValue(value) :
                null;
      });
      /// <inheritdoc cref="Select(object)" />
      public object Select(object model, Type type) => GetAssignmentValue(Select(model), type);
      /// <inheritdoc cref="Select(object)" />
      public T Select<T>(object model) => (T)Select(model, typeof(T));
      /// <summary>
      /// If the path exists; assigns the given value to the member referenced by the path from the given data structure.
      /// </summary>
      /// <param name="model">The data structure to be traversed.</param>
      /// <param name="value">The value to be assigned</param>
      /// <returns>A <see cref="boolean" /> indicating if the path was found and the value could be assigned.</returns>
      /// <example>
      ///  <code>
      ///   // Settings a value in an IViewModel with a Selector
      ///   var model = new ViewModel { { "foo", new ViewModel { { "bar", new ViewModel { { "baz", "qux" } } } } };
      ///   Binding.Selector selector = "foo.bar.baz";
      ///   var success = selector.Assign(model, "quux"); // true
      ///   var value = model["foo"]["bar"]["baz"]; // "quux"
      ///  </code>
      /// </example>
      public bool Assign(object model, object value)
      {
        model = Model.Select(model);
        var index = Index;
        if (model == null) { return false; }
        if (model is IViewModel vm) { vm[index] = value; return true; }
        if (model is IViewList va) { if (int.TryParse(index, out var i) && i >= 0 && i < va.Count) { va[i] = value; return true; } return false; }
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
      /// <example>
      ///  <code>
      ///   // Casting a float to a decimal
      ///   var value = Binding.Selector.GetAssignmentValue(3.14f, typeof(decimal)); // 3.14m
      ///  </code>
      /// </example>
      /// <example>
      ///  <code>
      ///   // Casting a string to a float
      ///   var value = Binding.Selector.GetAssignmentValue("3.14", typeof(float)); // 3.14f
      ///  </code>
      /// </example>
      /// <example>
      ///  <code>
      ///   // Using a custom type converter
      ///   public class MyTypeConverter : TypeConverter { 
      ///     public override bool CanConvertFrom(ITypeDescriptorContext context, Type type) => type == typeof(string);
      ///     public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) {
      ///       if (value is string s) { 
      ///         var parts = s.Split(','); 
      ///         return new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2])); 
      ///       }
      ///       return base.ConvertFrom(context, culture, value);
      ///     }
      ///     public override bool CanConvertTo(ITypeDescriptorContext context, Type type) => type == typeof(Vector3); 
      ///     public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type type) {
      ///       if (type == typeof(string) && value is Vector3 v) { 
      ///         return $"{v.x},{v.y},{v.z}"; 
      ///       }
      ///       return base.ConvertTo(context, culture, value, type);
      ///     }    
      ///   }
      ///   TypeDescriptor.AddAttributes(typeof(Vector3), new TypeConverterAttribute(typeof(MyTypeConverter)));
      ///   TypeDescriptor.AddAttributes(typeof(string), new TypeConverterAttribute(typeof(MyTypeConverter)));
      ///   var parsedValue = Binding.Selector.GetAssignmentValue("1,2,3", typeof(Vector3)); // (1, 2, 3)
      ///   var formatValue = Binding.Selector.GetAssignmentValue(new Vector3(1, 2, 3), typeof(string)); // "1,2,3"
      ///  </code>
      /// </example>
      public static object GetAssignmentValue(object value, Type type)
      {
        if (value == null) { return type.IsValueType ? Activator.CreateInstance(type) : null; }
        var fromType = value.GetType();
        if (type.IsAssignableFrom(fromType)) { return value; }
        try {
          var descriptor = TypeDescriptor.GetConverter(type);
          if (descriptor.CanConvertFrom(fromType) == true) { return descriptor.ConvertFrom(value); }
          descriptor = TypeDescriptor.GetConverter(fromType);
          if (descriptor?.CanConvertTo(type) == true) { return descriptor.ConvertTo(value, type); }
        } catch (Exception ex) { Debug.Log(new TypeConversionException(value, fromType, type, ex)); }
        if (type.IsValueType) { return Activator.CreateInstance(type); }
        return default;
      }
      /// <summary>
      /// Thrown in the event a type conversion says it will work, but doesn't
      /// </summary>
      public class TypeConversionException : Exception { public TypeConversionException(object value, Type from, Type to, Exception inner) : base($"Failed to convert `{value}` from {from} to {to}.", inner) { } }
      /// <summary>
      /// Converters for common types
      /// </summary>
      public static class Converters
      {
        /// <summary>
        /// A base <see cref="TypeConverter" /> for parsing <see cref="Selector" />s.
        /// </summary>
        public abstract class SelectorConverter<TConverter, TType> : TypeConverter 
          where TConverter : SelectorConverter<TConverter, TType>, new()
        {
          private static readonly TConverter _instance = new();
          /// <summary>
          /// The parser for this <see cref="TypeConverter" />.
          /// </summary>
          protected abstract Regex Parser { get; }
          /// <summary>
          /// Tests the value to see if it can be converted.
          /// </summary>
          public static bool CanParse(Selector selector) => _instance.Parser.IsMatch(selector);
          /// <summary>
          /// The parser method for this <see cref="TypeConverter" />.
          /// </summary>
          protected abstract TType Parse(Match match);
          /// <summary>
          /// Parses the value of a <see cref="Selector" />.
          /// </summary>
          public static TType Parse(Selector selector) => _instance.Parse(_instance.Parser.Match(selector));
          /// <inheritdoc/>
          public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(Selector);
          /// <inheritdoc/>
          public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) => Parse(Parser.Match((Selector)value));
          /// <inheritdoc/>
          public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(TType);
          /// <inheritdoc/>
          public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) => Parse(Parser.Match((Selector)value));
        }
        /// <summary>
        /// Converts a <see cref="Selector" /> to a <see cref="Vector3" />.
        /// </summary>
        [TypeConverter(typeof(Vector3Converter))]
        public class Vector3Converter : SelectorConverter<Vector3Converter, Vector3> { protected override Regex Parser => new(@"^\s*(?<x>-?\d+(\.\d+)?)\s*,\s*(?<y>-?\d+(\.\d+)?)\s*,\s*(?<z>-?\d+(\.\d+)?)\s*$"); protected override Vector3 Parse(Match match) => new(float.Parse(match.Groups["x"].Value), float.Parse(match.Groups["y"].Value), float.Parse(match.Groups["z"].Value)); }
        /// <summary>
        /// Converts a <see cref="Selector" /> to a <see cref="bool" />.
        /// </summary>
        [TypeConverter(typeof(BooleanConverter))]
        public class BooleanConverter : SelectorConverter<BooleanConverter, bool> { protected override Regex Parser => new(@"^\s*(?<value>true|false)\s*$"); protected override bool Parse(Match match) => bool.Parse(match.Groups["value"].Value); }
        /// <summary>
        /// Converts a <see cref="Selector" /> to a <see cref="byte" />.
        /// </summary>
        [TypeConverter(typeof(ByteConverter))]
        public class ByteConverter : SelectorConverter<ByteConverter, byte> { protected override Regex Parser => new(@"^\s*(?<value>\d+)\s*$"); protected override byte Parse(Match match) => byte.Parse(match.Groups["value"].Value); }
        /// <summary>
        /// Converts a <see cref="Selector" /> to a <see cref="int" />.
        /// </summary>
        [TypeConverter(typeof(Int32Converter))]
        public class Int32Converter : SelectorConverter<Int32Converter, int> { protected override Regex Parser => new(@"^\s*(?<value>-?\d+)\s*$"); protected override int Parse(Match match) => int.Parse(match.Groups["value"].Value); }
        /// <summary>
        /// Converts a <see cref="Selector" /> to a <see cref="long" />.
        /// </summary>
        [TypeConverter(typeof(Int64Converter))]
        public class Int64Converter : SelectorConverter<Int64Converter, long> { protected override Regex Parser => new(@"^\s*(?<value>-?\d+)\s*$"); protected override long Parse(Match match) => long.Parse(match.Groups["value"].Value); }
        /// <summary>
        /// Converts a <see cref="Selector" /> to a <see cref="float" />.
        /// </summary>
        [TypeConverter(typeof(SingleConverter))]
        public class SingleConverter : SelectorConverter<SingleConverter, float> { protected override Regex Parser => new(@"^\s*(?<value>-?\d+(\.\d+)?)\s*$"); protected override float Parse(Match match) => float.Parse(match.Groups["value"].Value); }
        /// <summary>
        /// Converts a <see cref="Selector" /> to a <see cref="double" />.
        /// </summary>
        [TypeConverter(typeof(DoubleConverter))]
        public class DoubleConverter : SelectorConverter<DoubleConverter, double> { protected override Regex Parser => new(@"^\s*(?<value>-?\d+(\.\d+)?)\s*$"); protected override double Parse(Match match) => double.Parse(match.Groups["value"].Value); }
        /// <summary>
        /// Converts a <see cref="Selector" /> to a <see cref="decimal" />.
        /// </summary>
        [TypeConverter(typeof(DecimalConverter))]
        public class DecimalConverter : SelectorConverter<DecimalConverter, decimal> { protected override Regex Parser => new(@"^\s*(?<value>-?\d+(\.\d+)?)\s*$"); protected override decimal Parse(Match match) => decimal.Parse(match.Groups["value"].Value); }
      }

      #if UNITY_EDITOR
      [CustomPropertyDrawer(typeof(Selector))]
      [Serializable] private class Editor : InlineEditor { public Editor() : base(
        "Path"
      ) { } }
      #endif
    }
  }
}