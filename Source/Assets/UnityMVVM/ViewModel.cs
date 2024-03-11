using System;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityMVVM.Base;
using Numbers = System.Globalization.NumberStyles;

namespace UnityMVVM
{
  /// <inheritdoc cref="IViewModel" />
  /// <remarks>
  /// A view model represents a data model or business model restructured to present a system's Information Architecture.
  /// </remarks>
  /// <example>
  /// <code>
  /// var model = new ViewModel {
  ///   { "title", "Hello, World!" },
  ///   { "items", new ViewModel.ViewList {
  ///     new ViewModel { { "name", "Item 1" }, { "value", 1 } },
  ///     new ViewModel { { "name", "Item 2" }, { "value", 2 } },
  ///   } }
  /// };
  /// </code>
  /// </example>    
  public class ViewModel : Dictionary<string, object>, IViewModel
  {
    /// <summary>Instantiates a new ViewModel</summary>
    public ViewModel() : base() {}
    /// <summary>Instantiates a new ViewModel from an existing <see cref="IDictionary{string, object}" /></summary>
    public ViewModel(IDictionary<string, object> items) : base(items ?? new ViewModel()) {}
    /// <inheritdoc cref="IViewList" />
    /// <remarks>
    /// A view model represents a data model or business model restructured to present a system's Information Architecture.
    /// </remarks>
    /// <example>
    /// <code>
    /// var model = new ViewModel.ViewList {
    ///   new ViewModel { { "name", "Item 1" }, { "value", 1 } },
    ///   new ViewModel { { "name", "Item 2" }, { "value", 2 } },
    /// };
    /// </code>
    /// </example>    
    public class ViewList : Collection<object>, IViewList {
      public ViewList() : base() {}
      public ViewList(IList<object> items) : base(items) {}
    }
    /// <summary>
    /// Default functionality for parsing <see cref="ViewModel" />s
    /// </summary>
    /// <remarks>
    /// <para>This functionality parses JSON-like strings by default and can be extended to support custom formatting.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var model = ViewModel.Parser.Parse(@"
    ///   { 
    ///     ""title"": ""Hello, World!"", 
    ///     ""items"": [ 
    ///       { ""name"": ""Item 1"", ""value"": 1 }, 
    ///       { ""name"": ""Item 2"", ""value"": 2 } 
    ///     ] 
    ///   }"
    /// );
    /// </code>
    /// </example>
    public class Parser : IEnumerable<Parser.Strategy>
    {
      /// <summary>
      /// Attempts to parse a value from the string at the given position.
      /// </summary>
      /// <param name="text">Reference to the string being parsed</param>
      /// <param name="position">Reference to the current parse character index</param>
      /// <param name="value">The parsed value or null</param>
      /// <param name="parser">Reference to the <cref see="Parser" /></param>
      /// <returns>True if this <see cref="Strategy" /> parsed a value.</returns>
      public delegate bool Strategy(ref string text, ref int position, out object value, Parser parser);
      private readonly Strategy[] _strategies;
      /// <inheritdoc cref="Parser" />
      public Parser(params Strategy[] strategies) => 
        _strategies = strategies?.Any() != true
          ? new Strategy[] {
            Strategies.TryParseNull,
            Strategies.TryParseBoolean,
            Strategies.TryParseNumber,
            Strategies.TryParseString,
            Strategies.TryParseList,
            Strategies.TryParseModel
          } : strategies.ToArray();
      /// <summary>
      /// Itterates this <see cref="Parser" />'s <see cref="Strategy" />s over a value.
      /// </summary>
      /// <param name="text">The text to be parsed</param>
      /// <param name="position">The position to parse from</param>
      /// <param name="value">The value parsed on success</param>
      /// <returns><see cref="true" /> if the value was parsed</returns>
      public bool TryParse(ref string text, ref int position, out object value)
      {
        Strategies.Trim(ref text, ref position);
        foreach (var strategy in this) {
          if (strategy(ref text, ref position, out value, this)) { return true; }
        }
        value = null;
        return false;
      }
      /// <summary>
      /// Parses the text into a value
      /// </summary>
      /// <param name="text">The text to parse</param>
      /// <returns>A parsed value or null</returns>
      public static object Parse(string text) {
        var parser = new Parser(); var position = 0;
        return parser.TryParse(ref text, ref position, out var value) ? value : null;
      }
      /// <summary>
      /// Default <see cref="Strategy" />s for parsing
      /// </summary>
      public static class Strategies {
        /// <summary>
        /// Advances the position to the next non-whitespace character.
        /// </summary>
        /// <param name="text">Reference to the string being parsed</param>
        /// <param name="position">Reference to the current parsing position</param>
        /// <returns>The new position</returns>
        public static int Trim(ref string text, ref int position) {
          while (position < text.Length && char.IsWhiteSpace(text[position])) { position++; }
          return position;
        }
        /// <inheritdoc cref="Strategy" />
        /// <remarks>This is the default strategy for parsing null</remarks>
        public static bool TryParseNull(ref string text, ref int position, out object value, Parser parser) {
          value = null;
          if (text.Length-position >= 3 && text.Substring(position, 3).ToLowerInvariant() == "null") { position += 3; return true; }
          return false;
        }
        /// <inheritdoc cref="Strategy" />
        /// <remarks>This is the default strategy for parsing bool</remarks>
        public static bool TryParseBoolean(ref string text, ref int position, out object value, Parser parser) {
          value = default;
          var length = text.Length - position;
          if (length >= 4 && text.Substring(position, 4).ToLowerInvariant() == "true") { position += 4; return (bool)(value = true); }
          if (length >= 5 && text.Substring(position, 5).ToLowerInvariant() == "false") { position += 5; return !(bool)(value = false); }
          return false;
        }
        private const string Numerics = "0123456789-+.eE";
        /// <inheritdoc cref="Strategy" />
        /// <remarks>This is the default strategy for parsing numbers</remarks>
        public static bool TryParseNumber(ref string text, ref int position, out object value, Parser parser) {
          var index = position; value = default;
          while (index < text.Length && Numerics.Contains(text[index])) { index++; }
          if (index == position) { return false; }
          if (!float.TryParse(text.Substring(position, index-position), out var number)) { return false; }
          value = number; position = index;
          return true;
        }
        private const int UnicodeLength = 4;
        private static readonly Dictionary<char, char> Unescapes = new() {
          { '"', '"' },
          { 'r', '\r' },
          { 'n', '\n' },
          { 't', '\t' },
          { 'b', '\b' },
          { 'f', '\f' },
          { '/', '/' },
          { '\\', '\\' },
        };
        /// <summary>
        /// Unescapes a JSON-like string.
        /// </summary> 
        public static string Unescape(string text) {
          var position = 0;
          var next = text.IndexOf('\\');
          var builder = new StringBuilder();
          while (next >= 0) {
            builder.Append(text[position..next++]);
            if (next >= text.Length) { break; }
            var code = text[next++];
            if (Unescapes.TryGetValue(code, out var unescape)) { 
              builder.Append(unescape); 
            } else if (code == 'u') {
              if (next + UnicodeLength >= text.Length) { break; }
              if (int.TryParse(text.Substring(next, UnicodeLength), Numbers.HexNumber, null, out var character)) {
                builder.Append(char.ConvertFromUtf32(character));
              }
              next += UnicodeLength;
            } else { builder.Append(code); }
            position = next;
            next = text.IndexOf('\\', position);
          }
          builder.Append(text[position..]);
          return builder.ToString();
        }
        /// <inheritdoc cref="Strategy" />
        /// <remarks>This is the default strategy for parsing strings</remarks>
        public static bool TryParseString(ref string text, ref int position, out object value, Parser parser) {
          var index = position; value = default;
          if (index < text.Length && text[index++] != '"') { return false; }
          var escape = false;
          var start = index;
          while (index < text.Length) {
            if (escape) { escape = false; index++; continue; }
            if (text[index] == '\\') { escape = true; index++; continue; }
            if (text[index] == '"') { break; }
            index++;
          }
          if (index >= text.Length) { return false; }
          position = index;
          value = Unescape(text[start..position++]);
          return true;
        }
        /// <inheritdoc cref="Strategy" />
        /// <remarks>This is the default strategy for parsing arrays</remarks>
        public static bool TryParseList(ref string text, ref int position, out object value, Parser parser) {
          var index = position; value = default;
          if (index < text.Length && text[index++] != '[') { return false; }
          Trim(ref text, ref index);
          if (index < text.Length && text[index] == ']') { position = ++index; return true; }
          var set = (ViewList)(value = new ViewList());
          while (index < text.Length) {
            if (!parser.TryParse(ref text, ref index, out var item)) { return false; }
            set.Add(item);
            Trim(ref text, ref index);
            if (text[index] == ']') { position = ++index; return true; }
            if (text[index] != ',') { return false; }
            index++;
            Trim(ref text, ref index);
          }
          return false;
        }
        /// <inheritdoc cref="Strategy" />
        /// <remarks>This is the default strategy for parsing objects</remarks>
        public static bool TryParseModel(ref string text, ref int position, out object value, Parser parser) {
          var index = position; value = default; 
          if (index < text.Length && text[index++] != '{') { return false; }
          Trim(ref text, ref index);
          if (index < text.Length && text[index] == '}') { position = ++index; return true; }
          var model = (ViewModel)(value = new ViewModel());
          while (index < text.Length) {
            if (!TryParseString(ref text, ref index, out var key, parser)) { return false; }
            Trim(ref text, ref index);
            if (text[index] != ':') { return false; }
            index++;
            Trim(ref text, ref index);
            if (!parser.TryParse(ref text, ref index, out var item)) { return false; }
            model[(string)key] = item;
            Trim(ref text, ref index);
            if (text[index] == '}') { position = ++index; return true; }
            if (text[index] != ',') { return false; }
            index++;
            Trim(ref text, ref index);
          }
          return false;
        }
      }
      /// <inheritdoc />
      public IEnumerator<Strategy> GetEnumerator() => _strategies.AsEnumerable().GetEnumerator();
      /// <inheritdoc />
      IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    /// <summary>
    /// Default functionality for formatting <see cref="ViewModel" />s
    /// </summary>
    /// <remarks>
    /// <para>The default strategies identify select, well-known types as values. Otherwise memberwise reflection is performed.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var model = new ViewModel {
    ///  { "title", "Hello, World!" },
    /// };
    /// var formatted = ViewModel.Formatter.Format(model);
    /// </code>
    /// </example>  
    public class Formatter : IEnumerable<Formatter.Strategy>
    {
      /// <summary>
      /// A strategy for formatting a value into a string.
      /// </summary>
      public delegate bool Strategy(object value, StringBuilder formatted, Formatter formatter);
      private readonly Strategy[] _strategies;
      /// <inheritdoc cref="Formatter" />
      public Formatter(params Strategy[] strategies) 
        => _strategies = strategies?.Any() != true
          ? new Strategy[] {
            Strategies.TryFormatNull,
            Strategies.TryFormatEnum,
            Strategies.TryFormatBool,
            Strategies.TryFormatChar,
            Strategies.TryFormatGuid,
            Strategies.TryFormatUri,
            Strategies.TryFormatDateTime,
            Strategies.TryFormatString,
            Strategies.TryFormatNumber,
            Strategies.TryFormatDictionary,
            Strategies.TryFormatCollection,
            Strategies.TryFormatObject
          } : strategies.ToArray();
      /// <summary>
      /// Formats the value using the <see cref="Formatter" />'s <see cref="Strategy" />s
      /// </summary>
      public bool TryFormat(object value, StringBuilder formatted) {
        foreach (var strategy in this) { if (strategy(value, formatted, this)) { return true; } }
        return false;
      }
      /// <summary>
      /// Formats the value using the default <see cref="Strategy" />s
      /// </summary>
      /// <example>
      /// <code>
      /// var model = new ViewModel {
      ///  { "title", "Hello, World!" },
      /// };
      /// var formatted = ViewModel.Formatter.Format(model);
      /// </code>
      /// </example>  
      public static string Format(object value) {
        var formatter = new Formatter(); var formatted = new StringBuilder();
        return formatter.TryFormat(value, formatted) ? formatted.ToString() : "";
      }
      /// <summary>
      /// Default <see cref="Strategy" />s for formatting
      /// </summary>
      public static class Strategies 
      {
        private const string NULL = "null";
        /// <inheritdoc cref="Strategy" />
        /// <remarks>This is the default strategy for formatting <see cref="null" />s</remarks>
        public static bool TryFormatNull(object value, StringBuilder formatted, Formatter formatter) {
          if (value != null) { return false; }
          formatted.Append(NULL); 
          return true;
        }
        /// <inheritdoc cref="Strategy" />
        /// <remarks>This is the default strategy for formatting <see cref="enum" />s</remarks>
        public static bool TryFormatEnum(object value, StringBuilder formatted, Formatter formatter) {
          if (value is Enum @enum) { return TryFormatString(@enum.ToString(), formatted, formatter); }
          return false;
        }
        /// <inheritdoc cref="Strategy" />
        /// <remarks>This is the default strategy for formatting <see cref="bool" />s</remarks>
        public static bool TryFormatBool(object value, StringBuilder formatted, Formatter formatter) {
          if (value is bool @bool) { formatted.Append(@bool.ToString().ToLowerInvariant()); return true; }
          return false;
        }
        /// <inheritdoc cref="Strategy" />
        /// <remarks>This is the default strategy for formatting <see cref="char" />s</remarks>
        public static bool TryFormatChar(object value, StringBuilder formatted, Formatter formatter) {
          if (value is char @char) { return TryFormatString(@char.ToString(), formatted, formatter); }
          return false;
        }
        /// <inheritdoc cref="Strategy" />
        /// <remarks>This is the default strategy for formatting <see cref="Guid" />s</remarks>
        public static bool TryFormatGuid(object value, StringBuilder formatted, Formatter formatter) {
          if (value is Guid @guid) { return TryFormatString(@guid.ToString(), formatted, formatter); }
          return false;
        }
        /// <inheritdoc cref="Strategy" />
        /// <remarks>This is the default strategy for formatting <see cref="Uri" />s</remarks>
        public static bool TryFormatUri(object value, StringBuilder formatted, Formatter formatter) {
          if (value is Uri @uri) { return TryFormatString(@uri.ToString(), formatted, formatter); }
          return false;
        }
        /// <inheritdoc cref="Strategy" />
        /// <remarks>This is the default strategy for formatting <see cref="DateTime" />s</remarks>
        public static bool TryFormatDateTime(object value, StringBuilder formatted, Formatter formatter) {
          if (value is DateTime @datetime) { return TryFormatNumber(((DateTimeOffset)@datetime).ToUnixTimeMilliseconds(), formatted, formatter); }
          return false;
        }
        private const int UnicodeLength = 4;
        private static readonly Dictionary<char, char> Unescapes = new() {
          { '"', '"' },
          { '\r', 'r' },
          { '\n', 'n' },
          { '\t', 't' },
          { '\b', 'b' },
          { '\f', 'f' },
          { '/', '/' },
          { '\\', '\\' },
        };
        /// <summary>
        /// Escapes a string for JSON-like formatting.
        /// </summary>
        public static string Escape(string text) {
          var builder = new StringBuilder();
          foreach (var @char in text) {
            if (Unescapes.TryGetValue(@char, out var unescape)) { builder.Append('\\').Append(unescape); continue; }
            if (@char < 32) { builder.Append("\\u").Append(((int)@char).ToString("X4")); continue; }
            builder.Append(@char);
          }
          return builder.ToString();
        }
        /// <inheritdoc cref="Strategy" />
        /// <remarks>This is the default strategy for formatting <see cref="string" />s</remarks>
        public static bool TryFormatString(object value, StringBuilder formatted, Formatter formatter) {
          if (value is string @string) { formatted.Append('"').Append(Escape(@string)).Append('"'); return true; }
          return false;
        }
        /// <inheritdoc cref="Strategy" />
        /// <remarks>This is the default strategy for formatting <see cref="IConvertible" />s</remarks>
        public static bool TryFormatNumber(object value, StringBuilder formatted, Formatter formatter) {
          //if (value is string || value is char || value is bool || value is Guid || value is DateTime) { return false; }
          if (value is IConvertible convertible) { formatted.Append(convertible.ToDecimal(null).ToString("N5")); return true; }
          return false;
        }
        /// <summary>
        /// Formats a property.
        /// </summary>
        public static void FormatProperty(string key, object value, StringBuilder formatted, Formatter formatter) {
          if (!TryFormatString(key, formatted, formatter)) { return; }
          formatted.Append(':');
          if (
            !formatter.TryFormat(value, formatted)
            && !TryFormatString($"{value}", formatted, formatter)
          ) { formatted.Append(NULL); }
        }
        /// <inheritdoc cref="Strategy" />
        /// <remarks>This is the default strategy for formatting <see cref="IDictionary" />s</remarks>
        public static bool TryFormatDictionary(object value, StringBuilder formatted, Formatter formatter) {
          if (value is IDictionary dictionary) {
            formatted.Append('{');
            var delimiter = "";
            foreach (var key in dictionary.Keys) {
              formatted.Append(delimiter);
              FormatProperty($"{key}", dictionary[key], formatted, formatter);
              delimiter = ",";
            }
            formatted.Append('}');
            return true;
          }
          return false;
        }
        /// <inheritdoc cref="Strategy" />
        /// <remarks>This is the default strategy for formatting <see cref="ICollection" />s</remarks>
        public static bool TryFormatCollection(object value, StringBuilder formatted, Formatter formatter) {
          if (value is ICollection collection) {
            formatted.Append('[');
            var delimiter = "";
            foreach (var item in collection) {
              formatted.Append(delimiter);
              formatter.TryFormat(item, formatted);
              delimiter = ",";
            }
            formatted.Append(']');
            return true;
          }
          return false;
        }
        /// <inheritdoc cref="Strategy" />
        /// <remarks>This is the default strategy for formatting a value as an object</remarks>
        public static bool TryFormatObject(object value, StringBuilder formatted, Formatter formatter) {
          if (value == null) { return false; }
          var members = value.GetType().GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
          formatted.Append('{');
          var delimiter = "";
          foreach (var member in members) {
            formatted.Append(delimiter);
            if (member is FieldInfo field && !field.IsInitOnly) { FormatProperty(field.Name, field.GetValue(value), formatted, formatter); }
            if (member is PropertyInfo property && property.CanRead) { FormatProperty(property.Name, property.GetValue(value), formatted, formatter); }
            delimiter = ",";
          }
          formatted.Append('}');
          return true;
        }
          
      }
      /// <inheritdoc />
      public IEnumerator<Strategy> GetEnumerator() => _strategies.AsEnumerable().GetEnumerator();
      /// <inheritdoc />
      IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    /// <summary>
    /// Default functionality for reflecting objects into <see cref="ViewModel" />s
    /// </summary>
    /// <remarks>
    /// <para>The default strategies identify select, well-known types as values. Otherwise memberwise reflection is performed.</para>
    /// </remarks>
    public class Reflector : IEnumerable<Reflector.Strategy>
    {
      /// <summary>
      /// A strategy for reflecting single or related value types.
      /// </summary>
      /// <param name="value">The value to be reflected</param>
      /// <param name="reflected">The reflected value</param>
      /// <param name="reflector">Reference to the <see cref="Reflector" /></param>
      /// <returns>True if this <see cref="Strategy" /> handled the value.</returns>
      public delegate bool Strategy(object value, out object reflected, Reflector reflector);
      private readonly Dictionary<object, object> _seen = new();
      private readonly Strategy[] _strategies;
      /// <inheritdoc cref="Reflector" />
      public Reflector(params Strategy[] strategies) => 
        _strategies = strategies?.Any() != true
          ? new Strategy[] {
            Strategies.TryReflectString,
            Strategies.TryReflectCharArray,
            Strategies.TryReflectChar,
            Strategies.TryReflectBool,
            Strategies.TryReflectByte,
            Strategies.TryReflectSByte,
            Strategies.TryReflectShort,
            Strategies.TryReflectUShort,
            Strategies.TryReflectInt,
            Strategies.TryReflectUInt,
            Strategies.TryReflectLong,
            Strategies.TryReflectULong,
            Strategies.TryReflectFloat,
            Strategies.TryReflectDouble,
            Strategies.TryReflectDecimal,
            Strategies.TryReflectEnum,
            Strategies.TryReflectGuid,
            Strategies.TryReflectUri,
            Strategies.TryReflectDateTime,
            Strategies.TryReflectDateTimeOffset,
            Strategies.TryReflectDictionary,
            Strategies.TryReflectCollection,
          } : strategies.ToArray();
      /// <summary>
      /// Determines if this <see cref="Reflector" /> instance has encountered the reference
      /// </summary>
      /// <param name="reference">The reference to be compared</param>
      /// <returns>True if the reference is recognized</returns>
      public bool HasSeen(object reference) => _seen.ContainsKey(reference);
      /// <summary>
      /// Gets or sets a cached reflection for the reference
      /// </summary>
      /// <value>Reference to the original value</value>
      public object this[object reference] {
        get => _seen[reference];
        set => _seen[reference] = value;
      }
      /// <summary>
      /// Generates a reflection for the given value
      /// </summary>
      /// <param name="value">The value to be evaluated</param>
      /// <returns>The evaluated reflection</returns>
      public object Evaluate(object value) {
        if (value == null) { return null; }
        if (HasSeen(value)) { return this[value]; }
        foreach (var strategy in this) {
          if (strategy(value, out var reflected, this)) { return this[value] = reflected; }
        }
        var model = (ViewModel)(this[value] = new ViewModel());
        foreach (var member in value.GetType().GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
          if (member is FieldInfo field) { model[field.Name] = Evaluate(field.GetValue(value)); }
          if (member is PropertyInfo property && property.CanRead) { model[property.Name] = Evaluate(property.GetValue(value)); }
          // TODO: Add support for methods / eventing
        }
        return model;
      }
      /// <inheritdoc cref="Evaluate(object)" />
      public static object Reflect(object value) => new Reflector().Evaluate(value);
      public static class Strategies {
        /// <inheritdoc cref="Strategy" />
        /// <remarks>Identifies a <see cref="string" />.</remarks>
        public static bool TryReflectString(object value, out object reflected, Reflector reflector) { 
          if (value is string @string) { reflected = @string; return true; }
          reflected = null; return false;
        }
        /// <inheritdoc cref="Strategy" />
        /// <remarks>Identifies a <see cref="char" /> and generates a string.</remarks>
        public static bool TryReflectChar(object value, out object reflected, Reflector reflector) { 
          if (value is char @char) { reflected = @char.ToString(); return true; }
          reflected = null; return false;
        }
        /// <inheritdoc cref="Strategy" />
        /// <remarks>Identifies a <see cref="char[]" /> and generates a string.</remarks>
        public static bool TryReflectCharArray(object value, out object reflected, Reflector reflector) { 
          if (value is char[] @array) { reflected = new string(@array); return true; }
          reflected = null; return false;
        }
        /// <inheritdoc cref="Strategy" />
        /// <remarks>Identifies a <see cref="bool" /></remarks>
        public static bool TryReflectBool(object value, out object reflected, Reflector reflector) { 
          if (value is bool @bool) { reflected = @bool; return true; }
          reflected = null; return false;
        }
        /// <inheritdoc cref="Strategy" />
        /// <remarks>Identifies a <see cref="byte" /></remarks>
        public static bool TryReflectByte(object value, out object reflected, Reflector reflector) { 
          if (value is byte @byte) { reflected = (decimal)@byte; return true; }
          reflected = null; return false;
        }
        /// <inheritdoc cref="Strategy" />
        /// <remarks>Identifies a <see cref="sbyte" /></remarks>
        public static bool TryReflectSByte(object value, out object reflected, Reflector reflector) { 
          if (value is sbyte @sbyte) { reflected = (decimal)@sbyte; return true; }
          reflected = null; return false;
        }
        /// <inheritdoc cref="Strategy" />
        /// <remarks>Identifies a <see cref="short" /></remarks>
        public static bool TryReflectShort(object value, out object reflected, Reflector reflector) { 
          if (value is short @short) { reflected = (decimal)@short; return true; }
          reflected = null; return false;
        }
        /// <inheritdoc cref="Strategy" />
        /// <remarks>Identifies a <see cref="ushort" /></remarks>
        public static bool TryReflectUShort(object value, out object reflected, Reflector reflector) { 
          if (value is ushort @ushort) { reflected = (decimal)@ushort; return true; }
          reflected = null; return false;
        }
        /// <inheritdoc cref="Strategy" />
        /// <remarks>Identifies a <see cref="int" /></remarks>
        public static bool TryReflectInt(object value, out object reflected, Reflector reflector) { 
          if (value is int @int) { reflected = (decimal)@int; return true; }
          reflected = null; return false;
        }
        /// <inheritdoc cref="Strategy" />
        /// <remarks>Identifies a <see cref="uint" /></remarks>
        public static bool TryReflectUInt(object value, out object reflected, Reflector reflector) { 
          if (value is uint @uint) { reflected = (decimal)@uint; return true; }
          reflected = null; return false;
        }
        /// <inheritdoc cref="Strategy" />
        /// <remarks>Identifies a <see cref="long" /></remarks>
        public static bool TryReflectLong(object value, out object reflected, Reflector reflector) { 
          if (value is long @long) { reflected = (decimal)@long; return true; }
          reflected = null; return false;
        }
        /// <inheritdoc cref="Strategy" />
        /// <remarks>Identifies a <see cref="ulong" /></remarks>
        public static bool TryReflectULong(object value, out object reflected, Reflector reflector) { 
          if (value is ulong @ulong) { reflected = (decimal)@ulong; return true; }
          reflected = null; return false;
        }
        /// <inheritdoc cref="Strategy" />
        /// <remarks>Identifies a <see cref="float" /></remarks>
        public static bool TryReflectFloat(object value, out object reflected, Reflector reflector) { 
          if (value is float @float) { reflected = (decimal)@float; return true; }
          reflected = null; return false;
        }
        /// <inheritdoc cref="Strategy" />
        /// <remarks>Identifies a <see cref="double" /></remarks>
        public static bool TryReflectDouble(object value, out object reflected, Reflector reflector) { 
          if (value is double @double) { reflected = (decimal)@double; return true; }
          reflected = null; return false;
        }
        /// <inheritdoc cref="Strategy" />
        /// <remarks>Identifies a <see cref="decimal" /></remarks>
        public static bool TryReflectDecimal(object value, out object reflected, Reflector reflector) { 
          if (value is decimal @decimal) { reflected = @decimal; return true; }
          reflected = null; return false;
        }
        /// <inheritdoc cref="Strategy" />
        /// <remarks>Identifies an <see cref="enum" /> and generates a string.</remarks>
        public static bool TryReflectEnum(object value, out object reflected, Reflector reflector) { 
          if (value is Enum @enum) { reflected = @enum.ToString(); return true; }
          reflected = null; return false;
        }
        /// <inheritdoc cref="Strategy" />
        /// <remarks>Identifies a <see cref="Guid" /> and generates a string.</remarks>
        public static bool TryReflectGuid(object value, out object reflected, Reflector reflector) { 
          if (value is Guid @guid) { reflected = @guid.ToString(); return true; }
          reflected = null; return false;
        }
        /// <inheritdoc cref="Strategy" />
        /// <remarks>Identifies a <see cref="Uri" /> and generates a string.</remarks>
        public static bool TryReflectUri(object value, out object reflected, Reflector reflector) { 
          if (value is Uri @uri) { reflected = @uri.ToString(); return true; }
          reflected = null; return false;
        }
        /// <inheritdoc cref="Strategy" />
        /// <remarks>Identifies a <see cref="DateTime" /> and generates a string.</remarks>
        public static bool TryReflectDateTime(object value, out object reflected, Reflector reflector) { 
          if (value is DateTime @datetime) { reflected = ((DateTimeOffset)@datetime).ToUniversalTime().ToUnixTimeMilliseconds(); return true; }
          reflected = null; return false;
        }
        /// <inheritdoc cref="Strategy" />
        /// <remarks>Identifies a <see cref="DateTimeOffset" /> and generates a string.</remarks>
        public static bool TryReflectDateTimeOffset(object value, out object reflected, Reflector reflector) { 
          if (value is DateTimeOffset @datetime) { reflected = @datetime.ToUniversalTime().ToUnixTimeMilliseconds(); return true; }
          reflected = null; return false;
        }
        /// <inheritdoc cref="Strategy" />
        /// <remarks>Identifies an <see cref="IDictionary" /> and generates a <see cref="ViewModel" />.</remarks>
        public static bool TryReflectDictionary(object value, out object reflected, Reflector reflector) { 
          if (value is IDictionary dictionary) { 
            var model = (ViewModel)(reflected = new ViewModel());
            foreach (var key in dictionary) { 
              model[key?.ToString()] = reflector.Evaluate(dictionary[key]); 
            }
            return true;
          }
          reflected = null; return false;
        }
        /// <inheritdoc cref="Strategy" />
        /// <remarks>
        /// <para>Identifies an <see cref="ICollection" /> and generates a <see cref="ViewList" />.</para>
        /// <para>NOTE: Order this <see cref="Strategy" /> last when capturing other, more specific <see cref="ICollection" /> types</para>
        /// </remarks>
        public static bool TryReflectCollection(object value, out object reflected, Reflector reflector) { 
          if (value is ICollection collection) { reflected = new ViewList(collection.Cast<object>().Select(v => reflector.Evaluate(v)).ToList()); return true; }
          reflected = null; return false;
        }
      }
      /// <inheritdoc/>
      public IEnumerator<Strategy> GetEnumerator() => _strategies.AsEnumerable().GetEnumerator();
      /// <inheritdoc/>
      IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    /// <summary>
    /// Default converters for <see cref="IViewModel" />s and <see cref="IViewList" />s
    /// </summary>
    public static class Converters {
      [TypeConverter(typeof(ViewModelConverter))]
      public class ViewModelConverter : TypeConverter {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => typeof(IViewModel).IsAssignableFrom(sourceType);
        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) => new ViewModel(value as IViewModel);
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
          => destinationType.IsAssignableFrom(typeof(ViewModel))
            || !destinationType.IsPrimitive
            && !destinationType.IsAbstract
            && !destinationType.IsInterface
            && !destinationType.IsEnum
            && !destinationType.IsTypeDefinition
            && !destinationType.IsGenericTypeDefinition
            && destinationType.GetConstructor(Type.EmptyTypes) != null;
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType) {
          var model = value as IViewModel;
          if (model == null) { return base.ConvertTo(context, culture, value, destinationType); }
          var instance = Activator.CreateInstance(destinationType);
          var members = destinationType.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).ToDictionary(m => m.Name);
          foreach (var name in model.Keys.Where(members.ContainsKey)) {
            if (members[name] is FieldInfo field && !field.IsInitOnly) { field.SetValue(instance, Binding.Selector.GetAssignmentValue(model[name], field.FieldType)); }
            if (members[name] is PropertyInfo property && property.CanWrite) { property.SetValue(instance, Binding.Selector.GetAssignmentValue(model[name], property.PropertyType)); }
          }
          return instance;
        }
      }
      [TypeConverter(typeof(ViewListConverter))]
      public class ViewListConverter : TypeConverter {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => typeof(IEnumerable).IsAssignableFrom(sourceType);
        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) {
          if (value is IEnumerable e) { return new ViewList(e.Cast<object>().ToList()); }
          return new ViewList();
        }
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
          => destinationType.IsArray
          || destinationType.IsGenericType && destinationType.GetGenericTypeDefinition() == typeof(List<>)
          || destinationType.IsGenericType && destinationType.GetGenericTypeDefinition() == typeof(IList<>);
        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType) {
          if (value is IList items) {
            var itemType = destinationType.IsArray ? destinationType.GetElementType() : destinationType.GetGenericArguments().First();
            var array = items.Cast<object>().Select(o => Binding.Selector.GetAssignmentValue(o, itemType)).ToArray();
            if (destinationType.IsArray) { return array; }
            var listType = typeof(List<>).MakeGenericType(itemType);
            return (IList)Activator.CreateInstance(listType, array);
          }
          return base.ConvertTo(context, culture, value, destinationType);
        }
      }
    }
  }
}