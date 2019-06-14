﻿// Licensed under the MIT License.
// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Expressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Builder.Dialogs.Adaptive.Steps
{
    /// <summary>
    /// Sets a property with the result of evaluating a value expression
    /// </summary>
    public class SetProperty : DialogCommand
    {
        [JsonConstructor]
        public SetProperty([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0) : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        /// <summary>
        /// Value expression
        /// </summary>
        public Expression Value { get; set; }

        protected override async Task<DialogTurnResult> OnRunCommandAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (options is CancellationToken)
            {
                throw new ArgumentException($"{nameof(options)} cannot be a cancellation token");
            }

            // Ensure planning context
            if (dc is SequenceContext planning)
            {
                // SetProperty evaluates the "Value" expression and returns it as the result of the dialog
                var (value, error) = Value.TryEvaluate(dc.State);

                if (error == null)
                {
                    var sc = dc as SequenceContext;

                    // If this step interrupted a step in the active plan
                    if (sc != null && sc.Steps.Count > 1 && sc.Steps[1].DialogStack.Count > 0)
                    {
                        // Reset the next step's dialog stack so that when the plan continues it reevaluates new changed state
                        sc.Steps[1].DialogStack.Clear();
                    }
                }

                return await planning.EndDialogAsync(value, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            else
            {
                throw new Exception("`SetProperty` should only be used in the context of an adaptive dialog.");
            }
        }

        protected override string OnComputeId()
        {
            return $"SetProperty[${this.Property.ToString() ?? string.Empty}]";
        }
    }
}
