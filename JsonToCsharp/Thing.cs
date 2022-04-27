namespace JsonToCsharp
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    
    public class Thing 
    {
        [JsonProperty("items")]
        public Items Items {get; set;}
    }
}
