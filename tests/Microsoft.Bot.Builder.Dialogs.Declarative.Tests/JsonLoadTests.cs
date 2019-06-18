﻿// Licensed under the MIT License.
// Copyright (c) Microsoft Corporation. All rights reserved.

using System.IO;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Bot.Builder.Dialogs.Declarative.Types;
using Microsoft.Bot.Builder.Dialogs.Declarative.Tests.Recognizers;
using Microsoft.Bot.Schema;
using System.Collections.Generic;
using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Bot.Builder.Dialogs.Debugging;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Builder.LanguageGeneration;

namespace Microsoft.Bot.Builder.Dialogs.Loader.Tests
{
    [TestClass]
    public class JsonLoadTests
    {
        private static string getOsPath(string path) => Path.Combine(path.TrimEnd('\\').Split('\\'));
        
        private readonly string samplesDirectory = getOsPath(@"..\..\..\..\..\samples\Microsoft.Bot.Builder.TestBot.Json\Samples\");

        private static ResourceExplorer resourceExplorer;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            TypeFactory.Configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            TypeFactory.RegisterAdaptiveTypes();
            TypeFactory.Register("Microsoft.RuleRecognizer", typeof(RuleRecognizer));
            string projPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, getOsPath($@"..\..\..\..\..\samples\Microsoft.Bot.Builder.TestBot.Json\Microsoft.Bot.Builder.TestBot.Json.csproj")));
            resourceExplorer = ResourceExplorer.LoadProject(projPath);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            resourceExplorer.Dispose();
        }

        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task JsonDialogLoad_Steps()
        {
            await BuildTestFlow(@"Steps.main.dialog")
                .SendConversationUpdate()
                .AssertReply("Step 1")
                .AssertReply("Step 2")
                .AssertReply("Step 3")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task JsonDialogLoad_EndTurn()
        {
            await BuildTestFlow("EndTurn.main.dialog")
            .Send("hello")
                .AssertReply("What's up?")
            .Send("Nothing")
                .AssertReply("Oh I see!")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task JsonDialogLoad_IfProperty()
        {
            await BuildTestFlow("IfCondition.main.dialog")
            .SendConversationUpdate()
            .AssertReply("Hello, I'm Zoidberg. What is your name?")
            .Send("Carlos")
            .AssertReply("Hello Carlos, nice to talk to you!")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task JsonDialogLoad_TextInputWithoutProperty()
        {
            await BuildTestFlow("TextInput.WithoutProperty.main.dialog")
            .SendConversationUpdate()
                .AssertReply("Hello, I'm Zoidberg. What is your name?")
            .Send("Carlos")
                .AssertReply("Hello, nice to talk to you!")
                .StartTestAsync();
        }

        [TestMethod]
        public async Task JsonDialogLoad_TextInput()
        {
            await BuildTestFlow("TextInput.main.dialog")
            .SendConversationUpdate()
                .AssertReply("Hello, I'm Zoidberg. What is your name?")
            .Send("Carlos")
                .AssertReply("Hello Carlos, nice to talk to you!")
            .Send("hi")
                .AssertReply("Hello Carlos, nice to talk to you!")
                .StartTestAsync();
        }

        [TestMethod]
        public async Task JsonDialogLoad_NumberInput()
        {
            await BuildTestFlow("NumberInput.main.dialog")
            .SendConversationUpdate()
                .AssertReply("What is your age?")
            .Send("Blablabla")
                .AssertReply("Please input a number.")
            .Send("4")
                .AssertReply("Hello, your age is 4!")
                .AssertReply("2 * 2.2 equals?")
            .Send("4.4")
                .AssertReply("2 * 2.2 equals 4.4, that's right!")
                .StartTestAsync();
        }

        [TestMethod]
        public async Task JsonDialogLoad_RepeatDialog()
        {
            await BuildTestFlow("RepeatDialog.main.dialog")
            .SendConversationUpdate()
                .AssertReply("RepeatDialog.main.dialog starting")
                .AssertReply("Hello, what is your name?")
            .Send("Carlos")
                .AssertReply("Hello Carlos, nice to meet you! (type cancel to end this)")
            .Send("hi")
                .AssertReply("RepeatDialog.main.dialog starting")
                .AssertReply("Hello Carlos, nice to meet you! (type cancel to end this)")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task JsonDialogLoad_TraceAndLog()
        {
            await BuildTestFlow("TraceAndLog.main.dialog", sendTrace: true)
            .SendConversationUpdate()
                .AssertReply("Hello, what is your name?")
            .Send("Carlos")
                .AssertReply(activity =>
                {
                    var trace = (Activity)activity;
                    Assert.AreEqual(ActivityTypes.Trace, trace.Type, "should be trace activity");
                    Assert.AreEqual("memory", trace.ValueType, "value type should be memory");
                })
            .StartTestAsync();
        }

        [TestMethod]
        public async Task JsonDialogLoad_DoSteps()
        {
            await BuildTestFlow("DoSteps.main.dialog")
            .Send(new Activity(ActivityTypes.ConversationUpdate, membersAdded: new List<ChannelAccount>() { new ChannelAccount("bot", "Bot") }))
            .SendConversationUpdate()
                .AssertReply("Hello, I'm Zoidberg. What is your name?")
            .Send("Carlos")
                .AssertReply("Hello Carlos, nice to talk to you!")
                .AssertReply("Hey, I can tell you a joke, or tell your fortune")
            .Send("Do you know a joke?")
                .AssertReply("Why did the chicken cross the road?")
            .Send("Why?")
                .AssertReply("To get to the other side")
            .Send("What happened in the future?")
                .AssertReply("Seeing into the future...")
                .AssertReply("I see great things happening...")
                .AssertReply("Perhaps even a successful bot demo")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task JsonDialogLoad_BeginDialog()
        {
            await BuildTestFlow("BeginDialog.main.dialog")
            .Send(new Activity(ActivityTypes.ConversationUpdate, 
                membersAdded: new List<ChannelAccount>() { new ChannelAccount("bot", "Bot")}))
            .SendConversationUpdate()
                .AssertReply("Hello, I'm Zoidberg. What is your name?")
            .Send("Carlos")
                .AssertReply("Hello Carlos, nice to talk to you!")
                .AssertReply("Hey, I can tell you a joke, or tell your fortune")
            .Send("Do you know a joke?")
                .AssertReply("Why did the chicken cross the road?")
            .Send("Why?")
                .AssertReply("To get to the other side")
            .Send("What happened in the future?")
                .AssertReply("Seeing into the future...")
                .AssertReply("I see great things in your future...")
                .AssertReply("Potentially a successful demo")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task JsonDialogLoad_ChoiceInputDialog()
        {
            await BuildTestFlow("ChoiceInput.main.dialog")
            .SendConversationUpdate()
                .AssertReply("Please select a value from below:\n\n   1. Test1\n   2. Test2\n   3. Test3")
            .Send("Test1")
                .AssertReply("You select: Test1")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task JsonDialogLoad_ExternalLanguage()
        {
            await BuildTestFlow("ExternalLanguage.main.dialog")
            .SendConversationUpdate()
                .AssertReplyOneOf(new string[]
                {
                    "Zoidberg here, welcome to my world!",
                    "Hello, my name is Zoidberg and I'll be your guide.",
                    "Hail Zoidberg!"
                })
                .AssertReplyOneOf(new string[]
                {
                    "Hello. What is your name?",
                    "I would like to know you better, what's your name?"
                })
            .Send("Carlos")
                .AssertReplyOneOf(new string[]
                {
                    "Hello Carlos, nice to talk to you!",
                    "Hi Carlos, you seem nice!",
                    "Whassup Carlos?"
                })
            .Send("Help")
                .AssertReply("I can tell jokes and also forsee the future!\n")
            .Send("Do you know a joke?")
                .AssertReply("Why did the chicken cross the road?")
            .Send("Why?")
                .AssertReply("To get to the other side")
            .Send("What happened in the future?")
                .AssertReply("I see great things in your future...")
                .AssertReply("Potentially a successful demo")
            .StartTestAsync();
        }

        [TestMethod]
        //[Ignore("Temporarily disabling to unblock event model PR. This is a super important test, reenabling shortly.")]
        public async Task JsonDialogLoad_ToDoBot()
        {
            await BuildTestFlow("ToDoBot.main.dialog")
            .Send(new Activity(ActivityTypes.ConversationUpdate, membersAdded: new List<ChannelAccount>() { new ChannelAccount("bot", "Bot") }))
            .SendConversationUpdate()
                .AssertReply("Hi! I'm a ToDo bot. Say \"add a todo named first\" to get started.")
            .Send("add a todo named first")
                .AssertReply("Successfully added a todo named \"first\"")
            .Send("add a todo named second")
                .AssertReply("Successfully added a todo named \"second\"")
            .Send("add a todo")
                .AssertReply("OK, please enter the title of your todo.")
            .Send("third")
                .AssertReply("Successfully added a todo named \"third\"")
            .Send("show todos")
                .AssertReply("Your most recent 3 tasks are\n* first\n* second\n* third\n")
            .Send("delete todo named second")
                .AssertReply("Successfully removed a todo named \"second\"")
            .Send("show todos")
                .AssertReply("Your most recent 2 tasks are\n* first\n* third\n")
            .Send("add a todo")
                .AssertReply("OK, please enter the title of your todo.")
            .Send("cancel")
                .AssertReply("ok.")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task JsonDialogLoad_HttpRequest()
        {
            await BuildTestFlow("HttpRequest.main.dialog")
            .Send(new Activity(ActivityTypes.ConversationUpdate, membersAdded: new List<ChannelAccount>() { new ChannelAccount("bot", "Bot") }))
            .AssertReply("Welcome! Here is a http request sample, please enter a name for you visual pet.")
            .Send("TestPetName")
            .AssertReply("Great! Your pet's name is TestPetName")
            .AssertReply("Now please enter the id of your pet, this could help you find your pet later.")
            .Send("12121")
            .AssertReply("Done! You have added a pet named \"TestPetName\" with id \"12121\"")
            .AssertReply("Now try to specify the id of your pet, and I will help your find it out from the store.")
            .Send("12121")
            .AssertReply("Great! I found your pet named \"TestPetName\"")
            .StartTestAsync();
        }

        private TestFlow BuildTestFlow(string resourceName, bool sendTrace = false)
        {
            TypeFactory.Configuration = new ConfigurationBuilder().Build();
            var storage = new MemoryStorage();
            var convoState = new ConversationState(storage);
            var userState = new UserState(storage);
            var adapter = new TestAdapter(TestAdapter.CreateConversation(TestContext.TestName), sendTrace);
            adapter
                .UseStorage(storage)
                .UseState(userState, convoState)
                .UseResourceExplorer(resourceExplorer)
                .UseLanguageGeneration(resourceExplorer)
                .Use(new TranscriptLoggerMiddleware(new FileTranscriptLogger()));

            var resource = resourceExplorer.GetResource(resourceName);
            var dialog = DeclarativeTypeLoader.Load<IDialog>(resource, resourceExplorer, DebugSupport.SourceRegistry);

            return new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                if (dialog is AdaptiveDialog planningDialog)
                {
                    await planningDialog.OnTurnAsync(turnContext, null, cancellationToken).ConfigureAwait(false);
                }
            });
        }
    }
}
