using System;
using System.Collections.Generic;
using System.Linq;

using IronSphere.Extensions;

namespace JsonToCsharp
{
    public class NetCodeWriter
    {
        private readonly IEnumerable<Clazz> _classes;

        private const string NamespaceCode = @"namespace {0}
{{
{1}
    {2}
}}
";

        public NetCodeWriter(IEnumerable<Clazz> classes)
        {
            _classes = classes;
        }

        public IList<string> Imports { get; set; } = new List<string>
        {
            "System",
            "System.Collections.Generic",
            "System.Globalization",
            "Newtonsoft.Json",
            "Newtonsoft.Json.Converters"
        };

        public string Namespace { get; set; } = "None";


        public string GetCode()
        {
            return string.Format(NamespaceCode, Namespace, Environment.NewLine.Join(_getImports()), 
                Environment.NewLine.Join(_classes.Select(y=>new ClassGenerator(y).GetCode())));
        }

        private IEnumerable<string> _getImports()
        {
            return Imports.Select(import => $"    using {import};");
        }
    }
}