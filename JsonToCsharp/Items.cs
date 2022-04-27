namespace JsonToCsharp
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    
    public class Items 
    {
        [JsonProperty("item")]
        public IList<Item> Items {get; set;}
    }
}
