﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Rules;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Steps;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Builder.Dialogs.Adaptive.Tests
{
    [TestClass]
    public class SettingsState_Tests
    {
        public TestContext TestContext { get; set; }

        public IConfiguration Configuration;

        public SettingsState_Tests()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            this.Configuration = builder.Build();
        }

        [TestMethod]
        public async Task DialogContextState_SettingsTest()
        {
            await CreateFlow("en-us")
                .Send("howdy")
                    .AssertReply("00000000-0000-0000-0000-000000000000")
                .Send("howdy")
                    .AssertReply("00000000-0000-0000-0000-000000000000")
                .Send("howdy")
                    .AssertReply("00000000-0000-0000-0000-000000000000")
                .StartTestAsync();
        }

        private TestFlow CreateFlow(string locale)
        {
            var convoState = new ConversationState(new MemoryStorage());
            var userState = new UserState(new MemoryStorage());
            var planningDialog = new AdaptiveDialog();
            planningDialog.AddRules(new List<IRule>()
            {
                new UnknownIntentRule(steps:
                    new List<IDialog>()
                    {
                        new SendActivity()
                        {
                            Activity = new ActivityTemplate("{settings.ApplicationInsights.InstrumentationKey}")
                        },
                    }),
            });

            var resourceExplorer = new ResourceExplorer();

            var adapter = new TestAdapter(TestAdapter.CreateConversation(TestContext.TestName))
                .Use(new RegisterClassMiddleware<ResourceExplorer>(resourceExplorer))
                .Use(new RegisterClassMiddleware<IStorage>(new MemoryStorage()))
                .UseLanguageGeneration(resourceExplorer)
                .Use(new RegisterClassMiddleware<IConfiguration>(this.Configuration))
                .Use(new AutoSaveStateMiddleware(convoState, userState))
                .Use(new TranscriptLoggerMiddleware(new FileTranscriptLogger()));

            // adapter.Locale = locale;

            var userStateProperty = userState.CreateProperty<Dictionary<string, object>>("user");
            var convoStateProperty = convoState.CreateProperty<Dictionary<string, object>>("conversation");

            var dialogState = convoState.CreateProperty<DialogState>("dialogState");
            var dialogs = new DialogSet(dialogState);


            return new TestFlow((TestAdapter)adapter, async (turnContext, cancellationToken) =>
            {
                await planningDialog.OnTurnAsync(turnContext, null).ConfigureAwait(false);
            });
        }

    }
}
