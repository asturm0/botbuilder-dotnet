﻿// Licensed under the MIT License.
// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Expressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Builder.Dialogs.Adaptive.Steps
{
    /// <summary>
    /// Lets you modify an array in memory
    /// </summary>
    public class EditArray : DialogCommand
    {
        public enum ArrayChangeType
        {
            /// <summary>
            /// Push item onto the end of the array
            /// </summary>
            Push,

            /// <summary>
            /// Pop the item off the end of the array
            /// </summary>
            Pop,

            /// <summary>
            /// Take an item from the front of the array
            /// </summary>
            Take,

            /// <summary>
            /// Remove the item from the array, regardless of it's location
            /// </summary>
            Remove,

            /// <summary>
            /// Clear the contents of the array
            /// </summary>
            Clear
        }

        [JsonConstructor]
        public EditArray([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        protected override string OnComputeId()
        {
            return $"array[{ChangeType + ": " + ArrayProperty}]";
        }

        /// <summary>
        /// type of change being applied
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("changeType")]
        public ArrayChangeType ChangeType { get; set; }

        /// <summary>
        /// Memory expression of the array to manipulate
        /// </summary>Edit
        [JsonProperty("arrayProperty")]
        public string ArrayProperty { get; set; }

        /// <summary>
        /// The result of the action
        /// </summary>
        [JsonProperty("resultProperty")]
        public string ResultProperty { get; set; }

        /// <summary>
        /// The expression of the item to put onto the array
        /// </summary>
        [JsonProperty("Value")]
        public Expression Value { get; set; }

        public EditArray(ArrayChangeType changeType, string arrayProperty = null, Expression value = null, string resultProperty = null)
            : base()
        {

            this.ChangeType = changeType;

            if (!string.IsNullOrEmpty(arrayProperty))
            {
                this.ArrayProperty = arrayProperty;
            }

           switch (changeType)
           {
                case ArrayChangeType.Clear:
                case ArrayChangeType.Pop:
                case ArrayChangeType.Take:
                    this.ResultProperty = resultProperty;
                    break;
                case ArrayChangeType.Push:
                case ArrayChangeType.Remove:
                    this.Value = value;
                    break;
           }           
        }

        protected override async Task<DialogTurnResult> OnRunCommandAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (options is CancellationToken)
            {
                throw new ArgumentException($"{nameof(options)} cannot be a cancellation token");
            }

            if (string.IsNullOrEmpty(ArrayProperty))
            {
                throw new Exception($"EditArray: \"{ ChangeType }\" operation couldn't be performed because the arrayProperty wasn't specified.");
            }

            var prop = await new TextTemplate(this.ArrayProperty).BindToData(dc.Context, dc.State).ConfigureAwait(false);
            var array = dc.State.GetValue(prop, new JArray());

            object item = null;
            object result = null;

            switch (ChangeType)
            {
                case ArrayChangeType.Pop:
                    item = array[array.Count - 1];
                    array.RemoveAt(array.Count - 1);
                    result = item;
                    break;
                case ArrayChangeType.Push:
                    EnsureValue();
                    var (itemResult, error) = this.Value.TryEvaluate(dc.State);
                    if (error == null && itemResult != null)
                    {
                        array.Add(itemResult);
                    }
                    break;
                case ArrayChangeType.Take:
                    if (array.Count == 0)
                    {
                        break;
                    }
                    item = array[0];
                    array.RemoveAt(0);                   
                    result = item;
                    break;
                case ArrayChangeType.Remove:
                    EnsureValue();
                    (itemResult, error) = this.Value.TryEvaluate(dc.State);
                    if (error == null && itemResult != null)
                    {
                        result = false;
                        if (array.Values<string>().Contains<object>(itemResult))
                        {
                            result = true;
                            array.Where(x => x.Value<string>() == itemResult.ToString()).First().Remove();
                        }                        
                    }
                    break;
                case ArrayChangeType.Clear:
                    result = array.Count > 0;
                    array.Clear();
                    break;
            }

            dc.State.SetValue(prop, array);
            return await dc.EndDialogAsync(result);
        }

        private void EnsureValue()
        {
            if (Value == null)
            {
                throw new Exception($"EditArray: \"{ ChangeType }\" operation couldn't be performed for array \"{ArrayProperty}\" because a value wasn't specified.");
            }
        }

    }
}
