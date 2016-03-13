using System;
using System.Text;
using System.Collections.Generic;
using ICSharpCode.Decompiler;

namespace ManualILSpy.Extention.Json
{
    #region Json Member
    public enum JsonValueType
    {
        String,
        Number,
        Object,
        Array,
        Boolean,
        Null,
        Error
    }

    public abstract class JsonValue
    {
        protected JsonValueType type;

        public JsonValueType ValueType { get { return type; } }
        public abstract void AcceptWriter(ITextOutput writer);
        public abstract void AcceptVisitor(IJsonVisitor visitor);

        public string Comment; //this is an extension
    }

    public class JsonObject : JsonValue
    {
        public Dictionary<string, JsonValue> Values { get; private set; }

        public JsonObject()
        {
            Values = new Dictionary<string, JsonValue>();
            type = JsonValueType.Object;
        }

        public void AddJsonValue(string key, JsonValue value)
        {
            if (!Values.ContainsKey(key))
            {
                Values.Add(key, value);
            }
            else
            {
                throw new Exception("This key already contain value");
            }
        }
        public void AddJsonValue(string key, int value)
        {
            AddJsonValue(key, new JsonElement(value));
        }
        public void AddJsonValue(string key, string value)
        {
            AddJsonValue(key, new JsonElement(value));
        }
        public override void AcceptWriter(ITextOutput writer)
        {
            List<string> keyList = new List<string>(Values.Keys);
            int keyCount = keyList.Count;

            JsonValue jsonValue;
            bool isFirst = true;
            for (int i = 0; i < keyCount; i++)
            {
                if (isFirst)
                {
                    writer.Write('{');
                    isFirst = false;
                }
                else
                {
                    writer.Write(", ");
                }
                writer.Write('"');
                writer.Write(keyList[i]);
                writer.Write('"');
                writer.Write(':');
                if (Values.TryGetValue(keyList[i], out jsonValue))
                {
                    jsonValue.AcceptWriter(writer);
                }
            }
            if (!isFirst)
                writer.Write('}');
        }

        public override void AcceptVisitor(IJsonVisitor visitor)
        {
            visitor.VisitJsonObject(this);
        }
    }

    public class JsonArray : JsonValue
    {
        public List<JsonValue> ValueList { get; private set; }
        public int Count { get { return ValueList.Count; } }

        public JsonArray()
        {
            ValueList = new List<JsonValue>();
            type = JsonValueType.Array;
        }

        public void AddJsonValue(JsonValue value)
        {
            ValueList.Add(value);
        }

        public override void AcceptWriter(ITextOutput writer)
        {
            int count = ValueList.Count;
            bool isFirst = true;
            for (int i = 0; i < count; i++)
            {
                if (isFirst)
                {
                    writer.Write('[');
                    isFirst = false;
                }
                else
                {
                    writer.Write(", ");
                }
                ValueList[i].AcceptWriter(writer);
            }
            if (!isFirst)
                writer.Write(']');
        }

        public override void AcceptVisitor(IJsonVisitor visitor)
        {
            visitor.VisitJsonArray(this);
        }

        public override string ToString()
        {
            int count = ValueList.Count;
            if (count == 0)
            {
                return null;
            }
            string str = "[";
            foreach (var value in ValueList)
            {
                str += value.ToString();
            }
            str += "]";
            return base.ToString();
        }
    }

    public class JsonElement : JsonValue
    {
        public string ElementValue { get; private set; }
        public JsonElement()
        {
            ElementValue = "null";
            type = JsonValueType.Null;
        }

        public JsonElement(bool value)
        {
            if (value)
            {
                ElementValue = "true";
            }
            else
            {
                ElementValue = "false";
            }
            type = JsonValueType.Boolean;
        }

        public JsonElement(string value)
        {
            if (value != null)
            {
                ElementValue = value;
                type = JsonValueType.String;
            }
            else
            {
                ElementValue = "null";
                type = JsonValueType.Null;
            }
        }

        public JsonElement(int value)
        {
            ElementValue = value.ToString();
            type = JsonValueType.Number;
        }

