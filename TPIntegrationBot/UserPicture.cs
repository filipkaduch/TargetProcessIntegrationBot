using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using System.Runtime.Serialization.Json;
using System.Collections.Specialized;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using Microsoft.SharePoint;
using System.Net.Http;
using RestSharp;
using RestSharp.Authenticators;
using modelsAlias = Microsoft.Bot.Connector.Teams.Models;
using Microsoft.Bot.Connector.Teams;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols;
using System.Configuration;
using System.Globalization;
using AdaptiveCards;

namespace TPIntegrationBot
{
    public static class UserPicture
    {
        public static async Task<byte[]> GetStreamWithAuthAsync(this HttpClient client, string accessToken, string endpoint)
        {
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            using (var response = await client.GetAsync(endpoint))
            {
                if (response.IsSuccessStatusCode)
                {
                    var stream = await response.Content.ReadAsStreamAsync();
                    byte[] bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, (int)stream.Length);
                    return bytes;
                }
                else
                    return null;
            }
        }
    }
}
