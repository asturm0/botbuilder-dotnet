﻿// Licensed under the MIT License.
// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Rules;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Selectors;
using Microsoft.Bot.Builder.Dialogs.Debugging;
using Microsoft.Bot.Builder.Expressions;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using static Microsoft.Bot.Builder.Dialogs.Debugging.DebugSupport;

namespace Microsoft.Bot.Builder.Dialogs.Adaptive
{

    /// <summary>
    /// The Adaptive Dialog models conversation using events and rules to adapt dynamicaly to changing conversation flow
    /// </summary>
    public class AdaptiveDialog : DialogContainer
    {
        private const string ADAPTIVE_KEY = "adaptiveDialogState";

        private readonly string changeKey = Guid.NewGuid().ToString();
        
        private bool installedDependencies = false;

        public IStatePropertyAccessor<BotState> BotState { get; set; }

        public IStatePropertyAccessor<Dictionary<string, object>> UserState { get; set; }

        /// <summary>
        /// Recognizer for processing incoming user input 
        /// </summary>
        public IRecognizer Recognizer { get; set; }

        /// <summary>
        /// Language Generator override
        /// </summary>
        public ILanguageGenerator Generator { get; set; }

        /// <summary>
        /// Gets or sets the steps to execute when the dialog begins
        /// </summary>
        public List<IDialog> Steps { get; set; } = new List<IDialog>();

        /// <summary>
        /// Rules for handling events to dynamic modifying the executing plan 
        /// </summary>
        public virtual List<IRule> Rules { get; set; } = new List<IRule>();

        /// <summary>
        /// Gets or sets the policty to Automatically end the dialog when there are no steps to execute
        /// </summary>
        /// <remarks>
        /// If true, when there are no steps to execute the current dialog will end
        /// If false, when there are no steps to execute the current dialog will simply end the turn and still be active
        /// </remarks>
        public bool AutoEndDialog { get; set; } = true;

        /// <summary>
        /// Gets or sets the selector for picking the possible rules to execute.
        /// </summary>
        public IRuleSelector Selector { get; set; }

        /// <summary>
        /// Gets or sets the property to return as the result when the dialog ends when there are no more Steps and AutoEndDialog = true.
        /// </summary>
        public string DefaultResultProperty { get; set; } = "dialog.result";

        public override IBotTelemetryClient TelemetryClient
        {
            get
            {
                return base.TelemetryClient;
            }
            set
            {
                var client = value ?? new NullBotTelemetryClient();
                _dialogs.TelemetryClient = client;
                base.TelemetryClient = client;
            }
        }

        public AdaptiveDialog(string dialogId = null, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base(dialogId)
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (options is CancellationToken)
            {
                throw new ArgumentException($"{nameof(options)} should not ever be a cancellation token");
            }

            EnsureDependenciesInstalled();

            var activeDialogState = dc.ActiveDialog.State as Dictionary<string, object>;
            activeDialogState[ADAPTIVE_KEY] = new AdaptiveDialogState();
            var state = activeDialogState[ADAPTIVE_KEY] as AdaptiveDialogState;

            // Persist options to dialog state
            state.Options = options ?? new Dictionary<string, object>();

            // Initialize 'result' with any initial value
            if (state.Options.GetType() == typeof(Dictionary<string, object>) && (state.Options as Dictionary<string, object>).ContainsKey("value"))
            {
                state.Result = state.Options["value"];
            }

            // Evaluate rules and queue up step changes
            var dialogEvent = new DialogEvent()
            {
                Name = AdaptiveEvents.BeginDialog,
                Value = options,
                Bubble = false
            };

            await this.OnDialogEventAsync(dc, dialogEvent, cancellationToken).ConfigureAwait(false);

            // Continue step execution
            return await this.ContinueStepsAsync(dc, cancellationToken).ConfigureAwait(false);
        }

        public override async Task<DialogTurnResult> ContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            EnsureDependenciesInstalled();

            // Continue step execution
            return await ContinueStepsAsync(dc, cancellationToken).ConfigureAwait(false);
        }

