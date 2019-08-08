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
    public class AttachmentsBot : ActivityHandler
    {
        private static string authToken = "";
        private static string chatId = "";
        private static string selectedEntity = "";
        private static string entityName = "";
        private static string projectName = "";
        private static string userName = "";
        private static string entityDescription = "";
        private static string fileUrl = "";
        private static int dialogCounter = 0;
        private static int openingDialogCounter = 0;
        private static List<string> attachementsUrl = new List<string>();
        private static List<string> previewUrl = new List<string>();
        private static bool hasBegun = false;
        public string serviceUrl = "";
        public static string localDownloadUrl = "";
        private static List<Payload> currentPayloads = new List<Payload>();
        private static List<string> adminList = new List<string>(SettingsStructure.AdminName);
        private static List<string> memberList = new List<string>();
        private static List<int> memberIdList = new List<int>();
        private static List<string> memberRoles = new List<string>();
        private static Dictionary<string, int> nameId = new Dictionary<string, int>();
        private static List<CardAction> devButtons = new List<CardAction>();

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            
            await SendWelcomeMessageAsync(turnContext, cancellationToken);
            //await DisplayOptionsAsync(turnContext, cancellationToken);
        }
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            string adminReplyText = "";

            //Check for first launch of bot
            if (SettingsStructure.FirstTimeOpened == "false")
            {                              
                var adminNameCheck = turnContext.Activity.From.Name;
                var checkedName = SettingsStructure.AdminName.FirstOrDefault(r => r == adminNameCheck);
                var adminTxt = turnContext.Activity.Text;

                if (adminTxt != null)
                {
                    if (adminTxt.ToLower() == "admin" && checkedName != null)
                    {
                        var processedPay = currentPayloads.FirstOrDefault(r => r.Id == turnContext.Activity.From.Name);
                        if (processedPay != null)
                            currentPayloads.RemoveAll(s => s.Id == turnContext.Activity.From.Name);
                        SettingsStructure.FirstTimeOpened = "true";
                    }
                    else if (adminTxt.ToLower() == "admin" && checkedName == null)
                    {
                        var processedPay = currentPayloads.FirstOrDefault(r => r.Id == turnContext.Activity.From.Name);
                        if (processedPay != null)
                            currentPayloads.RemoveAll(s => s.Id == turnContext.Activity.From.Name);
                        var replyAdmin = ProcessAdminRights(turnContext);
                        adminReplyText = replyAdmin.Text;
                        await turnContext.SendActivityAsync(replyAdmin, cancellationToken);
                    }
                }
            }

            if (SettingsStructure.FirstTimeOpened == "true")
            {
                var replyCard = turnContext.Activity;
                
                MicrosoftAppCredentials.TrustServiceUrl(turnContext.Activity.ServiceUrl);
                MicrosoftAppCredentials mscred = new MicrosoftAppCredentials("cc7ad832-acee-4df4-ac3a-471e180785ec", "blcKCSHN7()cbceJM2218$*");
                ConnectorClient connector = new ConnectorClient(new System.Uri(turnContext.Activity.ServiceUrl), mscred);
                
                var openingDialog = ProcessOpening(turnContext);
                await connector.Conversations.SendToConversationAsync(openingDialog, cancellationToken);                
                //await turnContext.SendActivityAsync(MessageFactory.Attachment(CreateAdaptiveCardAttachment(turnContext)), cancellationToken);              
            }
            else if(adminReplyText == "" || adminReplyText == null)
            {
                var processedPay = currentPayloads.FirstOrDefault(r => r.Id == turnContext.Activity.From.Name);
                var rstText = turnContext.Activity.Text;

                if (rstText != null)
                {
                    if (rstText.ToLower() == "reset")
                    {
                        currentPayloads.RemoveAll(s => s.Id == turnContext.Activity.From.Name);
                        ResetReport(turnContext.Activity);
                    }
                }
                if(processedPay == null)
                {
                    await SendWelcomeMessageAsync(turnContext, cancellationToken);
                }
                else if (processedPay.HasBegun == false)
                {
                    await SendWelcomeMessageAsync(turnContext, cancellationToken);
                }
                else
                {
                    var reply = ProcessInput(turnContext);
                    await turnContext.SendActivityAsync(reply, cancellationToken);
                }
            }
        }

        private static void ResetReport(IMessageActivity activity)
        {
            var payloadToReset = currentPayloads.FirstOrDefault(r => r.Id == activity.From.Name);
            if (payloadToReset != null)
            {
                payloadToReset.DialogCounter = 0;
                payloadToReset.Entity = "";
                payloadToReset.Project = "";
                payloadToReset.Text = "";
                payloadToReset.Title = "";
                payloadToReset.HasBegun = false;
                payloadToReset.Developer.Clear();
                payloadToReset.Category.Clear();
            }
            dialogCounter = 0;
            selectedEntity = "";
            projectName = "";
            entityDescription = "";
            attachementsUrl.Clear();
            entityName = "";
            hasBegun = false;
        }

        private static async Task DisplayOptionsAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            Payload currentPayload = new Payload();
            currentPayloads.Add(currentPayload);
            currentPayload.Id = turnContext.Activity.From.Name;
            currentPayload.DialogCounter = 0;
            currentPayload.HasBegun = true;
            currentPayload.Category = new List<string>();
            currentPayload.Developer = new List<string>();          

            var reply = turnContext.Activity.CreateReply();       

            // Create a HeroCard with options for the user to interact with the bot.
            var card = new HeroCard
            {
                Title = "What kind of entity would you like to create?",
                Buttons = new List<CardAction>
                {
                    new CardAction(ActionTypes.ImBack, title: "1. Bug", value: "1"),
                    new CardAction(ActionTypes.ImBack, title: "2. Epic ", value: "2"),
                    new CardAction(ActionTypes.ImBack, title: "3. User story ", value: "3"),
                    new CardAction(ActionTypes.ImBack, title: "4. Feature ", value: "4"),
                    //new CardAction(ActionTypes.ImBack, title: "5. Task", value: "5"),
                },
            };

            // Add the card to our reply.
            reply.Attachments = new List<Attachment>() { card.ToAttachment() };
            await turnContext.SendActivityAsync(reply, cancellationToken);
        }

        // Greet the user and give them instructions on how to interact with the bot.
        private static async Task SendWelcomeMessageAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            await DisplayOptionsAsync(turnContext, cancellationToken);
            hasBegun = true;
        }

        private static Attachment CreateAdaptiveCardAttachment(ITurnContext turnContext)
        {
            var adaptiveCardJson = File.ReadAllText("AdminSettingsCard.json");
            AdaptiveCard ac = new AdaptiveCard(adaptiveCardJson);

            var adaptiveCardAttachment = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,//"application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(adaptiveCardJson),
            };
            return adaptiveCardAttachment;
        }

        private static Activity ProcessAdminRights(ITurnContext turnContext)
        {
            var reply = turnContext.Activity.CreateReply();
            reply.Text = "You do not have permission to edit admin settings!";
            return reply;
        }

        private static Activity ProcessOpening(ITurnContext turnContext)
        {
            const string checkText = "Are you satisfied with your settings: ";
            const string closingText = "Alright, you are ready to use this bot for reporting entities in TargetProcess! Wake up this bot with any message. ";
            const string adminText1 = "Would you like to promote ";
            const string adminText2 = " to a new Bot admin?";
            const string adminDeclineText = "Promoting user to admin declined!";
            const string adminPromoteCheckText = " is already a Bot admin!";

            var reply = turnContext.Activity.CreateReply();           

            switch (openingDialogCounter)
            {
                case 0:
                    {                       
                        var adaptiveCardJson = File.ReadAllText("AdminSettingsCard.json");
                        AdaptiveCardParseResult result = AdaptiveCard.FromJson(adaptiveCardJson);

                        AdaptiveCard cardAc = result.Card;
                        var adaptiveCardAttachment = new Attachment()
                        {
                            ContentType = "application/vnd.microsoft.card.adaptive",
                            Content = cardAc,
                        };

                        reply.Attachments = new List<Attachment>() { adaptiveCardAttachment };

                        openingDialogCounter++;
                        break;
                    }
                case 1:
                    {
                        dynamic answers = (JObject)turnContext.Activity.Value;
                        string tpUrl = answers.tpSetUrl.ToString();
                        
                        string basicAdminName = turnContext.Activity.From.Name;
                        string basicAdminNameCheck = SettingsStructure.AdminName.FirstOrDefault(r => r == basicAdminName);
                        if(basicAdminNameCheck == null)
                        {
                            adminList.Add(basicAdminName);
                        }                      

                        string adminName = answers.adminSetRights.ToString();
                        string adminNameCheck = SettingsStructure.AdminName.FirstOrDefault(r => r == adminName);
                        if(adminName != null && adminName != "" && adminNameCheck == null)
                        {
                            adminList.Add(adminName);
                        }

                        //Get Project IDs and Names from TP api
                        string queryGet = "projects?where=(EntityState.IsFinal%20eq%20%27false%27)";
                        
                        HttpClient client = new HttpClient();
                        client.BaseAddress = new Uri(SettingsStructure.TargetProcessUrl + "api/v1/");
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                        string authentication = Convert.ToBase64String(Encoding.ASCII.GetBytes("filip.kaduch@instarea.com:magduska89"));
                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authentication);
                        HttpResponseMessage responseGet = client.GetAsync(queryGet).Result;
                        if (responseGet.IsSuccessStatusCode == true)
                        {
                            var returnedProjects = responseGet.Content.ReadAsStringAsync().Result;
                            GetProjectsDictionary(returnedProjects);
                        }
                        


                        if (answers.tpSetUrl.ToString() != "")
                            SettingsStructure.TargetProcessUrl = answers.tpSetUrl.ToString();

                        SettingsStructure.AdminName = new List<string>(adminList);

                        if (tpUrl != "")
                        {
                            var card = new HeroCard
                            {
                                Title = checkText,
                                Text = $"<b>TargetProcess URL:</b> " + SettingsStructure.TargetProcessUrl + /*$" <br></br><b>Project names:</b> " + stringNames + $"<br></br><b>Project Ids:</b> " + stringIds +*/ $"<br></br>",
                                Buttons = new List<CardAction>
                            {
                                new CardAction(ActionTypes.ImBack, title: "Confirm", value: "confirm"),
                                new CardAction(ActionTypes.ImBack, title: "Delete", value: "delete"),
                            },
                            };
                            reply.Attachments = new List<Attachment>() { card.ToAttachment() };
                            openingDialogCounter++;
                        }
                        else if(adminName != null && adminName != "" && adminNameCheck == null)
                        {
                            var card = new HeroCard
                            {
                                Text = adminText1 + adminName + adminText2,
                                Buttons = new List<CardAction>
                            {
                                new CardAction(ActionTypes.ImBack, title: "Confirm", value: "confirm"),
                                new CardAction(ActionTypes.ImBack, title: "Decline", value: "decline"),
                            },
                            };
                            reply.Attachments = new List<Attachment>() { card.ToAttachment() };
                            openingDialogCounter++;
                        }
                        else if(adminNameCheck != null)
                        {
                            reply.Text = adminName + adminPromoteCheckText;
                            openingDialogCounter = 0;
                            SettingsStructure.FirstTimeOpened = "false";
                        }                        
                        break;
                    }
                case 2:
                    {
                        if (turnContext.Activity.Text == "delete")
                        {
                            openingDialogCounter = 0;
                        }
                        else if (turnContext.Activity.Text == "confirm")
                        {
                            reply.Text = $"{closingText}";
                            openingDialogCounter = 0;
                            SettingsStructure.FirstTimeOpened = "false";
                            SettingsStructure toJson = new SettingsStructure();
                            toJson._FirstTimeOpened = SettingsStructure.FirstTimeOpened;
                            toJson._AdminName = new List<string>(SettingsStructure.AdminName);
                            toJson._TargetProcessUrl = SettingsStructure.TargetProcessUrl;
                            toJson._ProjectIds = new List<string>(SettingsStructure.ProjectIds);
                            toJson._ProjectNames = new List<string>(SettingsStructure.ProjectNames);
                            string json = JsonConvert.SerializeObject(toJson);
                            System.IO.File.WriteAllText(@"C:\Users\fkaduch\source\repos\TPIntegrationBot\TPIntegrationBot\UserSettings.json", json);                           
                        }
                        else if(turnContext.Activity.Text == "decline")
                        {
                            reply.Text = adminDeclineText;
                            openingDialogCounter = 0;
                            SettingsStructure.FirstTimeOpened = "false";
                        }
                        else
                        {
                            reply.Text = "Please select either confirm or delete option!";
                        }
                        break;
                    }
            }
            return reply;
        }

        // Given the input from the message, create the response.
        private static Activity ProcessInput(ITurnContext turnContext)
        {
            var activity = turnContext.Activity;
            var reply = activity.CreateReply();
            
            if (activity.Attachments != null && activity.Attachments.Any())
            {
                // We know the user is sending an attachment as there is at least one item
                // in the Attachments list.
                HandleIncomingAttachment(activity, reply);
            }
            else if(activity.Text == "skip")
            {
                HandleSkip(activity, reply);
            }
            else
            {
                HandleOutgoingAttachment(activity, reply);
            }

            return reply;
        }

        private static void HandleSkip(IMessageActivity activity, IMessageActivity reply)
        {
            var processedPay = currentPayloads.FirstOrDefault(r => r.Id == activity.From.Name);
            List<int> devIndexes = new List<int>();
            int devIndex = 0;
            int projectIndex = 0;
            foreach (var dev in memberIdList)
            {
                if (processedPay.Developer.FirstOrDefault(r => Int32.Parse(r) == dev) != null)
                {
                    devIndexes.Add(devIndex);
                }
                devIndex++;
            }
            foreach (var pro in SettingsStructure.ProjectIds)
            {
                if (pro == processedPay.Project)
                {
                    break;
                }
                projectIndex++;
            }

            var proName = SettingsStructure.ProjectNames.ElementAt(projectIndex);
            string devName = "";
            List<string> devNames = new List<string>();

            foreach(var ind in devIndexes)
            {
                devName += memberList.ElementAt(ind);
                devName += " ";
            }


            var card = new HeroCard
            {
                Title = $"Do you want to submit this {processedPay.Entity}?",
                Text =
                $"<b>Project:</b> " + proName + " "
                + $"<br></br><b>Title:</b> " + processedPay.Title + " "
                + $"<br></br><b>Description:</b> " + processedPay.Text + " "
                + $"<b>Developers:</b> " + devName,
                Buttons = new List<CardAction>
                    {
                        new CardAction(ActionTypes.ImBack, title: "1. Submit", value: "Submit"),
                        new CardAction(ActionTypes.ImBack, title: "2. Delete", value: "Delete"),
                    },
            };
            reply.Attachments = new List<Attachment>() { card.ToAttachment() };
        }


        private static void HandleOutgoingAttachment(IMessageActivity activity, IMessageActivity reply)
        {
            const string descriptionText = "Add <b>description</b> to your";
            const string titleText = "Add <b>title</b> to your";
            const string pictureText = "Send a <b>picture</b> (or multiple) as an attachement to your";
            var processedPayload = currentPayloads.FirstOrDefault(pay => pay.Id == activity.From.Name);

            if (processedPayload.DialogCounter == 3)
            {
                processedPayload.Title = activity.Text;
                processedPayload.DialogCounter++;
                reply.Text = $"{descriptionText} {processedPayload.Entity}:";
                dialogCounter++;
            }
            else if (processedPayload.DialogCounter == 4)
            {
                processedPayload.Text = activity.Text + $"<br></br>";
                processedPayload.DialogCounter++;
                reply.Text = $"{pictureText} {processedPayload.Entity} or type <b>skip</b> to leave out picture:";
                dialogCounter++;
            }
            else if (processedPayload.DialogCounter == 5)
            {
                if(activity.Text == "Submit")
                {
                    PostMessage(processedPayload);
                    reply.Text = $"Thanks "+ processedPayload.Id +" for reporting "+ processedPayload.Entity;
                    var itemToRemove = currentPayloads.Single(r => r.Id == activity.From.Name);
                    currentPayloads.Remove(itemToRemove);
                }
                else if(activity.Text == "Delete")
                {
                    currentPayloads.RemoveAll(s => s.Id == activity.From.Name);
                    ResetReport(activity);
                    reply.Text = "Report deleted!";
                }
            }
            else if (processedPayload.DialogCounter == 0)
            {
                if (activity.Text.StartsWith("1"))
                {
                    processedPayload.Entity = "Bug";
                    processedPayload.DialogCounter++;
                }
                else if (activity.Text.StartsWith("2"))
                {
                    processedPayload.Entity = "Epic";
                    processedPayload.DialogCounter++;
                }
                else if (activity.Text.StartsWith("3"))
                {
                    processedPayload.Entity = "User Story";
                    processedPayload.DialogCounter++;
                }
                else if(activity.Text.StartsWith("4"))
                {
                    processedPayload.Entity = "Feature";
                    processedPayload.DialogCounter++;
                }
                else if (activity.Text.StartsWith("5"))
                {
                    processedPayload.Entity = "Task";
                    processedPayload.DialogCounter++;
                }
                else
                {
                    // The user did not enter input that this bot was built to handle.
                    reply.Text = "Please select an entity from the suggested type choices";
                }
                if(processedPayload.DialogCounter == 1)
                {

                    List<CardAction> projectButtons = new List<CardAction>();

                    for (int i = 0; i < SettingsStructure.ProjectNames.Count; i++)
                    {
                        projectButtons.Add(new CardAction(ActionTypes.ImBack, title: SettingsStructure.ProjectNames.ElementAt(i), value: SettingsStructure.ProjectIds.ElementAt(i)));
                    }

                    var card = new HeroCard
                    {
                        Title = $"Which project has {processedPayload.Entity}:",
                        Buttons = new List<CardAction>(projectButtons),
                    };
                    reply.Attachments = new List<Attachment>() { card.ToAttachment() };
                }
            }else if(processedPayload.DialogCounter == 2)
            {
                if (activity.Text == "continue")
                {
                    processedPayload.DialogCounter++;
                    reply.Text = $"{titleText} {processedPayload.Entity}:";
                }
                else if(activity.Text == "add")
                {
                    var card = new HeroCard
                    {
                        Title = $"Choose developer for {processedPayload.Entity}:",
                        Buttons = new List<CardAction>(devButtons),
                    };

                    reply.Attachments = new List<Attachment>() { card.ToAttachment() };
                }
                else
                {
                    processedPayload.Developer.Add(activity.Text);
                    if (processedPayload.Entity != "Epic")
                    {
                        var card = new HeroCard
                        {
                            Title = $"Would you like to assign more developers or continue reporting {processedPayload.Entity}?",
                            Buttons = new List<CardAction>
                            {
                                new CardAction(ActionTypes.ImBack, title: "1. Add developer", value: "add"),
                                new CardAction(ActionTypes.ImBack, title: "2. Continue", value: "continue"),
                            },
                        };
                        reply.Attachments = new List<Attachment>() { card.ToAttachment() };
                    }
                    else
                    {
                        processedPayload.DialogCounter++;
                        reply.Text = $"{titleText} {processedPayload.Entity}:";
                    }
                }
                dialogCounter++;
            }
            else if(processedPayload.DialogCounter == 1)
            {
                processedPayload.Project = activity.Text;
                string queryGet = "projects/" + processedPayload.Project + "/ProjectMembers?where=(User.IsActive eq 'true')include=[User]&take=1000";
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(SettingsStructure.TargetProcessUrl + "api/v1/");
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                string authentication = Convert.ToBase64String(Encoding.ASCII.GetBytes("filip.kaduch@instarea.com:magduska89"));
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authentication);
                HttpResponseMessage responseGet = client.GetAsync(queryGet).Result;
                if (responseGet.IsSuccessStatusCode && memberList.Count == 0)
                {
                    var returnedMembers = responseGet.Content.ReadAsStringAsync().Result;
                    GetMembersDictionary(returnedMembers);
                    for (int i = 0; i < memberList.Count; i++)
                    {
                        devButtons.Add(new CardAction(ActionTypes.ImBack, title: memberList.ElementAt(i), value: memberIdList.ElementAt(i)));
                    }
                }

                string queryAuth = "Authentication?login=filip.kaduch@instarea.com";
                HttpResponseMessage responseAuth = client.GetAsync(queryAuth).Result;
                if (responseAuth.IsSuccessStatusCode == true)
                {
                    var returnedAuth = responseAuth.Content.ReadAsStringAsync().Result;
                    //authToken += "?token=";
                    var splitAuth = returnedAuth.Split("<Authentication Token=\"").ElementAt(1);
                    authToken += splitAuth.Split("\"").ElementAt(0);
                    //authToken += "==";
                }

                var card = new HeroCard
                {
                    Title = $"Choose developer for {processedPayload.Entity}:",
                    Buttons = new List<CardAction>(devButtons),
                };
                reply.Attachments = new List<Attachment>() { card.ToAttachment() };

                processedPayload.DialogCounter++;
            }
        }

        private static bool IsDigitsOnly(string str)
        {
            foreach (char c in str)
            {
                if (c < '0' || c > '9')
                    return false;
            }

            return true;
        }
        //Parse project IDs and names from returned xml
        public static void GetProjectsDictionary(string returnedProjects)
        {
            SettingsStructure.ProjectNames.Clear();
            SettingsStructure.ProjectIds.Clear();
            var splitProjects = returnedProjects.Split("<Project ResourceType=\"Project\" Id=\"");
            int indexer = 0;
            
            foreach (var pro in splitProjects)
            {
                if(indexer == 0)
                {
                    indexer++;
                    continue;
                }
                int nameIndex = pro.IndexOf("Name=\"") + "Name=\"".Length;
                string projectName = "";
                int counter = 0;
                string projectId = "";                

                foreach (var c in pro)
                {
                    if (c != '"')
                    {
                        projectId += c;
                    }
                    else
                    {
                        SettingsStructure.ProjectIds.Add(projectId);
                        break;
                    }
                }
                foreach (var c in pro)
                {
                    if (pro[nameIndex + counter] != '"')
                    {
                        projectName += pro[nameIndex + counter];
                        counter++;
                    }
                    else
                    {
                        SettingsStructure.ProjectNames.Add(projectName);
                        break;
                    }
                }
            }
        }

        //Parse project members and separete developers from returned xml
        public static void GetMembersDictionary(string returnedMembers)
        {
            var splitUsers = returnedMembers.Split("</ProjectMember>");

            foreach (var mem in splitUsers)
            {
                string role = "";
                string roleCheck = "";
                int counter = 0;
                bool foundRole = false;
                var foundStartRole = mem.IndexOf("<Role ResourceType=\"Role\" Id=\"") + "<Role ResourceType=\"Role\" Id=\"".Length;
                string memberName = ""; //<Role ResourceType=\"Role\" Id=\"7\" Name=\"Project Manager\" />
                var foundStartLast = mem.IndexOf("<LastName>") + "<LastName>".Length;
                var foundEndLast = mem.IndexOf("</LastName>");
                int spaceLast = foundEndLast - foundStartLast;
                var foundEndFirst = mem.IndexOf("</FirstName>");
                var foundStartFirst = mem.IndexOf("<FirstName>") + "<FirstName>".Length;
                int firstLength = foundEndFirst - foundStartFirst;
                int lastLength = foundEndLast - foundStartLast;
                var foundIdFirst = mem.IndexOf("<User ResourceType=\"User\" Id=\"") + "<User ResourceType=\"User\" Id=\"".Length;
                int uId = 0;
                string userId = "";
                int charcount = 0;
                foreach (var c in mem)
                {
                    if (foundIdFirst + counter <= mem.Length)
                    {
                        if (mem[foundIdFirst + counter] == '\"')
                        {
                            break;
                        }
                        else
                        {
                            userId += mem[foundIdFirst + counter];
                            counter++;
                        }
                    }else
                    {
                        break;
                    }
                }
                counter = 0;

                foreach(var c in mem)
                {
                    charcount++;
                }
                
                foreach (var c in mem)
                {
                    if (counter + foundStartRole <= mem.Length)
                    {
                        if (mem[foundStartRole + counter] == 'N')
                        {
                            foundRole = true;
                        }
                        if (foundRole == true)
                        {
                            if (roleCheck == "Name=\"")
                            {
                                if (mem[foundStartRole + counter] == '\"')
                                {
                                    break;
                                }
                                else
                                    role += mem[foundStartRole + counter];
                            }
                            else
                            {
                                roleCheck += mem[foundStartRole + counter];
                            }
                        }
                        counter++;
                    }else
                    {
                        foundRole = false;
                        break;
                    }
                }


                if (IsDigitsOnly(userId) == true && userId != "")
                {
                    uId = Int32.Parse(userId);
                }
                
                
                if (foundEndFirst != 0 && uId != 0 && role != "Project Manager")
                {
                    memberName += mem.Substring(foundStartFirst, firstLength);
                    memberName += " ";
                    memberName += mem.Substring(foundStartLast, lastLength);
                    if (!nameId.ContainsKey(memberName))
                    {
                        nameId.Add(memberName, uId);
                    }
                    int? memIdCheck = memberIdList.FirstOrDefault(c => c == uId);
                    var memNameCheck = memberList.FirstOrDefault(c => c == memberName);
                    if (memNameCheck == null)
                        memberList.Add(memberName);
                    if (memIdCheck == 0)
                        memberIdList.Add(uId);
                }
            }
        }

        //Post the actual payload to TP api
        public static void PostMessage(Payload payload)
        {
            string payloadJson = JsonConvert.SerializeObject(payload);
            var accessToken = "NTM1OnZRMGo5NWJJSVhya3BhM0VsK0FEeXMzL0dONG51SFFNSkhVV0l3NnJIYWM9";
            
            
            
            string token = "";
            HttpClient client = new HttpClient();           
            client.BaseAddress = new Uri(SettingsStructure.TargetProcessUrl+ "api/v1/");
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            string query = "";

            HttpContent payloadTP = new StringContent("");

            string assignments = "";
            int multi = 0;
            foreach(var name in payload.Developer)
            {
                if(multi > 0)
                {
                    assignments += ",";
                }
                assignments += "{GeneralUser:{Id: "+ name +"},Role:{Id:1}}";
                multi++;
            }

            switch (payload.Entity)
            {
                case "Bug":
                    {
                        query = "Bugs";
                        payloadTP = new StringContent("{Name:'" + payload.Title + "',Description:'" + payload.Text + "',Project:{Id:" + payload.Project + "},Assignments:["+assignments+"]}", Encoding.UTF8, "application/json");
                        break;
                    }
                case "User Story":
                    {
                        query = "userstories";
                        payloadTP = new StringContent("{Name:'" + payload.Title + "',Description:'" + payload.Text + "',Project:{Id:" + payload.Project + "},Assignments:[" + assignments + "]}", Encoding.UTF8, "application/json");
                        break;
                    }
                case "Epic":
                    {
                        query = "epics";
                        payloadTP = new StringContent("{Name:'" + payload.Title + "',Description:'" + payload.Text + "', Project:{Id:" + payload.Project + "},Assignments:[{GeneralUser:{Id:" + payload.Developer + "},Role:{Id:7}}]}", Encoding.UTF8, "application/json");
                        break;
                    }
                case "Feature":
                    {
                        query = "features";
                        payloadTP = new StringContent("{Name:'" + payload.Title + "',Description:'" + payload.Text + "',Project:{Id:" + payload.Project + "},Assignments:[" + assignments + "]}", Encoding.UTF8, "application/json");
                        break;
                    }
                case "Task":
                    {
                        query = "tasks";
                        payloadTP = new StringContent("{Name:'" + payload.Title + "',Project:{Id:" + payload.Project + "}}", Encoding.UTF8, "application/json");
                        break;
                    }
            }
            //query += "?format=json";
            //query += authToken;
            
            string authentication = Convert.ToBase64String(Encoding.ASCII.GetBytes("filip.kaduch@instarea.com:magduska89"));
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authentication);


            string returnedEntity = "";
            string entityId = "";
            int entNum = 0;

            HttpResponseMessage response = client.PostAsync(query, payloadTP).Result;

            if (response.IsSuccessStatusCode)
            {
                returnedEntity = response.Content.ReadAsStringAsync().Result;

                var splitId = returnedEntity.Split("Id\":");
                foreach (char c in splitId[1])
                {
                    if (c == ',')
                        break;
                    else
                    {
                        entityId += c;
                    }
                }
                entNum = Int32.Parse(entityId);
            }
            else
            {
                Console.WriteLine("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
            }

            RestClient restClient = new RestClient(SettingsStructure.TargetProcessUrl);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11;
            //var restBug = new RestRequest("/api/v1/requests" + authToken, Method.POST);
            //restBug.AddHeader("Content-Type", "application/json; charset=utf-8");
            //restBug.AddJsonBody(payloadTP);
            
            //var responseRest = restClient.Execute<Request>(restBug);

            foreach (var localUrl in payload.Category)
            {
                var nameOfFile = localUrl.Split("\\");
                int lengthOfList = nameOfFile.Length;
                var reportImage = nameOfFile.ElementAt((lengthOfList - 1));
                string typeImage = "image/";
                var type = reportImage.Split(".");
                typeImage += type[1];
                var pathToFile = @"" + localUrl;

                var file = new AttachmentFile()
                {
                    FileName = reportImage,
                    ContentType = typeImage,
                    Content = new MemoryStream(File.ReadAllBytes(pathToFile))
                };

                
                UploadAttachment(restClient, token, file, entNum);
            }
            attachementsUrl.Clear();
            hasBegun = false;
        }

        public class Request
        {
            public string Description { get; set; }

            public string Name { get; set; }
            public Project Project { get; set; }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object[])")]
            public override string ToString() =>
                $"{base.ToString()}, {nameof(Description)}: {Description}, {nameof(Project)}: {{{Project}}}";
        }
        public class Project
        {
            public int Id { get; set; }
        }


        private static Attachment UploadAttachment(RestClient restClient, string token, AttachmentFile file, int id)
        {
            restClient.Authenticator = new HttpBasicAuthenticator("filip.kaduch@instarea.com", "magduska89");
            var restRequest = new RestRequest("UploadFile.ashx" + token, Method.POST);
            restRequest.AddHeader("Content-Type", "multipart/form-data");
            restRequest.AddFile("attachment", file.Content.ToArray(), file.FileName, file.ContentType);
            restRequest.AddParameter("generalId", id);
            var response = restClient.Execute<Attachment>(restRequest);
            return response.Data;
        }

        private static void HandleIncomingAttachment(IMessageActivity activity, IMessageActivity reply)
        {
            var processedPay = currentPayloads.FirstOrDefault(r => r.Id == activity.From.Name);
            string url = "";
            processedPay.Category = new List<string>();
            foreach (var file in activity.Attachments)
            {
                // Determine where the file is hosted.
                var remoteFileUrl = ((JObject)file.Content)["downloadUrl"].ToString();
                //var remoteFileUrl = ((JValue)file.ContentUrl).ToString();
                url = remoteFileUrl;
                var checkUrl = file.ContentUrl;
                fileUrl = checkUrl.Replace(" ","%20");
                // Save the attachment to the system temp directory.
                var parsedName = file.Name.Split("fromClient::");

                localDownloadUrl = Path.Combine(Path.GetTempPath(), file.Name);
                
                try
                {
                    var webClient = new WebClient();

                    webClient.DownloadFile(remoteFileUrl, localDownloadUrl);
                }
                catch (Exception ex)
                {
                    var e = ex;
                }

                previewUrl.Add(remoteFileUrl);
                processedPay.Category.Add(localDownloadUrl);
                attachementsUrl.Add(localDownloadUrl);
            }

            string serviceUrl = activity.ServiceUrl;
            var channelData = activity.GetChannelData<modelsAlias::TeamsChannelData>();
            var message = Activity.CreateMessageActivity();
            message.Text = activity.Text;

            List<CardImage> reportImages = new List<CardImage>();
            foreach (var s in previewUrl)
            {
                reportImages.Add(new CardImage(s));
            }

            List<int> devIndexes = new List<int>();
            int devIndex = 0;
            int projectIndex = 0;
            foreach (var dev in memberIdList)
            {
                if (processedPay.Developer.FirstOrDefault(r => Int32.Parse(r) == dev) != null)
                {
                    devIndexes.Add(devIndex);
                }
                devIndex++;
            }

            foreach(var pro in SettingsStructure.ProjectIds)
            {
                if(pro == processedPay.Project)
                {
                    break;
                }
                projectIndex++;
            }

            var proName = SettingsStructure.ProjectNames.ElementAt(projectIndex);
            string devName = "";
            List<string> devNames = new List<string>();

            foreach (var ind in devIndexes)
            {
                devName += memberList.ElementAt(ind);
                devName += " ";
            }


            var card = new HeroCard
            {
                Title = $"Do you want to submit this {processedPay.Entity}?", 
                Text = 
                $"<b>Project:</b> "+proName+" "
                + $"<br></br><b>Title:</b> "+processedPay.Title+" "
                + $"<br></br><b>Description:</b> "+processedPay.Text+" "
                + $"<b>Developer:</b> " + devName,
                Images = reportImages,
                Buttons = new List<CardAction>
                    {
                        new CardAction(ActionTypes.ImBack, title: "1. Submit", value: "Submit"),
                        new CardAction(ActionTypes.ImBack, title: "2. Delete", value: "Delete"),
                    },
            };
            reply.Attachments = new List<Attachment>() { card.ToAttachment() };        
        }

        public class SingleOrArrayConverter<T> : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return (objectType == typeof(List<T>));
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                JToken token = JToken.Load(reader);
                if (token.Type == JTokenType.Array)
                {
                    return token.ToObject<List<T>>();
                }
                return new List<T> { token.ToObject<T>() };
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                List<T> list = (List<T>)value;
                if (list.Count == 1)
                {
                    value = list[0];
                }
                serializer.Serialize(writer, value);
            }

            public override bool CanWrite
            {
                get { return true; }
            }
        }

        public class AttachmentFile
        {
            public string FileName { get; set; }
            public MemoryStream Content { get; set; }
            public string ContentType { get; set; }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object[])")]
            public override string ToString() =>
                $"{nameof(FileName)}: {FileName}, {nameof(ContentType)}: {ContentType}";
        }

    public class Payload
        {
            [JsonProperty("entity")]
            public string Entity { get; set; }

            [JsonProperty("project")]
            public string Project { get; set; }

            [JsonProperty("title")]
            public string Title { get; set; }

            [JsonProperty("username")]
            public string Username { get; set; }

            [JsonProperty("text")]
            public string Text { get; set; }

            [JsonProperty("developer")]
            [JsonConverter(typeof(SingleOrArrayConverter<string>))]
            public List<string> Developer { get; set; }

            [JsonProperty("category")]
            [JsonConverter(typeof(SingleOrArrayConverter<string>))]
            public List<string> Category { get; set; }

            public string Id { get; set; }
            public int DialogCounter { get; set; }
            public bool HasBegun { get => hasBegun; set => hasBegun = value; }

            private bool hasBegun = false;
        }
    }
}

