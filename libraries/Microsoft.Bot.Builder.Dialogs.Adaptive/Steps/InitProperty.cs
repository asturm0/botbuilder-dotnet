﻿// Licensed under the MIT License.
// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Builder.Dialogs.Adaptive.Steps
{
    /// <summary>
    /// Sets a property with the result of evaluating a value expression
    /// </summary>
    public class InitProperty : DialogCommand
    {
        [JsonConstructor]
        public InitProperty([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0) : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        /// <summary>
        /// Property which is bidirectional property for input and output.  Example: user.age will be passed in, and user.age will be set when the dialog completes
        /// </summary>
        public string Property
        {
            get
            {
                return OutputBinding;
            }
            set
            {
                InputBindings[DialogContextState.DIALOG_VALUE] = value;
                OutputBinding = value;
            }
        }

        /// <summary>
        ///  Type, either Array or Object
        /// </summary>
        public string Type { get; set; }

        protected override async Task<DialogTurnResult> OnRunCommandAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (options is CancellationToken)
            {
                throw new ArgumentException($"{nameof(options)} cannot be a cancellation token");
            }

            var prop = await new TextTemplate(this.Property).BindToData(dc.Context, dc.State).ConfigureAwait(false);


            // Ensure planning context
            if (dc is SequenceContext planning)
            {
                switch (Type.ToLower())
                {
                    case "array":
                        dc.State.SetValue(prop, new JArray());
                        break;
                    case "object":
                        dc.State.SetValue(prop, new JObject());
                        break;
                }

                return await planning.EndDialogAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            else
            {
                throw new Exception("`InitProperty` should only be used in the context of an adaptive dialog.");
            }
        }

        protected override string OnComputeId()
        {
            return $"InitProperty[${this.Property.ToString() ?? string.Empty}]";
        }
    }
}
