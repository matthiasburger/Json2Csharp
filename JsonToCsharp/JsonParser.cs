using System;
using System.Collections.Generic;
using System.Linq;

using IronSphere.Extensions;

using Newtonsoft.Json.Linq;

namespace JsonToCsharp
{
    public class JsonParser
    {
        private readonly string _json;

        public IList<Clazz> Classes { get; set; } = new List<Clazz>();
        public string InitialClassName { get; set; } = "initial_class";

        public JsonParser(string json)
        {
            _json = json.Trim();
        }

        public void Parse()
        {
            if (_json.StartsWith("{"))
            {
                JObject root = JObject.Parse(_json);
                Clazz rootClass = new Clazz(InitialClassName);
                Classes.Add(rootClass);
                _parse(root, rootClass);
            }
            else if (_json.StartsWith("["))
            {
                JArray root = JArray.Parse(_json);
                Clazz rootClass = new Clazz(InitialClassName);
                Classes.Add(rootClass);
                _parseArray(root, rootClass);
            }
            else throw new Exception($"cannot parse json with start \"{_json.CutAt(5, "...")}\"");
        }

        private Clazz _getOrCreateClazz(string propertyName)
        {
            Clazz clazz = Classes.FirstOrDefault(x => x.Name == propertyName);
            if (clazz != null)
                return clazz;

            clazz = new Clazz(propertyName);
            Classes.Add(clazz);
            return clazz;
        }

        private void _parse(JObject root, Clazz clazz)
        {
            IEnumerable<JProperty> x = root.Properties();
            foreach (JProperty jProperty in x)
            {
                string type;
                bool isList = false;

                switch (jProperty.Value.Type)
                {
                    case JTokenType.Object:
                        {
                            Clazz c = _getOrCreateClazz(jProperty.Name);
                            type = c.GetClassName();
                            _parse((JObject)jProperty.Value, c);
                            break;
                        }
                    case JTokenType.Array:
                        {
                            Clazz c = _getOrCreateClazz(jProperty.Name);
                            type = c.GetClassName();
                            _parseArray((JArray)jProperty.Value, c);
                            isList = true;
                            break;
                        }
                    default:
                        type = _getClrType(jProperty.Value.Type);
                        break;
                }

                clazz.AddProperty(new Property
                {
                    Name = jProperty.Name,
                    JsonName = jProperty.Name,
                    Type = type,
                    IsList = isList
                });
            }
        }

        private void _parseArray(JArray root, Clazz clazz)
        {
            foreach (JToken jToken in root)
            {
                if (jToken.Type == JTokenType.Object)
                    _parse((JObject)jToken, clazz);
            }
        }

        private string _getClrType(JTokenType tokenType)
        {
            switch (tokenType)
            {
                case JTokenType.None:
                    return "object";
                case JTokenType.Object:
                    return "object";
                case JTokenType.Array:
                    return "ArrayList";
                case JTokenType.Integer:
                    return "int";
                case JTokenType.Float:
                    return "float";
                case JTokenType.String:
                    return "string";
                case JTokenType.Boolean:
                    return "bool";
                case JTokenType.Null:
                    return "object";
                case JTokenType.Undefined:
                    return "object";
                case JTokenType.Date:
                    return "DateTime";
                case JTokenType.Bytes:
                    return "byte[]";
                case JTokenType.Guid:
                    return "Guid";
                case JTokenType.Uri:
                    return "string"; // or Uri?
                case JTokenType.TimeSpan:
                    return "TimeSpan";
                default:
                    throw new ArgumentOutOfRangeException(nameof(tokenType), tokenType, null);
            }
        }
    }
}
