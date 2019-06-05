﻿using System;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Dialogs
{
    /// <summary>
    /// Defines how to generate an IActivity based on all of the parameters which drive resolution.
    /// </summary>
    /// <typeparam name="T">type of IActivity to return </typeparam>
    public interface IActivityGenerator<T>
        where T : IActivity
    {
        /// <summary>
        /// Generate a IActivity based on paramters
        /// </summary>
        /// <param name="turnContext">turn context</param>
        /// <param name="template">template or [templateId]</param>
        /// <param name="data">data to bind to.</param>
        /// <returns></returns>
        Task<T> Generate(ITurnContext turnContext, string template, object data);
    }
}
