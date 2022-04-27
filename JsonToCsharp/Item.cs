namespace JsonToCsharp
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    
    public class Item 
    {
        [JsonProperty("things")]
        public IList<string> Things {get; set;}
        [JsonProperty("id")]
        public string Id {get; set;}
        [JsonProperty("type")]
        public string Type {get; set;}
        [JsonProperty("name")]
        public string Name {get; set;}
        [JsonProperty("ppu")]
        public float Ppu {get; set;}
        [JsonProperty("batters")]
        public Batters Batters {get; set;}
        [JsonProperty("topping")]
        public IList<Topping> Toppings {get; set;}
    }
}
