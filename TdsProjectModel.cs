using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace tds2scs
{
    public class TdsProjectModel
    {

        [XmlRoot("Project", Namespace = "http://schemas.microsoft.com/developer/msbuild/2003")]
        public class Project
        {
            [XmlArrayItem(ElementName = "SitecoreItem")]
            public List<SitecoreItem> ItemGroup { get; set; }
        }

        public class SitecoreItem
        {
            [XmlAttribute("Include")]
            public string Include { get; set; }
            [XmlAttribute("ChildItemSynchronization")]
            public string ChildItemSynchronization { get; set; }
            [XmlAttribute("SitecoreName")] 
            public string SitecoreName { get; set; }
            [XmlAttribute("ItemDeployment")] 
            public string ItemDeployment { get; set; }
        }
    }
}
