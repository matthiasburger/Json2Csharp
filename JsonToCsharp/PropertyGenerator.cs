namespace JsonToCsharp
{
    public class PropertyGenerator
    {
        private readonly Property _property;

        private const string PropertyCode = @"        [JsonProperty(""{0}"")]
        public {1} {2} {{get; set;}}";

        public PropertyGenerator(Property property)
        {
            _property = property;
        }
        
        public string GetCode()
        {
            string propertyType = _property.Type;
            if (_property.IsList)
                propertyType = $"IList<{propertyType}>";

            return string.Format(PropertyCode, _property.JsonName, propertyType, _property.GetPropertyName());
        }
    }
}