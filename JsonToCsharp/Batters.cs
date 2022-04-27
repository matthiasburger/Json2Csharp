namespace JsonToCsharp
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    
    public class Batters 
    {
        [JsonProperty("batter")]
        public IList<Batter> Batters {get; set;}
    }
}
