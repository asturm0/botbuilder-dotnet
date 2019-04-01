﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;

namespace Microsoft.Bot.Builder.Dialogs.Rules.Steps
{
    using CodeStepHandler = Func<DialogContext, object, Task<DialogTurnResult>>;

    public class CodeStep : DialogCommand
    {
        private readonly CodeStepHandler codeHandler;

        public CodeStep(CodeStepHandler codeHandler) : base()
        {
            this.codeHandler = codeHandler ?? throw new ArgumentNullException(nameof(codeHandler));
        }

        protected override async Task<DialogTurnResult> OnRunCommandAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await this.codeHandler(dc, options).ConfigureAwait(false);
        }

        protected override string OnComputeId()
        {
            return $"CodeStep({codeHandler.ToString()})";
        }
    }
}