        public JsonElement(long value)
        {
            ElementValue = value.ToString();
            type = JsonValueType.Number;
        }

        public JsonElement(float value)
        {
            ElementValue = value.ToString();
            type = JsonValueType.Number;
        }

        public JsonElement(double value)
        {
            ElementValue = value.ToString();
            type = JsonValueType.Number;
        }

        public JsonElement(decimal value)
        {
            ElementValue = value.ToString();
            type = JsonValueType.Number;
        }

        public void SetValueType(JsonValueType type)
        {
            this.type = type;
        }

        public void SetValue(bool value)
        {
            if (value)
            {
                ElementValue = "true";
            }
            else
            {
                ElementValue = "false";
            }
            type = JsonValueType.Boolean;
        }

        public void SetValue(string value)
        {
            if (value != null)
            {
                ElementValue = value;
                type = JsonValueType.String;
            }
            else
            {
                ElementValue = "null";
                type = JsonValueType.Null;
            }
        }

        public void SetValue(int value)
        {
            ElementValue = value.ToString();
            type = JsonValueType.Number;
        }

        public void SetValue(long value)
        {
            ElementValue = value.ToString();
            type = JsonValueType.Number;
        }

        public void SetValue(float value)
        {
            ElementValue = value.ToString();
            type = JsonValueType.Number;
        }

        public void SetValue(double value)
        {
            ElementValue = value.ToString();
            type = JsonValueType.Number;
        }

        public void SetValue(decimal value)
        {
            ElementValue = value.ToString();
            type = JsonValueType.Number;
        }

        public override void AcceptWriter(ITextOutput writer)
        {
            switch (ValueType)
            {
                case JsonValueType.String:
                    writer.Write('"' + ElementValue + '"');
                    break;
                default:
                    writer.Write(ElementValue);
                    break;
            }
        }

        public override void AcceptVisitor(IJsonVisitor visitor)
        {
            visitor.VisitJsonElement(this);
        }

        public override string ToString()
        {
            return ElementValue;
        }
    }
    #endregion

    #region Json Visitor and Writer
    public interface IJsonVisitor
    {
        void VisitJsonObject(JsonObject obj);
        void VisitJsonArray(JsonArray arr);
        void VisitJsonElement(JsonElement element);
    }

    enum BraceStyle
    {
        Array,
        Object
    }

    public class JsonWriterVisitor : IJsonVisitor
    {
        int braceCounter;
        ITextOutput writer;
        bool lastIsNewLine;
        public bool Debug { get; set; }
        public JsonWriterVisitor(ITextOutput output)
        {
            braceCounter = 0;
            writer = output;
            lastIsNewLine = true;
        }

        void OpenBrace(BraceStyle style)
        {
            WriteLine();
            switch (style)
            {
                case BraceStyle.Array:
                    Write('[');
                    break;
                case BraceStyle.Object:
                    Write('{');
                    break;
                default:
                    throw new Exception("Unknowed Brace Syyle");
            }
            braceCounter++;
            lastIsNewLine = false;
            WriteLine();
        }

        void CloseBrace(BraceStyle style)
        {
            braceCounter--;
            lastIsNewLine = false;
            WriteLine();
            switch (style)
            {
                case BraceStyle.Array:
                    Write(']');
                    break;
                case BraceStyle.Object:
                    Write('}');
                    break;
                default:
                    throw new Exception("Unknowed Brace Syyle");
            }
        }

        void Write(string value)
        {
            writer.Write(value);
            lastIsNewLine = false;
        }

        void Write(char ch)
        {
            writer.Write(ch);
            lastIsNewLine = false;
        }

        bool WriteComment(string comment)
        {
            if (comment != null)
            {
                WriteLine();
                writer.Write("\"!comment\":\"");
                writer.Write(comment);
                writer.Write('"');

                //writer.Write("/*" + comment + "*/");
                lastIsNewLine = false;
                return true;
            }
            return false;
        }

        void WriteLine()
        {
            if (lastIsNewLine)
            {
                return;
            }
            writer.WriteLine();
            writer.Write(new string('\t', braceCounter));//write space
            lastIsNewLine = true;
        }

