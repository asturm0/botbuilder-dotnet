﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.LanguageGeneration;
using System.IO;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.TestBot.Json
{
    public class TestBotLG : IBot
    {
        private readonly TemplateEngine engine;

        private static string getOsPath(string path) => Path.Combine(path.TrimEnd('\\', '/').Split('\\', '/'));

        private string GetLGResourceFile(string fileName)
        {
            return getOsPath(AppContext.BaseDirectory.Substring(0, AppContext.BaseDirectory.IndexOf("bin")) + "LG\\" + fileName);
        }

        public TestBotLG(TestBotAccessors accessors)
        {
            // load LG file into engine
            engine = new TemplateEngine().AddFile(GetLGResourceFile("8.LG"));
        }

        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                if (turnContext.Activity.Text.ToLower() == "hi")
                {
                    await turnContext.SendActivityAsync(engine.EvaluateTemplate("GreetingTemplate", null));
                }
                else if (turnContext.Activity.Text.ToLower().Contains("marco"))
                {
                    await turnContext.SendActivityAsync(engine.EvaluateTemplate("WordGameReply", new { GameName = "MarcoPolo" }));
                }
                else if (turnContext.Activity.Text.ToLower().Contains("what time is it"))
                {
                    await turnContext.SendActivityAsync(engine.EvaluateTemplate("TimeOfDayExmple", new { timeOfDay = "morning" }));
                }
                else if (turnContext.Activity.Text.ToLower().Contains("multi"))
                {
                    await turnContext.SendActivityAsync(engine.EvaluateTemplate("MultiLineExample", null));
                }
                else if (turnContext.Activity.Text.ToLower().Contains("card"))
                {
                    HeroCard card = JsonConvert.DeserializeObject<HeroCard>(engine.EvaluateTemplate("CardExample", null));
                    var reply = turnContext.Activity.CreateReply();
                    reply.Attachments = new List<Attachment>();
                    reply.Attachments.Add(card.ToAttachment());
                    await turnContext.SendActivityAsync(reply);
                }
                else if (turnContext.Activity.Text.ToLower().Contains("weather"))
                {
                    var temp = new
                    {
                        partOfDay = "morning",
                        isAGoodDay = "true",
                        high = "75",
                        low = "33"
                    };

                    await turnContext.SendActivityAsync(engine.EvaluateTemplate("WeatherForecast", temp));
                }
                else
                {
                    await turnContext.SendActivityAsync(engine.EvaluateTemplate("EchoTemplate", turnContext));
                }
            }
            else if (turnContext.Activity.Type == ActivityTypes.ConversationUpdate)
            {
                if (turnContext.Activity.MembersAdded != null)
                {
                    foreach (var member in turnContext.Activity.MembersAdded)
                    {
                        if (member.Id != turnContext.Activity.Recipient.Id)
                        {
                            await turnContext.SendActivityAsync(engine.EvaluateTemplate("WelcomeTemplate", null));
                        }
                    }
                }
            }
            else
            {
                await turnContext.SendActivityAsync($"{turnContext.Activity.Type} event detected");
            }
        }
    }
}
