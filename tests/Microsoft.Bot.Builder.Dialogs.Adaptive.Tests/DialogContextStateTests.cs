﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Recognizers;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Rules;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Steps;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Builder.Dialogs.Declarative.Types;
using Microsoft.Bot.Builder.Expressions;
using Microsoft.Bot.Builder.Expressions.Parser;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Builder.Dialogs.Adaptive.Tests
{
    public class Bar
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public bool cool { get; set; }
    }

    public class Foo
    {
        public string Name { get; set; }

        public int Age { get; set; }

        public bool cool { get; set; }

        public Bar SubName { get; set; }
    }

    [TestClass]
    public class DialogContextStateTests
    {
        public TestContext TestContext { get; set; }

        private Foo foo = new Foo()
        {
            Name = "Tom",
            Age = 15,
            cool = true,
            SubName = new Bar()
            {
                Name = "bob",
                Age = 122,
                cool = false
            }
        };

        [TestMethod]
        public async Task DialogContextState_SimpleValues()
        {
            var dialog = new AdaptiveDialog("test");
            var dialogs = new DialogSet();
            dialogs.Add(dialog);
            var dc = new DialogContext(dialogs, new TurnContext(new TestAdapter(), new Schema.Activity()), (DialogState)new DialogState());
            await dc.BeginDialogAsync(dialog.Id);
            DialogContextState state = new DialogContextState(dc: dc,
                settings: new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase),
                userState: new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase),
                conversationState: new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase),
                turnState: new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase));


            // simple value types
            state.SetValue("UseR.nuM", 15);
            state.SetValue("uSeR.NuM", 25);
            Assert.IsTrue(state.HasValue("user.num"), "should have the value");
            Assert.AreEqual(25, state.GetValue<int>("user.num"));

            state.SetValue("UsEr.StR", "string1");
            state.SetValue("usER.STr", "string2");
            Assert.IsTrue(state.HasValue("user.str"), "should have the value");
            Assert.AreEqual("string2", state.GetValue<string>("USer.str"));

            // simple value types
            state.SetValue("ConVErsation.nuM", 15);
            state.SetValue("ConVErSation.NuM", 25);
            Assert.IsTrue(state.HasValue("conversation.num"), "should have the value");
            Assert.AreEqual(25, state.GetValue<int>("conversation.num"));

            state.SetValue("ConVErsation.StR", "string1");
            state.SetValue("CoNVerSation.STr", "string2");
            Assert.IsTrue(state.HasValue("conversation.str"), "should have the value");
            Assert.AreEqual("string2", state.GetValue<string>("conversation.str"));

            // simple value types
            state.SetValue("tUrn.nuM", 15);
            state.SetValue("turN.NuM", 25);
            Assert.IsTrue(state.HasValue("turn.num"), "should have the value");
            Assert.AreEqual(25, state.GetValue<int>("turn.num"));

            state.SetValue("tuRn.StR", "string1");
            state.SetValue("TuRn.STr", "string2");
            Assert.IsTrue(state.HasValue("turn.str"), "should have the value");
            Assert.AreEqual("string2", state.GetValue<string>("turn.str"));
        }

        [TestMethod]
        public async Task DialogContextState_ComplexValuePaths()
        {
            var dialog = new AdaptiveDialog("test");
            var dialogs = new DialogSet();
            dialogs.Add(dialog);
            var dc = new DialogContext(dialogs, new TurnContext(new TestAdapter(), new Schema.Activity()), (DialogState)new DialogState());
            await dc.BeginDialogAsync(dialog.Id);
            DialogContextState state = new DialogContextState(dc: dc,
                settings: new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase),
                userState: new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase),
                conversationState: new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase),
                turnState: new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase));


            // complex type paths
            state.SetValue("UseR.fOo", foo);
            Assert.IsTrue(state.HasValue("user.foo.SuBname.name"), "should have the value");
            state.TryGetValue<string>("user.foo.SuBname.name", out var val);
            Assert.AreEqual("bob", val);

            // complex type paths
            state.SetValue("ConVerSation.FOo", foo);
            Assert.IsTrue(state.HasValue("conversation.foo.SuBname.name"), "should have the value");
            state.TryGetValue<string>("conversation.foo.SuBname.name", out val);
            Assert.AreEqual("bob", val);

            // complex type paths
            state.SetValue("TurN.fOo", foo);
            Assert.IsTrue(state.HasValue("TuRN.foo.SuBname.name"), "should have the value");
            state.TryGetValue<string>("TuRN.foo.SuBname.name", out val);
            Assert.AreEqual("bob", val);
        }

        [TestMethod]
        public async Task DialogContextState_ComplexTypes()
        {
            var dialog = new AdaptiveDialog("test");
            var dialogs = new DialogSet();
            dialogs.Add(dialog);
            var dc = new DialogContext(dialogs, new TurnContext(new TestAdapter(), new Schema.Activity()), (DialogState)new DialogState());
            await dc.BeginDialogAsync(dialog.Id);
            DialogContextState state = new DialogContextState(dc: dc,
                settings: new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase),
                userState: new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase),
                conversationState: new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase),
                turnState: new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase));


            // complex type paths
            state.SetValue("UseR.fOo", foo);
            Assert.IsTrue(state.HasValue("user.foo"), "should have the value");
            Assert.AreEqual(state.GetValue<Foo>("user.foo").SubName.Name, "bob");

            // complex type paths
            state.SetValue("ConVerSation.FOo", foo);
            Assert.IsTrue(state.HasValue("conversation.foo"), "should have the value");
            Assert.AreEqual(state.GetValue<Foo>("conversation.foo").SubName.Name, "bob");

            // complex type paths
            state.SetValue("TurN.fOo", foo);
            Assert.IsTrue(state.HasValue("turn.foo"), "should have the value");
            Assert.AreEqual(state.GetValue<Foo>("turn.foo").SubName.Name, "bob");
        }

        private TestFlow CreateFlow(AdaptiveDialog dialog, ConversationState convoState = null, UserState userState = null, bool sendTrace = false)
        {
            TypeFactory.Configuration = new ConfigurationBuilder().Build();
            var resourceExplorer = new ResourceExplorer();

            var adapter = new TestAdapter(TestAdapter.CreateConversation(TestContext.TestName), sendTrace)
                .Use(new RegisterClassMiddleware<ResourceExplorer>(resourceExplorer))
                .UseLanguageGeneration(resourceExplorer)
                .Use(new RegisterClassMiddleware<IStorage>(new MemoryStorage()))
                .Use(new AutoSaveStateMiddleware(userState ?? new UserState(new MemoryStorage()), convoState ?? new ConversationState(new MemoryStorage())))
                .Use(new TranscriptLoggerMiddleware(new FileTranscriptLogger()));

            var dm = new DialogManager(dialog);

            return new TestFlow((TestAdapter)adapter, async (turnContext, cancellationToken) =>
            {
                await dm.OnTurnAsync(turnContext, cancellationToken: cancellationToken).ConfigureAwait(false);
            });
        }

        [TestMethod]
        public async Task DialogContextState_TurnStateMappings()
        {
            var testDialog = new AdaptiveDialog("testDialog")
            {
                AutoEndDialog = false,
                Recognizer = new RegexRecognizer()
                {
                    Intents = new Dictionary<string, string>()
                    {
                        { "IntentNumber1", "intent1" },
                        { "NameIntent", ".*name is (?<name>.*)" }
                    }
                },
                Steps = new List<IDialog>()
                {
                    new SendActivity("{turn.activity.text}"),
                },
                Rules = new List<IRule>()
                {
                    new IntentRule(intent: "IntentNumber1",
                        steps:new List<IDialog>()
                        {
                            new SendActivity("{turn.activity.text}"),
                            new SendActivity("{turn.recognized.intent}"),
                            new SendActivity("{turn.recognized.score}"),
                            new SendActivity("{turn.recognized.text}"),
                            new SendActivity("{turn.recognized.intents.intentnumber1.score}"),
                        }),
                    new IntentRule(intent: "NameIntent",
                        steps:new List<IDialog>()
                        {
                            new SendActivity("{turn.recognized.entities.name}"),
                        }),
                }
            };

            await CreateFlow(testDialog)
                .Send("hi")
                    .AssertReply("hi")
                .Send("intent1")
                    .AssertReply("intent1")
                    .AssertReply("IntentNumber1")
                    .AssertReply("1")
                    .AssertReply("intent1")
                    .AssertReply("1")
                .Send("my name is joe")
                    .AssertReply("joe")
                .StartTestAsync();
        }

        [TestMethod]
        public async Task DialogContextState_DialogCommandScope()
        {
            var testDialog = new AdaptiveDialog("testDialog")
            {
                AutoEndDialog = false,
                Steps = new List<IDialog>()
                {
                    new SetProperty()
                    {
                        Property = "dialog.name",
                        Value = "'testDialog'"
                    },
                    new SendActivity("{dialog.name}"),
                    new IfCondition()
                    {
                        Condition= "{dialog.name} == 'testDialog'",
                        Steps = new List<IDialog>()
                        {
                            new SendActivity("nested dialogCommand {dialog.name}")
                        }
                    }
                }
            };

            await CreateFlow(testDialog)
                    .SendConversationUpdate()
                        .AssertReply("testDialog")
                        .AssertReply("nested dialogCommand testDialog")
                    .StartTestAsync();
        }

        [TestMethod]
        public async Task DialogContextState_InputBinding()
        {
            var testDialog = new AdaptiveDialog("testDialog")
            {
                AutoEndDialog = false,
                Steps = new List<IDialog>()
                {
                    new SetProperty() { Property = "dialog.name", Value = "'testDialog'" },
                    new SendActivity("{dialog.name}"),
                    new AdaptiveDialog("d1")
                    {
                        InputBindings = new Dictionary<string, string>() { { "$address.name", "dialog.name" } },
                        Steps = new List<IDialog>()
                        {
                            new SendActivity("nested dialogCommand {$address.name}")
                        }
                    },
                    new BeginDialog()
                    {
                        // bind dialog.name -> adaptive dialog
                        Dialog = new AdaptiveDialog("d2")
                        {
                            InputBindings = new Dictionary<string, string>() { { "$address.name", "dialog.name" } },
                            Steps = new List<IDialog>()
                            {
                                new SendActivity("nested begindialog {$address.name}")
                            }
                        }
                    },
                }
            };

            await CreateFlow(testDialog)
                    .SendConversationUpdate()
                        .AssertReply("testDialog")
                        .AssertReply("nested dialogCommand testDialog")
                        .AssertReply("nested begindialog testDialog")
                    .StartTestAsync();
        }

        [TestMethod]
        public async Task DialogContextState_OutputBinding()
        {
            var testDialog = new AdaptiveDialog("testDialog")
            {
                AutoEndDialog = false,
                Steps = new List<IDialog>()
                {
                    new SetProperty() { Property = "dialog.name", Value = "'testDialog'" },
                    new SendActivity("{dialog.name}"),
                    new AdaptiveDialog("d1")
                    {
                        InputBindings = new Dictionary<string, string>() { { "$xxx", "dialog.name" } },
                        OutputBinding = "dialog.name",
                        DefaultResultProperty = "$xxx",
                        Steps = new List<IDialog>()
                        {
                            new SendActivity("nested dialogCommand {$xxx}"),
                            new SetProperty() { Property = "dialog.xxx", Value = "'newName'" },
                            new SendActivity("nested dialogCommand {$xxx}"),
                        }
                    },
                    new SendActivity("{dialog.name}"),
                    new BeginDialog()
                    {
                        Dialog = new AdaptiveDialog("d2")
                        {
                            InputBindings = new Dictionary<string, string>() { { "$zzz", "dialog.name" } },
                            DefaultResultProperty = "$zzz",
                            // test output binding from adaptive dialog
                            OutputBinding = "dialog.name",
                            Steps = new List<IDialog>()
                            {
                                new SendActivity("nested begindialog {$zzz}"),
                                new SetProperty() { Property = "dialog.zzz", Value = "'newName2'" },
                                new SendActivity("nested begindialog {$zzz}"),
                            }
                        }
                    },
                    new SendActivity("{dialog.name}"),
                    new BeginDialog()
                    {
                        // test output binding from beginDialog
                        OutputBinding = "dialog.name",
                        Dialog = new AdaptiveDialog("d3")
                        {
                            InputBindings = new Dictionary<string, string>() { { "$qqq", "dialog.name" } },
                            DefaultResultProperty = "$qqq",
                            Steps = new List<IDialog>()
                            {
                                new SendActivity("nested begindialog2 {$qqq}"),
                                new SetProperty() { Property = "dialog.qqq", Value = "'newName3'" },
                                new SendActivity("nested begindialog2 {$qqq}"),
                            }
                        }
                    },
                    new SendActivity("{dialog.name}"),
                }
            };

            await CreateFlow(testDialog)
                    .SendConversationUpdate()
                        .AssertReply("testDialog")
                        .AssertReply("nested dialogCommand testDialog")
                        .AssertReply("nested dialogCommand newName")
                        .AssertReply("newName")
                        .AssertReply("nested begindialog newName")
                        .AssertReply("nested begindialog newName2")
                        .AssertReply("newName2")
                        .AssertReply("nested begindialog2 newName2")
                        .AssertReply("nested begindialog2 newName3")
                        .AssertReply("newName3")
                    .StartTestAsync();
        }

        [TestMethod]
        public async Task DialogContextState_CallstackScope()
        {
            var testDialog = new AdaptiveDialog("testDialog")
            {
                AutoEndDialog = false,
                Steps = new List<IDialog>()
                {
                    new SetProperty() { Property = "$xxx", Value = "'xxx'" },
                    new SetProperty() { Property = "$aaa", Value = "'d1'" },
                    new SendActivity("{$aaa}"),
                    new BeginDialog()
                    {
                        Dialog = new AdaptiveDialog("d2")
                        {
                            Steps = new List<IDialog>()
                            {
                                new SetProperty() { Property = "$yyy", Value = "'yyy'" },
                                new SendActivity("{$aaa == null}"),
                                new SendActivity("{^aaa}"),
                                new BeginDialog()
                                {
                                    Dialog = new AdaptiveDialog("d3")
                                    {
                                        Steps = new List<IDialog>()
                                        {
                                            new SetProperty() { Property = "$zzz", Value = "'zzz'" },
                                            new SetProperty() { Property = "$aaa", Value = "'d3'" },
                                            new SendActivity("{$aaa}"),
                                            new SendActivity("{^aaa}"),
                                            new SendActivity("{$zzz}"),
                                            new SendActivity("{$yyy==null}"),
                                            new SendActivity("{$xxx==null}"),
                                            new SendActivity("{^yyy}"),
                                            new SendActivity("{^xxx}"),
                                        }
                                    }
                                },
                            }
                        }
                    },
                }
            };

            await CreateFlow(testDialog)
                    .SendConversationUpdate()
                        // d1
                        .AssertReply("d1")
                        // d2
                        .AssertReply("True")
                        .AssertReply("d1")
                        // d3
                        .AssertReply("d3")
                        .AssertReply("d3")
                        .AssertReply("zzz")
                        .AssertReply("True")
                        .AssertReply("True")
                        .AssertReply("yyy")
                        .AssertReply("xxx")
                    .StartTestAsync();
        }

    }
}
