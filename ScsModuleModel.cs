using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace tds2scs
{
    internal class ScsModuleModel
    {

        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class Rule
        {
            [JsonProperty("path")]
            public string Path { get; set; }
            
            [JsonProperty("allowedPushOperations", NullValueHandling = NullValueHandling.Ignore)]
            public string AllowedPushOperations { get; set; }

            [JsonProperty("scope")]
            public string Scope { get; set; }

            [JsonProperty("alias", NullValueHandling = NullValueHandling.Ignore)]
            public string Alias { get; set; }
        }

        public class Include
        {
            [JsonProperty("allowedPushOperations", NullValueHandling = NullValueHandling.Ignore)]
            public string AllowedPushOperations { get; set; }

            [JsonProperty("maxRelativePathLength", NullValueHandling = NullValueHandling.Ignore)]
            public string MaxRelativePathLength { get; set; }

            [JsonProperty("name")] // Required
            public string Name { get; set; }

            [JsonProperty("path")] // Required
            public string Path { get; set; }

            [JsonProperty("scope", NullValueHandling = NullValueHandling.Ignore)]
            public string Scope { get; set; }

            [JsonProperty("database", NullValueHandling = NullValueHandling.Ignore)]
            public string Database { get; set; }

            [JsonProperty("rules")]
            public List<Rule> Rules { get; set; }
        }

        public class Items
        {
            [JsonProperty("includes")]
            public List<Include> Includes { get; set; }

            [JsonProperty("path", NullValueHandling = NullValueHandling.Ignore)]
            public string Path { get; set; }
        }

        public class Root
        {
            [JsonProperty("namespace")]            
            public string @Namespace { get; set; }

            [JsonProperty("items")]
            public Items Items { get; set; }

            [JsonProperty("description")]
            public string Description { get; set; }

            [JsonProperty("tags", NullValueHandling = NullValueHandling.Ignore)]
            public List<string> Tags { get; set; }
        }


    }
}