using System.Text;
using System.Collections.Generic;
using UnityMVVM.Base;
using System.Collections.ObjectModel;
using Numbers = System.Globalization.NumberStyles;

namespace UnityMVVM
{
  /// <summary>
  /// A dynamic model for data binding.
  /// </summary>
  /// <remarks>
  /// A view-model represents a data-model or business-model restructured to present a system's Information Architecture.
  /// </remarks>
  public class ViewModel : Dictionary<string, object>, Binding.IModel
  {
    public class Set : Collection<object>, Binding.ISet { }
    public static class Parser
    {
      public delegate bool Strategy(ref string text, ref int position, out object value);
      public static int Trim(ref string text, ref int position) {
        while (position < text.Length && char.IsWhiteSpace(text[position])) { position++; }
        return position;
      }
      public static bool TryParseNull(ref string text, ref int position) {
        if (text.Length-position >= 4 && text.Substring(position, 4).ToLowerInvariant() == "null") { position += 4; return true; }
        return false;
      }
      public static bool TryParseBoolean(ref string text, ref int position, out bool value) {
        value = default;
        var length = text.Length - position;
        if (length >= 4 && text.Substring(position, 4).ToLowerInvariant() == "true") { position += 4; return value = true; }
        if (length >= 5 && text.Substring(position, 5).ToLowerInvariant() == "false") { position += 5; return !(value = false); }
        return false;
      }
      private const string Numerics = "0123456789-+.eE";
      public static bool TryParseNumber(ref string text, ref int position, out decimal value) {
        var index = position; value = default;
        while (index < text.Length && Numerics.Contains(text[index])) { index++; }
        if (index == position) { return false; }
        if (!decimal.TryParse(text.Substring(position, index-position), out value)) { return false; }
        position = index;
        return true;
      }
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
      private const int UnicodeLength = 4;
      public static string Unescape(string text) {
        var position = 0;
        var next = text.IndexOf('\\');
        var builder = new StringBuilder();
        while (next >= 0) {
          builder.Append(text.Substring(position, next++ - position));
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
        builder.Append(text.Substring(position));
        return builder.ToString();
      }
      public static bool TryParseString(ref string text, ref int position, out string value) {
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
        value = Unescape(text.Substring(start, position++ - start));
        return true;
      }
      public static bool TryParseSet(ref string text, ref int position, out Binding.ISet value, Strategy strategy = null) {
        var index = position; value = default; strategy = strategy ?? DefaultStrategy;
        if (index < text.Length && text[index++] != '[') { return false; }
        Trim(ref text, ref index);
        if (index < text.Length && text[index] == ']') { position = ++index; return true; }
        value = new Set();
        while (index < text.Length) {
          if (!strategy(ref text, ref index, out var item)) { return false; }
          value.Add(item);
          Trim(ref text, ref index);
          if (text[index] == ']') { position = ++index; return true; }
          if (text[index] != ',') { return false; }
          index++;
          Trim(ref text, ref index);
        }
        return false;
      }
      public static bool TryParseModel(ref string text, ref int position, out Binding.IModel value, Strategy strategy = null) {
        var index = position; value = default; strategy = strategy ?? DefaultStrategy;
        if (index < text.Length && text[index++] != '{') { return false; }
        Trim(ref text, ref index);
        if (index < text.Length && text[index] == '}') { position = ++index; return true; }
        value = new ViewModel();
        while (index < text.Length) {
          if (!TryParseString(ref text, ref index, out var key)) { return false; }
          Trim(ref text, ref index);
          if (text[index] != ':') { return false; }
          index++;
          Trim(ref text, ref index);
          if (!strategy(ref text, ref index, out var item)) { return false; }
          value[key] = item;
          Trim(ref text, ref index);
          if (text[index] == '}') { position = ++index; return true; }
          if (text[index] != ',') { return false; }
          index++;
          Trim(ref text, ref index);
        }
        return false;
      }
      public static bool DefaultStrategy(ref string text, ref int position, out object value) {
        value = default;
        Trim(ref text, ref position);
        if (TryParseNull(ref text, ref position)) { value = null; return true; }
        if (TryParseBoolean(ref text, ref position, out var boolean)) { value = boolean; return true; }
        if (TryParseNumber(ref text, ref position, out var number)) { value = number; return true; }
        if (TryParseString(ref text, ref position, out var @string)) { value = @string; return true; }
        if (TryParseSet(ref text, ref position, out var array)) { value = array; return true; }
        if (TryParseModel(ref text, ref position, out var @object)) { value = @object; return true; }
        return false;
      }
      public static object Parse(string text, Strategy strategy = null) {
        var position = 0; strategy = strategy ?? DefaultStrategy;
        if (!strategy(ref text, ref position, out var value)) { throw new System.Exception("Unsupported Format"); }
        return value;
      }
    }
  }
}