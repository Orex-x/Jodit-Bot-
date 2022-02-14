using Newtonsoft.Json;

namespace Jodit_Bot_.Models
{
    public class GroupBot
    {
        [JsonProperty("idGroup")]
        
        public int IdGroup { get; set; }
        
        [JsonProperty("nameGroup")]
        public string NameGroup { get; set; }
    }
}