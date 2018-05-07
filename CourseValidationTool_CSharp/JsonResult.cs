using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CourseValidationTool_CSharp
{
    public class JsonResult
    {
        [JsonProperty("notetitle")]
        public string notetitle { get; set; }
        [JsonProperty("linkedvideo")]
        public List<string>linkedvideo { get; set; }
        [JsonProperty("notecontent")]
        public List<Notecontent> notecontent { get; set; }
    }

    public class Notecontent
    {
        public string phasetitle { get; set; }
        public List<string> phasecontent { get; set; }
        public List<Sectioncontent> sectioncontent { get; set; }
    }

    public class Sectioncontent
    {
        public List<string> section { get; set; }
    }
}
