using System;
using System.Collections.Generic;
using System.Linq;

using IronSphere.Extensions;

namespace JsonToCsharp
{
    public class ClassGenerator
    {
        private readonly Clazz _clazz;
        private const string ClassHeader = @"
    public class {0} 
    {{
{1}
    }}";

        public ClassGenerator(Clazz clazz)
        {
            _clazz = clazz;
        }

        public string GetCode() => string.Format(ClassHeader, _clazz.GetClassName(), _getPropertiesCode());

        private string _getPropertiesCode()
        {
            List<string> propertiesCodes = new List<string>(_clazz.Properties.Count);
            propertiesCodes.AddRange(
                _clazz.Properties
                    .Select(clazzProperty => new PropertyGenerator(clazzProperty))
                    .Select(generator => generator.GetCode())
                );

            return Environment.NewLine.Join(propertiesCodes);
        }
    }
}