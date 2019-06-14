﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Steps;
using Microsoft.Bot.Builder.Dialogs.Debugging;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Builder.Dialogs.Declarative.Types;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Builder.AI.LanguageGeneration.Tests
{
    public class MockLanguageGenator : ILanguageGenerator
    {
        public Task<string> Generate(ITurnContext turnContext, string template, object data)
        {
            return Task.FromResult(template);
        }
    }

    [TestClass]
    public class LGGeneratorTests
    {
        public TestContext TestContext { get; set; }

        private static ResourceExplorer resourceExplorer;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            TypeFactory.Configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            TypeFactory.RegisterAdaptiveTypes();
            resourceExplorer = ResourceExplorer.LoadProject(GetProjectFolder());
        }
        private static string GetProjectFolder()
        {
            return AppContext.BaseDirectory.Substring(0, AppContext.BaseDirectory.IndexOf("bin"));
        }


        [ClassCleanup]
        public static void ClassCleanup()
        {
            resourceExplorer.Dispose();
        }

        private ITurnContext GetTurnContext(string locale, ILanguageGenerator generator = null)
        {
            var context = new TurnContext(new TestAdapter()
                .UseResourceExplorer(resourceExplorer)
                .UseLanguageGeneration(resourceExplorer, generator ?? new MockLanguageGenator()), new Activity() { Locale = locale, Text = "" });
            context.TurnState.Add(new LanguageGeneratorManager(resourceExplorer));
            if (generator != null)
            {
                context.TurnState.Add<ILanguageGenerator>(generator);
            }
            return context;
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public async Task TestNotFoundTemplate()
        {
            var context = GetTurnContext("");
            var lg = new TemplateEngineLanguageGenerator("", name: "test");
            await lg.Generate(context, "[tesdfdfsst]", null);
        }

        [TestMethod]
        public async Task TestMultiLanguageGenerator()
        {
            var lg = new MultiLanguageGenerator();
            lg.LanguageGenerators[""] = new TemplateEngineLanguageGenerator(resourceExplorer.GetResource("test.lg").ReadText(), name: "test.lg");
            lg.LanguageGenerators["de"] = new TemplateEngineLanguageGenerator(resourceExplorer.GetResource("test.de.lg").ReadText(), name: "test.de.lg");
            lg.LanguageGenerators["en"] = new TemplateEngineLanguageGenerator(resourceExplorer.GetResource("test.en.lg").ReadText(), name: "test.en.lg");
            lg.LanguageGenerators["en-US"] = new TemplateEngineLanguageGenerator(resourceExplorer.GetResource("test.en-US.lg").ReadText(), name: "test.en-US.lg");
            lg.LanguageGenerators["en-GB"] = new TemplateEngineLanguageGenerator(resourceExplorer.GetResource("test.en-GB.lg").ReadText(), name: "test.en-GB.lg");
            lg.LanguageGenerators["fr"] = new TemplateEngineLanguageGenerator(resourceExplorer.GetResource("test.fr.lg").ReadText(), name: "test.fr.lg");

            // test targeted in each language
            Assert.AreEqual("english-us", await lg.Generate(GetTurnContext(locale: "en-us"), "[test]", null));
            Assert.AreEqual("english-gb", await lg.Generate(GetTurnContext(locale: "en-gb"), "[test]", null));
            Assert.AreEqual("english", await lg.Generate(GetTurnContext(locale: "en"), "[test]", null));
            Assert.AreEqual("default", await lg.Generate(GetTurnContext(locale: ""), "[test]", null));
            Assert.AreEqual("default", await lg.Generate(GetTurnContext(locale: "foo"), "[test]", null));

            // test fallback for en-us -> en -> default
            Assert.AreEqual("default2", await lg.Generate(GetTurnContext(locale: "en-us"), "[test2]", null));
            Assert.AreEqual("default2", await lg.Generate(GetTurnContext(locale: "en-gb"), "[test2]", null));
            Assert.AreEqual("default2", await lg.Generate(GetTurnContext(locale: "en"), "[test2]", null));
            Assert.AreEqual("default2", await lg.Generate(GetTurnContext(locale: ""), "[test2]", null));
            Assert.AreEqual("default2", await lg.Generate(GetTurnContext(locale: "foo"), "[test2]", null));
        }

        [TestMethod]
        public async Task TestResourceMultiLanguageGenerator()
        {
            var lg = new ResourceMultiLanguageGenerator("test.lg");

            // test targeted in each language
            Assert.AreEqual("english-us", await lg.Generate(GetTurnContext("en-us", lg), "[test]", null));
            Assert.AreEqual("english-gb", await lg.Generate(GetTurnContext("en-gb", lg), "[test]", null));
            Assert.AreEqual("english", await lg.Generate(GetTurnContext("en", lg), "[test]", null));
            Assert.AreEqual("default", await lg.Generate(GetTurnContext("", lg), "[test]", null));
            Assert.AreEqual("default", await lg.Generate(GetTurnContext("foo", lg), "[test]", null));

            // test fallback for en-us -> en -> default
            Assert.AreEqual("default2", await lg.Generate(GetTurnContext("en-us", lg), "[test2]", null));
            Assert.AreEqual("default2", await lg.Generate(GetTurnContext("en-gb", lg), "[test2]", null));
            Assert.AreEqual("default2", await lg.Generate(GetTurnContext("en", lg), "[test2]", null));
            Assert.AreEqual("default2", await lg.Generate(GetTurnContext("", lg), "[test2]", null));
            Assert.AreEqual("default2", await lg.Generate(GetTurnContext("foo", lg), "[test2]", null));
        }

        [TestMethod]
        public async Task TestLanguageGeneratorMiddleware()
        {
            await CreateFlow("en-us", async (turnContext, cancellationToken) =>
            {
                var lg = turnContext.TurnState.Get<ILanguageGenerator>();
                Assert.IsNotNull(lg, "ILanguageGenerator should not be null");
                Assert.IsNotNull(turnContext.TurnState.Get<ResourceExplorer>(), "ResourceExplorer should not be null");
                var text = await lg.Generate(turnContext, "[test]", null);
                Assert.AreEqual("english-us", text, "template should be there");
            })
            .Send("hello")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task TestDialogInjection()
        {
            var dialog = new AdaptiveDialog()
            {
                Generator = new ResourceMultiLanguageGenerator("subDialog.lg"),
                Steps = new List<IDialog>()
                {
                    new SendActivity("[test]")
                }
            };

            await CreateFlow("en-us", async (turnContext, cancellationToken) =>
            {
                await dialog.OnTurnAsync(turnContext, null).ConfigureAwait(false);

            })
            .Send("hello")
                .AssertReply("overriden")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task TestDialogInjectionDeclarative()
        {
            await CreateFlow("en-us", async (turnContext, cancellationToken) =>
            {
                var resource = resourceExplorer.GetResource("test.dialog");
                var dialog = (AdaptiveDialog)DeclarativeTypeLoader.Load<IDialog>(resource, resourceExplorer, DebugSupport.SourceRegistry);

                await dialog.OnTurnAsync(turnContext, null).ConfigureAwait(false);
            })
            .Send("hello")
                .AssertReply("root")
                .AssertReply("overriden")
            .StartTestAsync();
        }

        private TestFlow CreateFlow(string locale, BotCallbackHandler handler)
        {
            TypeFactory.Configuration = new ConfigurationBuilder().Build();
            var storage = new MemoryStorage();
            var convoState = new ConversationState(storage);
            var userState = new UserState(storage);

            var adapter = new TestAdapter(TestAdapter.CreateConversation(TestContext.TestName));
            adapter
                .UseStorage(storage)
                .UseState(userState, convoState)
                .UseResourceExplorer(resourceExplorer)
                .UseLanguageGeneration(resourceExplorer, "test.lg")
                .Use(new TranscriptLoggerMiddleware(new FileTranscriptLogger()));

            return new TestFlow(adapter, handler);
        }
    }
}