        public void VisitJsonArray(JsonArray jsonArr)
        {
            if (jsonArr == null || jsonArr.Count == 0)
            {
                VisitNull();
                return;
            }

            OpenBrace(BraceStyle.Array);
            bool isFirst = true;
            if (Debug)
            {
                //comment member of
                if (jsonArr.Comment != null)
                {
                    writer.Write('{');
                    isFirst = (WriteComment(jsonArr.Comment) != true);
                    writer.Write('}');
                }
            }

            foreach (JsonValue value in jsonArr.ValueList)
            {
                if (isFirst)
                {
                    WriteLine();
                    isFirst = false;
                }
                else
                {
                    Write(',');
                    WriteLine();
                }
                if (value == null)
                {
                    VisitNull();
                }
                else
                {
                    value.AcceptVisitor(this);
                }
            }
            CloseBrace(BraceStyle.Array);
        }

        public void VisitJsonElement(JsonElement jsonElement)
        {
            if (jsonElement == null)
            {
                VisitNull();
                return;
            }
            switch (jsonElement.ValueType)
            {
                case JsonValueType.String:
                    Write('"' + jsonElement.ElementValue + '"');
                    break;
                case JsonValueType.Number:
                case JsonValueType.Boolean:
                case JsonValueType.Null:
                    Write(jsonElement.ElementValue);
                    break;
                default:
                    throw new Exception("Not element");
            }
        }

        public void VisitJsonObject(JsonObject jsonObj)
        {
            if (jsonObj == null)
            {
                VisitNull();
                return;
            }

            OpenBrace(BraceStyle.Object);
            bool isFirst = true;
            if (Debug)
            {
                isFirst = (WriteComment(jsonObj.Comment) != true);
            }

            List<string> keys = new List<string>(jsonObj.Values.Keys);
            JsonValue value;
            foreach (string key in keys)
            {
                if (isFirst)
                {
                    WriteLine();
                    isFirst = false;
                }
                else
                {
                    Write(',');
                    WriteLine();
                }
                if (jsonObj.Values.TryGetValue(key, out value))
                {
                    Write('"' + key + '"' + " : ");
                    if (value != null)
                    {
                        value.AcceptVisitor(this);
                    }
                    else
                    {
                        VisitNull();
                    }
                }
                else
                {
                    throw new Exception("Can't get value");
                }
            }
            CloseBrace(BraceStyle.Object);
        }

        void VisitNull()
        {
            Write("null");
        }

        public override string ToString()
        {
            return writer.ToString();
        }
    }

    public class JsonReader
    {
        string json;
        int length;
        int index;
        int CurrentIndex { get { return index; } }
        char character;
        char CurrentChar { get { return character; } }

        StringBuilder strBuilder;

        public JsonReader(string json)
        {
            this.json = json;
            length = json.Length;
            index = 0;
            strBuilder = new StringBuilder();
        }

        void eat()
        {
            if (index == length)//eat last character
            {
                index++;
                return;
            }
            else if (index > length)//eat after last character
            {
                throw new Exception("Out of length.");
            }
            character = json[index++];
        }

        public JsonValue Read()
        {
            JsonValueType type = GetJsonType();
            JsonValue value = ReadByType(type);
            return value;
        }

        JsonValue ReadByType(JsonValueType type)
        {
            JsonValue value;
            switch (type)
            {
                case JsonValueType.Array:
                    value = ReadJsonArray();
                    break;
                case JsonValueType.Object:
                    value = ReadJsonObject();
                    break;
                case JsonValueType.String:
                case JsonValueType.Error:
                    value = ReadJsonElement();//try read by element
                    break;
                default:
                    throw new Exception("Something wrong");
            }
            return value;
        }

        JsonValueType GetJsonType()
        {
            ClearSpace();
            switch (CurrentChar)
            {
                case '{': return JsonValueType.Object;
                case '[': return JsonValueType.Array;
                case '"':
                default: return JsonValueType.String;//try to read element
            }
        }

