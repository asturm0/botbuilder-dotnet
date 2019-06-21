﻿// Licensed under the MIT License.
// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Dialogs.Adaptive.Steps
{
    /// <summary>
    /// Command to end the current dialog, returning the resultProperty as the result of the dialog.
    /// </summary>
    public class EndDialog : DialogCommand
    {
        /// <summary>
        /// Property which is bidirectional property for input and output.  Example: user.age will be passed in, and user.age will be set when the dialog completes
        /// </summary>
        public string ResultProperty
        {
            get
            {
                return OutputBinding;
            }
            set
            {
                InputBindings["value"] = value;
                OutputBinding = value;
            }
        }

        [JsonConstructor]
        public EndDialog(string resultProperty = null, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);

            if (!string.IsNullOrEmpty(resultProperty))
            {
                ResultProperty = resultProperty;
            }
        }

        protected override async Task<DialogTurnResult> OnRunCommandAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (options is CancellationToken)
            {
                throw new ArgumentException($"{nameof(options)} cannot be a cancellation token");
            }

            dc.State.TryGetValue<string>(ResultProperty, out var result);
            return await EndParentDialogAsync(dc, result, cancellationToken).ConfigureAwait(false);
        }

        protected override string OnComputeId()
        {
            return $"end({this.ResultProperty ?? string.Empty})";
        }
    }
}
