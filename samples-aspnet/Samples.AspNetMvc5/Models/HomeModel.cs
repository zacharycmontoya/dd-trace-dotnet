using System.Collections.Generic;

namespace Samples.AspNetMvc5.Models
{
    public class HomeModel
    {
        public string[] HttpModules { get; set; }
        public IEnumerable<KeyValuePair<string, string>> EnvVars { get; set; }
    }
}
