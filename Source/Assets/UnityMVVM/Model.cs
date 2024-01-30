using System.Text;
using System.Collections.Generic;

namespace UnityMVVM
{
  public class Model : Dictionary<string, object>
  {
    public class Set : List<object> { }
    public static class Parser
    {
      public delegate bool CustomParser(ref string text, ref int position, out object value);
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
      public static bool TryParseNumber(ref string text, ref int position, out decimal value) {
        var index = position; value = default;
        var negate = text[index] == '-';
        if (negate) { index++; Trim(ref text, ref index); }
        while (index < text.Length && char.IsDigit(text[index])) { index++; }
        if (index < text.Length && text[index] == '.') { index++; }
        while (index < text.Length && char.IsDigit(text[index])) { index++; }
        if (index == position) { return false; }
        value = decimal.Parse(text.Substring(position, index-position));
        position = index;
        return true;
      }
      private static readonly Dictionary<string, string> Unescapes = new() {
        { @"""", "\"" },
        { @"r", "\r" },
        { @"n", "\n" },
        { @"t", "\t" },
        { @"b", "\b" },
        { @"f", "\f" },
        { @"\", @"\" },
      };
      public static string Unescape(string text) {
        var builder = new StringBuilder();
        var position = 0;
        var next = text.IndexOf('\\');
        while (next >= 0) {
          builder.Append(text.Substring(position, next - position));
          if (next + 1 >= text.Length) { break; }
          var escape = text[next + 1];
          if (Unescapes.TryGetValue(escape.ToString(), out var unescape)) { builder.Append(unescape); }
          else { builder.Append(escape); }
          position = next + 2;
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
      public static bool TryParseSet(ref string text, ref int position, out Set value, CustomParser custom = null) {
        var index = position; value = default; custom = custom ?? TryParseAny;
        if (index < text.Length && text[index++] != '[') { return false; }
        Trim(ref text, ref index);
        if (index < text.Length && text[index] == ']') { position = ++index; return true; }
        value = new();
        while (index < text.Length) {
          if (!custom(ref text, ref index, out var item)) { return false; }
          value.Add(item);
          Trim(ref text, ref index);
          if (text[index] == ']') { position = ++index; return true; }
          if (text[index] != ',') { return false; }
          index++;
          Trim(ref text, ref index);
        }
        return false;
      }
      public static bool TryParseModel(ref string text, ref int position, out Model value, CustomParser custom = null) {
        var index = position; value = default; custom = custom ?? TryParseAny;
        if (index < text.Length && text[index++] != '{') { return false; }
        Trim(ref text, ref index);
        if (index < text.Length && text[index] == '}') { position = ++index; return true; }
        value = new();
        while (index < text.Length) {
          if (!TryParseString(ref text, ref index, out var key)) { return false; }
          Trim(ref text, ref index);
          if (text[index] != ':') { return false; }
          index++;
          Trim(ref text, ref index);
          if (!custom(ref text, ref index, out var item)) { return false; }
          value[key] = item;
          Trim(ref text, ref index);
          if (text[index] == '}') { position = ++index; return true; }
          if (text[index] != ',') { return false; }
          index++;
          Trim(ref text, ref index);
        }
        return false;
      }
      public static bool TryParseAny(ref string text, ref int position, out object value) {
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
      public static object Parse(string text, CustomParser custom = null) {
        var position = 0; custom = custom ?? TryParseAny;
        if (!custom(ref text, ref position, out var value)) { throw new System.Exception("Invalid Format"); }
        return value;
      }
    }
  }
}