        JsonObject ReadJsonObject()
        {
            JsonObject obj = new JsonObject();
            if (CurrentChar != '{')
            {
                throw new Exception("The first char it's not object brace");
            }
            string key;
            JsonValueType rightJsonValueType;
            while (CurrentChar != '}')
            {
                eat();
                key = GetString();
                ClearSpace();
                if (CurrentChar != ':')
                {
                    throw new Exception("Something wrong");
                }
                eat();
                rightJsonValueType = GetJsonType();
                obj.AddJsonValue(key, ReadByType(rightJsonValueType));
                if (CurrentChar == ',')
                {
                    eat();
                }
                else
                {
                    ClearSpace();
                }
            }
            eat();//eat '}'
            return obj;
        }

        JsonArray ReadJsonArray()
        {
            JsonArray arr = new JsonArray();
            if (CurrentChar != '[')
            {
                throw new Exception("The first cha it's not array brace");
            }
            eat();
            while (CurrentChar != ']')
            {
                arr.AddJsonValue(ReadByType(GetJsonType()));
                if (CurrentChar == ',')
                {
                    eat();
                }
                else
                {
                    ClearSpace();
                }
            }
            eat();//eat ']'
            return arr;
        }

        JsonElement ReadJsonElement()
        {
            JsonElement element = new JsonElement();
            switch (CurrentChar)
            {
                case '"': //read string
                    element.SetValue(GetString());
                    break;
                case 'n': //try read null
                    if (GetStringUtilSpaceOrComma() == "null")
                    {
                        element = new JsonElement(null);
                        break;
                    }
                    element.SetValueType(JsonValueType.Error);
                    break;
                case 't': //try read true
                    if (GetStringUtilSpaceOrComma() == "true")
                    {
                        element = new JsonElement(true);
                        break;
                    }
                    element.SetValueType(JsonValueType.Error);
                    break;
                case 'f': //try read false
                    if (GetStringUtilSpaceOrComma() == "false")
                    {
                        element = new JsonElement(false);
                        break;
                    }
                    element.SetValueType(JsonValueType.Error);
                    break;
                default: //try read number or error
                    element = TryCatchNumber(GetStringUtilSpaceOrComma());
                    break;
            }
            if (element.ValueType == JsonValueType.Error)
            {
                throw new Exception("Something wrong");
            }
            return element;
        }

        string GetString()
        {
            strBuilder.Clear();
            ClearSpace();
            if (CurrentChar != '"')
            {
                throw new Exception("It's not string element");
            }
            eat();
            while (CurrentChar != '"')
            {
                strBuilder.Append(CurrentChar);
                eat();
            }
            eat();
            return strBuilder.ToString();
        }

        string GetStringUtilSpaceOrComma()
        {
            strBuilder.Clear();
            strBuilder.Append(CurrentChar);
            bool isBreaker = false;
            while (!isBreaker)
            {
                eat();
                if (IsSpace(CurrentChar) || CurrentChar == ',')
                {
                    isBreaker = true;
                }
                else
                {
                    strBuilder.Append(CurrentChar);
                }
            }
            return strBuilder.ToString();
        }

        JsonElement TryCatchNumber(string str)
        {
            JsonElement element = new JsonElement();
            long numInteger;
            double numDouble;
            decimal numDecimal;
            if (Int64.TryParse(str, out numInteger))
            {
                return new JsonElement(numInteger);
            }
            else if (Double.TryParse(str, out numDouble))
            {
                return new JsonElement(numDouble);
            }
            else if (Decimal.TryParse(str, out numDecimal))
            {
                return new JsonElement(numDecimal);
            }
            else
            {
                element.SetValueType(JsonValueType.Error);
                return element;
            }
        }

        void ClearSpace()
        {
            while (IsSpace(CurrentChar))
            {
                eat();
            }
        }

        bool IsSpace(char ch)
        {
            switch (ch)
            {
                case ' ':
                case '\r':
                case '\n':
                case '\t':
                case '\0':
                    return true;
                default:
                    return false;
            }
        }
    }
    #endregion
}