        protected override async Task<bool> OnPreBubbleEvent(DialogContext dc, DialogEvent dialogEvent, CancellationToken cancellationToken = default(CancellationToken))
        {
            var sequenceContext = this.ToSequenceContext(dc);

            // Process event and queue up any potential interruptions
            return await this.ProcessEventAsync(sequenceContext, dialogEvent, preBubble: true, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        protected override async Task<bool> OnPostBubbleEvent(DialogContext dc, DialogEvent dialogEvent, CancellationToken cancellationToken = default(CancellationToken))
        {
            var sequenceContext = this.ToSequenceContext(dc);

            // Process event and queue up any potential interruptions
            return await this.ProcessEventAsync(sequenceContext, dialogEvent, preBubble: false, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        protected async Task<bool> ProcessEventAsync(SequenceContext sequenceContext, DialogEvent dialogEvent, bool preBubble, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Save into turn
            sequenceContext.State.SetValue(DialogContextState.TURN_DIALOGEVENT, dialogEvent);

            // Look for triggered rule
            var handled = await this.QueueFirstMatchAsync(sequenceContext, dialogEvent, preBubble, cancellationToken).ConfigureAwait(false);

            if (handled)
            {
                return true;
            }

            // Default processing
            if (preBubble)
            {
                switch (dialogEvent.Name)
                {
                    case AdaptiveEvents.BeginDialog:
                        if (this.Steps.Any())
                        {
                            // Initialize plan with steps
                            var changes = new StepChangeList()
                            {
                                ChangeType = StepChangeTypes.InsertSteps,
                                Steps = new List<StepState>()
                            };

                            this.Steps.ForEach(
                                s => changes.Steps.Add(
                                    new StepState()
                                    {
                                        DialogId = s.Id,
                                        DialogStack = new List<DialogInstance>()
                                    }));

                            sequenceContext.QueueChanges(changes);
                            handled = true;
                        }
                        else
                        {
                            // Emit leading ActivityReceived event
                            var e = new DialogEvent() { Name = AdaptiveEvents.ActivityReceived, Value = sequenceContext.Context.Activity, Bubble = false };
                            handled = await this.ProcessEventAsync(sequenceContext, dialogEvent: e, preBubble: true, cancellationToken: cancellationToken).ConfigureAwait(false);
                        }

                        break;

                    case AdaptiveEvents.ActivityReceived:

                        var activity = sequenceContext.Context.Activity;

                        if (activity.Type == ActivityTypes.Message)
                        {
                            // Recognize utterance
                            var recognized = await this.OnRecognize(sequenceContext, cancellationToken).ConfigureAwait(false);

                            sequenceContext.State.SetValue(DialogContextState.TURN_RECOGNIZED, recognized);

                            var (name, score) = recognized.GetTopScoringIntent();
                            sequenceContext.State.SetValue(DialogContextState.TURN_TOPINTENT, name);
                            sequenceContext.State.SetValue(DialogContextState.TURN_TOPSCORE, score);

                            if (this.Recognizer != null)
                            {
                                await sequenceContext.DebuggerStepAsync(Recognizer, AdaptiveEvents.RecognizedIntent, cancellationToken).ConfigureAwait(false);
                            }

                            // Emit leading RecognizedIntent event
                            var e = new DialogEvent() { Name = AdaptiveEvents.RecognizedIntent, Value = recognized, Bubble = false };
                            handled = await this.ProcessEventAsync(sequenceContext, dialogEvent: e, preBubble: true, cancellationToken: cancellationToken).ConfigureAwait(false);
                        }
                        break;
                }
            }
            else
            {
                switch (dialogEvent.Name)
                {
                    case AdaptiveEvents.BeginDialog:
                        var e = new DialogEvent() { Name = AdaptiveEvents.ActivityReceived, Value = sequenceContext.Context.Activity, Bubble = false };
                        handled = await this.ProcessEventAsync(sequenceContext, dialogEvent: e, preBubble: false, cancellationToken: cancellationToken).ConfigureAwait(false);

                        break;

                    case AdaptiveEvents.ActivityReceived:

                        var activity = sequenceContext.Context.Activity;

                        if (activity.Type == ActivityTypes.Message)
                        {
                            // Empty sequence?
                            if (!sequenceContext.Steps.Any())
                            {
                                // Emit trailing unknownIntent event
                                e = new DialogEvent() { Name = AdaptiveEvents.UnknownIntent, Bubble = false };
                                handled = await this.ProcessEventAsync(sequenceContext, dialogEvent: e, preBubble: false, cancellationToken: cancellationToken).ConfigureAwait(false);
                            }
                            else
                            {
                                handled = false;
                            }
                        }
                        break;
                }
            }

            return handled;
        }

        public override async Task<DialogTurnResult> ResumeDialogAsync(DialogContext dc, DialogReason reason, object result = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (result is CancellationToken)
            {
                throw new ArgumentException($"{nameof(result)} cannot be a cancellation token");
            }

            // Containers are typically leaf nodes on the stack but the dev is free to push other dialogs
            // on top of the stack which will result in the container receiving an unexpected call to
            // resumeDialog() when the pushed on dialog ends.
            // To avoid the container prematurely ending we need to implement this method and simply
            // ask our inner dialog stack to re-prompt.
            await RepromptDialogAsync(dc.Context, dc.ActiveDialog).ConfigureAwait(false);

            return Dialog.EndOfTurn;
        }

        public override async Task RepromptDialogAsync(ITurnContext turnContext, DialogInstance instance, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Forward to current sequence step
            var state = (instance.State as Dictionary<string, object>)[ADAPTIVE_KEY] as AdaptiveDialogState;

            if (state.Steps.Any())
            {
                // We need to mockup a DialogContext so that we can call RepromptDialog
                // for the active step
                var stepDc = new DialogContext(_dialogs, turnContext, state.Steps[0], new Dictionary<string, object>(), new Dictionary<string, object>());
                await stepDc.RepromptDialogAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public void AddRule(IRule rule)
        {
            rule.Steps.ForEach(s => _dialogs.Add(s));
            this.Rules.Add(rule);
        }

        public void AddRules(IEnumerable<IRule> rules)
        {
            foreach (var rule in rules)
            {
                this.AddRule(rule);
            }
        }

        public void AddDialog(IEnumerable<IDialog> dialogs)
        {
            foreach (var dialog in dialogs)
            {
                this._dialogs.Add(dialog);
            }
        }

        protected override string OnComputeId()
        {
            if (DebugSupport.SourceRegistry.TryGetValue(this, out var range))
            {
                return $"AdaptiveDialog({Path.GetFileName(range.Path)}:{range.Start.LineIndex})";
            }
            return $"AdaptiveDialog[{this.BindingPath()}]";
        }

        public override DialogContext CreateChildContext(DialogContext dc)
        {
            var activeDialogState = dc.ActiveDialog.State as Dictionary<string, object>;
            var state = activeDialogState[ADAPTIVE_KEY] as AdaptiveDialogState;

            if (state == null)
            {
                state = new AdaptiveDialogState();
                activeDialogState[ADAPTIVE_KEY] = state;
            }

            if (state.Steps != null && state.Steps.Any())
            {
                var ctx = new SequenceContext(this._dialogs, dc, state.Steps.First(), state.Steps, changeKey);
                ctx.Parent = dc;
                return ctx;
            }

            return null;
        }

        private string GetUniqueInstanceId(DialogContext dc)
        {
            return dc.Stack.Count > 0 ? $"{dc.Stack.Count}:{dc.ActiveDialog.Id}" : string.Empty;
        }

        protected async Task<DialogTurnResult> ContinueStepsAsync(DialogContext dc, CancellationToken cancellationToken)
        {
            // Apply any queued up changes
            var sequenceContext = this.ToSequenceContext(dc);
            await sequenceContext.ApplyChangesAsync(cancellationToken).ConfigureAwait(false);

            if (this.Generator != null)
            {
                dc.Context.TurnState.Set<ILanguageGenerator>(this.Generator);
            }

            // Get a unique instance ID for the current stack entry.
            // We need to do this because things like cancellation can cause us to be removed
            // from the stack and we want to detect this so we can stop processing steps.
            var instanceId = this.GetUniqueInstanceId(sequenceContext);

            var step = this.CreateChildContext(sequenceContext) as SequenceContext;

            if (step != null)
            {
                // Continue current step
                var result = await step.ContinueDialogAsync(cancellationToken).ConfigureAwait(false);

                // Start step if not continued
                if (result.Status == DialogTurnStatus.Empty && GetUniqueInstanceId(sequenceContext) == instanceId)
                {
                    var nextStep = step.Steps.First();
                    result = await step.BeginDialogAsync(nextStep.DialogId, nextStep.Options, cancellationToken).ConfigureAwait(false);
                }

                // Increment turns step count
                // This helps dialogs being resumed from an interruption to determine if they
                // should re-prompt or not.
                var stepCount = sequenceContext.State.GetValue<int>(DialogContextState.TURN_STEPCOUNT, 0);
                sequenceContext.State.SetValue(DialogContextState.TURN_STEPCOUNT, stepCount + 1);

                // Is the step waiting for input or were we cancelled?
                if (result.Status == DialogTurnStatus.Waiting || this.GetUniqueInstanceId(sequenceContext) != instanceId)
                {
                    return result;
                }

                // End current step
                await this.EndCurrentStepAsync(sequenceContext, cancellationToken).ConfigureAwait(false);

                // Execute next step
                // We call continueDialog() on the root dialog to ensure any changes queued up
                // by the previous steps are applied.
                DialogContext root = sequenceContext;
                while (root.Parent != null)
                {
                    root = root.Parent;
                }

                return await root.ContinueDialogAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await this.OnEndOfStepsAsync(sequenceContext, cancellationToken).ConfigureAwait(false);
            }
        }

        protected async Task<bool> EndCurrentStepAsync(SequenceContext sequenceContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (sequenceContext.Steps.Any())
            {
                sequenceContext.Steps.RemoveAt(0);

                if (!sequenceContext.Steps.Any())
                {
                    await sequenceContext.EmitEventAsync(AdaptiveEvents.SequenceEnded, value: null, bubble: false, fromLeaf: false, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }

            return false;
        }

        protected async Task<DialogTurnResult> OnEndOfStepsAsync(SequenceContext sequenceContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            // End dialog and return result
            if (sequenceContext.ActiveDialog != null)
            {
                if (this.ShouldEnd(sequenceContext))
                {
                    sequenceContext.State.TryGetValue<object>(DefaultResultProperty, out var result);
                    return await sequenceContext.EndDialogAsync(result, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    return Dialog.EndOfTurn;
                }
            }
            else
            {
                return new DialogTurnResult(DialogTurnStatus.Cancelled);
            }
        }

        protected async Task<RecognizerResult> OnRecognize(SequenceContext sequenceContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            var context = sequenceContext.Context;
            if (Recognizer != null)
            {
                var result = await Recognizer.RecognizeAsync(context, cancellationToken).ConfigureAwait(false);
                // only allow one intent 
                var topIntent = result.GetTopScoringIntent();
                result.Intents.Clear();
                result.Intents.Add(topIntent.intent, new IntentScore() { Score = topIntent.score });
                return result;
            }
            else
            {
                return new RecognizerResult()
                {
                    Text = context.Activity.Text ?? string.Empty,
                    Intents = new Dictionary<string, IntentScore>()
                    {
                        { "None", new IntentScore() { Score = 0.0} }
                    },
                    Entities = JObject.Parse("{}")
                };

            }
        }

        private async Task<bool> QueueFirstMatchAsync(SequenceContext sequenceContext, DialogEvent dialogEvent, bool preBubble, CancellationToken cancellationToken)
        {
            var selection = await Selector.Select(sequenceContext, cancellationToken).ConfigureAwait(false);
            if (selection.Any())
            {
                var rule = Rules[selection.First()];
                await sequenceContext.DebuggerStepAsync(rule, dialogEvent, cancellationToken).ConfigureAwait(false);
                System.Diagnostics.Trace.TraceInformation($"Executing Dialog: {this.Id} Rule[{selection}]: {rule.GetType().Name}: {rule.GetExpression(null)}");
                var changes = await rule.ExecuteAsync(sequenceContext).ConfigureAwait(false);

                if (changes != null && changes.Count > 0)
                {
                    sequenceContext.QueueChanges(changes[0]);
                    return true;
                }
            }
            return false;
        }

        private void EnsureDependenciesInstalled()
        {
            lock (this)
            {
                if (!installedDependencies)
                {
                    installedDependencies = true;

                    AddDialog(this.Steps.ToArray());

                    foreach (var rule in this.Rules)
                    {
                        AddDialog(rule.Steps.ToArray());
                    }

                    // Wire up selector
                    if (this.Selector == null)
                    {
                        // Default to most specific then first
                        this.Selector = new MostSpecificSelector
                        {
                            Selector = new FirstSelector()
                        };
                    }
                    this.Selector.Initialize(this.Rules, true);
                }
            }
        }

        private bool ShouldEnd(DialogContext dc)
        {
            return this.AutoEndDialog;
        }

        private SequenceContext ToSequenceContext(DialogContext dc)
        {
            var activeDialogState = dc.ActiveDialog.State as Dictionary<string, object>;
            var state = activeDialogState[ADAPTIVE_KEY] as AdaptiveDialogState;

            if (state == null)
            {
                state = new AdaptiveDialogState();
                activeDialogState[ADAPTIVE_KEY] = state;
            }

            if (state.Steps == null)
            {
                state.Steps = new List<StepState>();
            }

            var sequenceContext = new SequenceContext(dc.Dialogs, dc, new DialogState() { DialogStack = dc.Stack }, state.Steps, changeKey);
            sequenceContext.Parent = dc.Parent;
            return sequenceContext;
        }
    }
}
