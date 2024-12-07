// Decompiled with JetBrains decompiler
// Type: GDMiniJSON.Json
// Assembly: Assembly-CSharp-firstpass, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 0CE6B9F5-BB38-46E3-BBB2-F8A081EDEC68
// Assembly location: F:\Steam\steamapps\common\A Dance of Fire and Ice\A Dance of Fire and Ice_Data\Managed\Assembly-CSharp-firstpass.dll

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CustomHitSound
{
  public static class Json
  {
    public static object Deserialize(string json)
    {
      return json == null ? (object) null : Json.Parser.Parse(json);
    }

    public static object DeserializePartially(string json, string upToSection)
    {
      return json == null ? (object) null : Json.Parser.ParsePartially(json, upToSection);
    }

    public static string Serialize(object obj) => Json.Serializer.Serialize(obj);

    private sealed class Parser : IDisposable
    {
      private const string WHITE_SPACE = " \t\n\r\uFEFF";
      private const string WORD_BREAK = " \t\n\r{}[],:\"";
      private StringReader json;
      private string endSection;

      private Parser(string jsonString)
      {
        this.json = new StringReader(jsonString);
        if (this.json.Peek() != 65279)
          return;
        this.json.Read();
      }

      public static object Parse(string jsonString)
      {
        using (Json.Parser parser = new Json.Parser(jsonString))
          return parser.ParseValue();
      }

      public static object ParsePartially(string jsonString, string upToSection)
      {
        using (Json.Parser parser = new Json.Parser(jsonString))
        {
          parser.endSection = upToSection;
          return parser.ParseValue();
        }
      }

      public void Dispose()
      {
        this.json.Dispose();
        this.json = (StringReader) null;
      }

      private Dictionary<string, object> ParseObject()
      {
        Dictionary<string, object> dictionary = new Dictionary<string, object>();
        this.json.Read();
        while (true)
        {
          Json.Parser.TOKEN nextToken;
          do
          {
            nextToken = this.NextToken;
            if (nextToken != Json.Parser.TOKEN.NONE)
            {
              if (nextToken == Json.Parser.TOKEN.CURLY_CLOSE)
                goto label_5;
            }
            else
              goto label_4;
          }
          while (nextToken == Json.Parser.TOKEN.COMMA);
          string str = this.ParseString();
          if (str != null)
          {
            if (this.NextToken == Json.Parser.TOKEN.COLON)
            {
              if (this.endSection == null || !string.Equals(str, this.endSection))
              {
                this.json.Read();
                dictionary[str] = this.ParseValue();
              }
              else
                goto label_11;
            }
            else
              goto label_9;
          }
          else
            goto label_7;
        }
label_4:
        return (Dictionary<string, object>) null;
label_5:
        return dictionary;
label_7:
        return (Dictionary<string, object>) null;
label_9:
        return (Dictionary<string, object>) null;
label_11:
        return dictionary;
      }

      private List<object> ParseArray()
      {
        List<object> array = new List<object>();
        this.json.Read();
        bool flag = true;
        while (flag)
        {
          Json.Parser.TOKEN nextToken = this.NextToken;
          switch (nextToken)
          {
            case Json.Parser.TOKEN.NONE:
              return (List<object>) null;
            case Json.Parser.TOKEN.SQUARED_CLOSE:
              flag = false;
              continue;
            case Json.Parser.TOKEN.COMMA:
              continue;
            default:
              object byToken = this.ParseByToken(nextToken);
              array.Add(byToken);
              continue;
          }
        }
        return array;
      }

      private object ParseValue() => this.ParseByToken(this.NextToken);

      private object ParseByToken(Json.Parser.TOKEN token)
      {
        switch (token)
        {
          case Json.Parser.TOKEN.CURLY_OPEN:
            return (object) this.ParseObject();
          case Json.Parser.TOKEN.SQUARED_OPEN:
            return (object) this.ParseArray();
          case Json.Parser.TOKEN.STRING:
            return (object) this.ParseString();
          case Json.Parser.TOKEN.NUMBER:
            return this.ParseNumber();
          case Json.Parser.TOKEN.TRUE:
            return (object) true;
          case Json.Parser.TOKEN.FALSE:
            return (object) false;
          case Json.Parser.TOKEN.NULL:
            return (object) null;
          default:
            return (object) null;
        }
      }

      private string ParseString()
      {
        StringBuilder stringBuilder1 = new StringBuilder();
        this.json.Read();
        bool flag = true;
        while (flag)
        {
          if (this.json.Peek() == -1)
            break;
          char nextChar1 = this.NextChar;
          switch (nextChar1)
          {
            case '"':
              flag = false;
              continue;
            case '\\':
              if (this.json.Peek() == -1)
              {
                flag = false;
                continue;
              }
              char nextChar2 = this.NextChar;
              switch (nextChar2)
              {
                case '"':
                case '/':
                case '\\':
                  stringBuilder1.Append(nextChar2);
                  continue;
                case 'b':
                  stringBuilder1.Append('\b');
                  continue;
                case 'f':
                  stringBuilder1.Append('\f');
                  continue;
                case 'n':
                  stringBuilder1.Append('\n');
                  continue;
                case 'r':
                  stringBuilder1.Append('\r');
                  continue;
                case 't':
                  stringBuilder1.Append('\t');
                  continue;
                case 'u':
                  StringBuilder stringBuilder2 = new StringBuilder();
                  for (int index = 0; index < 4; ++index)
                    stringBuilder2.Append(this.NextChar);
                  stringBuilder1.Append((char) Convert.ToInt32(stringBuilder2.ToString(), 16));
                  continue;
                default:
                  continue;
              }
            default:
              stringBuilder1.Append(nextChar1);
              continue;
          }
        }
        return stringBuilder1.ToString();
      }

      private object ParseNumber()
      {
        string nextWord = this.NextWord;
        if (nextWord.IndexOf('.') == -1)
        {
          int result;
          int.TryParse(nextWord, out result);
          return (object) result;
        }
        float result1;
        float.TryParse(nextWord, out result1);
        return (object) result1;
      }

      private void EatWhitespace()
      {
        while (" \t\n\r\uFEFF".IndexOf(this.PeekChar) != -1)
        {
          this.json.Read();
          if (this.json.Peek() == -1)
            break;
        }
      }

      private char PeekChar => Convert.ToChar(this.json.Peek());

      private char NextChar => Convert.ToChar(this.json.Read());

      private string NextWord
      {
        get
        {
          StringBuilder stringBuilder = new StringBuilder();
          while (" \t\n\r{}[],:\"".IndexOf(this.PeekChar) == -1)
          {
            stringBuilder.Append(this.NextChar);
            if (this.json.Peek() == -1)
              break;
          }
          return stringBuilder.ToString();
        }
      }

      private Json.Parser.TOKEN NextToken
      {
        get
        {
          this.EatWhitespace();
          if (this.json.Peek() == -1)
            return Json.Parser.TOKEN.NONE;
          switch (this.PeekChar)
          {
            case '"':
              return Json.Parser.TOKEN.STRING;
            case ',':
              this.json.Read();
              return Json.Parser.TOKEN.COMMA;
            case '-':
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
              return Json.Parser.TOKEN.NUMBER;
            case ':':
              return Json.Parser.TOKEN.COLON;
            case '[':
              return Json.Parser.TOKEN.SQUARED_OPEN;
            case ']':
              this.json.Read();
              return Json.Parser.TOKEN.SQUARED_CLOSE;
            case '{':
              return Json.Parser.TOKEN.CURLY_OPEN;
            case '}':
              this.json.Read();
              return Json.Parser.TOKEN.CURLY_CLOSE;
            default:
              switch (this.NextWord)
              {
                case "false":
                  return Json.Parser.TOKEN.FALSE;
                case "true":
                  return Json.Parser.TOKEN.TRUE;
                case "null":
                  return Json.Parser.TOKEN.NULL;
                default:
                  return Json.Parser.TOKEN.NONE;
              }
          }
        }
      }

      private enum TOKEN
      {
        NONE,
        CURLY_OPEN,
        CURLY_CLOSE,
        SQUARED_OPEN,
        SQUARED_CLOSE,
        COLON,
        COMMA,
        STRING,
        NUMBER,
        TRUE,
        FALSE,
        NULL,
      }
    }

    private sealed class Serializer
    {
      private StringBuilder builder;

      private Serializer() => this.builder = new StringBuilder();

      public static string Serialize(object obj)
      {
        Json.Serializer serializer = new Json.Serializer();
        serializer.SerializeValue(obj);
        return serializer.builder.ToString();
      }

      private void SerializeValue(object value)
      {
        switch (value)
        {
          case null:
            this.builder.Append("null");
            break;
          case string str:
            this.SerializeString(str);
            break;
          case bool _:
            this.builder.Append(value.ToString().ToLower());
            break;
          case IList anArray:
            this.SerializeArray(anArray);
            break;
          case IDictionary dictionary:
            this.SerializeObject(dictionary);
            break;
          case char _:
            this.SerializeString(value.ToString());
            break;
          default:
            this.SerializeOther(value);
            break;
        }
      }

      private void SerializeObject(IDictionary obj)
      {
        bool flag = true;
        this.builder.Append("{\n");
        foreach (object key in (IEnumerable) obj.Keys)
        {
          if (!flag)
            this.builder.Append(",\n");
          this.SerializeString(key.ToString());
          this.builder.Append(':');
          this.SerializeValue(obj[key]);
          flag = false;
        }
        this.builder.Append("\n}");
      }

      private void SerializeArray(IList anArray)
      {
        this.builder.Append('[');
        bool flag = true;
        foreach (object an in (IEnumerable) anArray)
        {
          if (!flag)
            this.builder.Append(',');
          this.SerializeValue(an);
          flag = false;
        }
        this.builder.Append(']');
      }

      private void SerializeString(string str)
      {
        this.builder.Append('"');
        foreach (char ch in str.ToCharArray())
        {
          switch (ch)
          {
            case '\b':
              this.builder.Append("\\b");
              break;
            case '\t':
              this.builder.Append("\\t");
              break;
            case '\n':
              this.builder.Append("\\n");
              break;
            case '\f':
              this.builder.Append("\\f");
              break;
            case '\r':
              this.builder.Append("\\r");
              break;
            case '"':
              this.builder.Append("\\\"");
              break;
            case '\\':
              this.builder.Append("\\\\");
              break;
            default:
              int int32 = Convert.ToInt32(ch);
              if (int32 >= 32 && int32 <= 126)
              {
                this.builder.Append(ch);
                break;
              }
              this.builder.Append("\\u" + Convert.ToString(int32, 16).PadLeft(4, '0'));
              break;
          }
        }
        this.builder.Append('"');
      }

      private void SerializeOther(object value)
      {
        switch (value)
        {
          case float _:
          case int _:
          case uint _:
          case long _:
          case double _:
          case sbyte _:
          case byte _:
          case short _:
          case ushort _:
          case ulong _:
          case Decimal _:
            this.builder.Append(value.ToString());
            break;
          default:
            this.SerializeString(value.ToString());
            break;
        }
      }
    }
  }
}
