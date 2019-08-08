// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio EmptyBot v4.3.0

using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using System.Runtime.Serialization.Json;
using System.Collections.Specialized;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Collections.Generic;

namespace TPIntegrationBot
{
    public class ConfigurationCredentialProvider : SimpleCredentialProvider
    {
        public ConfigurationCredentialProvider(IConfiguration configuration)
            : base(configuration["MicrosoftAppId"], configuration["MicrosoftAppPassword"])
        {
            string jsonSettings;
            JObject settingsObj;
            using (StreamReader r = new StreamReader("UserSettings.json"))
            {
                jsonSettings = r.ReadToEnd();
                settingsObj = JObject.Parse(jsonSettings);
            }
            
            SettingsStructure.FirstTimeOpened = (string)settingsObj["_FirstTimeOpened"];
            SettingsStructure.AdminName = settingsObj["_AdminName"].ToObject<List<string>>();
            SettingsStructure.ProjectNames = settingsObj["_ProjectNames"].ToObject<List<string>>(); 
            SettingsStructure.ProjectIds = settingsObj["_ProjectIds"].ToObject<List<string>>();
            SettingsStructure.TargetProcessUrl = (string)settingsObj["_TargetProcessUrl"];
            //var pNames = JArray.Parse(settingsObj).Children()["_ProjectNames"]
        }
    }
}
