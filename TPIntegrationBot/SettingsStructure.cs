using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace TPIntegrationBot
{
    public class SettingsStructure
    {
        public string _FirstTimeOpened;
        public List<string> _AdminName;
        public string _TargetProcessUrl;
        public List<string> _ProjectNames;
        public List<string> _ProjectIds;
        public static List<string> ProjectNames { get; set; }
        public static List<string> ProjectIds { get; set; }

        public static string FirstTimeOpened { get; set; }

        public static List<string> AdminName { get; set; }
        public static string TargetProcessUrl { get; set; }

        //public SettingsStructure()
        //{
        //    //SettingsStructure = JsonConvert.DeserializeObject<SettingsStructure>(userSettings);
        //    //_TeamsChannelId = tempStruct._TeamsChannelId;
        //    //_FirstTimeOpened = _iconfig["FirstTimeOpened"];
        //}
    }
}
