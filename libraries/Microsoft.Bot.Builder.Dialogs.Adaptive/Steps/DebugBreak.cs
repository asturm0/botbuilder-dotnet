﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Dialogs.Adaptive.Steps
{
    public class DebugBreak : DialogCommand
    {
        public DebugBreak([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        protected override async Task<DialogTurnResult> OnRunCommandAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            DebugDump(dc);

            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
            return await dc.EndDialogAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        private void DebugDump(DialogContext dc)
        {
            // Best effort
            try
            {
                // Get stepCount from memory
                var stepCount = dc.State.GetValue<int>("turn.stepCount", 0);

                // Compute path
                var path = string.Empty;
                var connector = string.Empty;

                var current = dc.Parent;
                while (current != null)
                {
                    path = current.ActiveDialog?.Id ?? string.Empty + connector + path;
                    connector = "/";
                    current = current.Parent;
                }

                // Get list of steps
                var stepState = dc is SequenceContext sc ? sc.Steps : new List<StepState>();
                var stepsIds = stepState.Select(s => s.DialogId);

                Debug.WriteLine($"{path}: {stepCount} steps executed and {stepsIds.Count()} remaining.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to collect full debug dump. Error: {ex.ToString()}");
            }
        }
    }
}
