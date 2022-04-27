namespace JsonToCsharp
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    
    public class Topping 
    {
        [JsonProperty("id")]
        public string Id {get; set;}
        [JsonProperty("type")]
        public string Type {get; set;}
    }
}
