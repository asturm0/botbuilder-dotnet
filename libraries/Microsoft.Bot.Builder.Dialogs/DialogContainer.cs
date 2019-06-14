﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs.Debugging;

namespace Microsoft.Bot.Builder.Dialogs
{
    public abstract class DialogContainer : Dialog
    {
        protected readonly DialogSet _dialogs = new DialogSet();

        public DialogContainer(string dialogId = null)
            : base(dialogId)
        {
        }

        public abstract DialogContext CreateChildContext(DialogContext dc);

        public virtual Dialog AddDialog(IDialog dialog)
        {
            this._dialogs.Add(dialog);
            return this;
        }

        public IDialog FindDialog(string dialogId)
        {
            return this._dialogs.Find(dialogId);
        }
    }
}
