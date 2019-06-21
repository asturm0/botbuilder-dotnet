﻿// Licensed under the MIT License.
// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Builder.Dialogs.Adaptive.Steps
{
    /// <summary>
    /// Step for HttpRequests
    /// </summary>
    public class HttpRequest : DialogCommand
    {
        private static readonly HttpClient client = new HttpClient();

        public enum ResponseTypes
        {
            /// <summary>
            /// No response expected
            /// </summary>
            None,

            /// <summary>
            /// Plain JSON response 
            /// </summary>
            Json,

            /// <summary>
            /// JSON Activity object to send to the user
            /// </summary>
            Activity,

            /// <summary>
            /// Json Array of activity objects to send to the user
            /// </summary>
            Activities
        }

        public enum HttpMethod
        {
            GET,
            POST,
            PATCH
        }

        [JsonConstructor]
        public HttpRequest([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        protected override string OnComputeId()
        {
            return $"HttpRequest[{Method} {Url}]";
        }

        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("method")]
        public HttpMethod Method { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("header")]
        public Dictionary<string, string> Headers { get; set; }

        [JsonProperty("body")]
        public JToken Body { get; set; }

        [JsonProperty("returnType")]
        public ResponseTypes ResponseType { get; set; } = ResponseTypes.Json;

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
                InputBindings["value"] = value;
                OutputBinding = value;
            }
        }

        public HttpRequest(HttpMethod method, string url, string property, Dictionary<string, string> headers = null, JObject body = null, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
        {
            this.RegisterSourceLocation(callerPath, callerLine);
            this.Method = method;
            this.Url = url ?? throw new ArgumentNullException(nameof(url));
            this.Property = property;
            this.Headers = headers;
            this.Body = body;
        }

        private async Task ReplaceJTokenRecursively(DialogContext dc, JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    foreach (var child in token.Children<JProperty>())
                    {
                        await ReplaceJTokenRecursively(dc, child);
                    }
                    break;

                case JTokenType.Array:
                    foreach (var child in token.Children())
                    {
                        await ReplaceJTokenRecursively(dc, child);
                    }
                    break;

                case JTokenType.Property:
                    await ReplaceJTokenRecursively(dc, ((JProperty)token).Value);
                    break;

                default:
                    if (token.Type == JTokenType.String)
                    {
                        token.Replace(await new TextTemplate(token.ToString()).BindToData(dc.Context, dc.State));
                    }
                    break;
            }
        }

        protected override async Task<DialogTurnResult> OnRunCommandAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (options is CancellationToken)
            {
                throw new ArgumentException($"{nameof(options)} cannot be a cancellation token");
            }

            // Single command running with a copy of the original data
            client.DefaultRequestHeaders.Clear();

            JToken instanceBody = null;
            if (this.Body != null)
            {
                instanceBody = (JToken)this.Body.DeepClone();
            }

            var instanceHeaders = Headers == null ? null : new Dictionary<string, string>(Headers);
            var instanceUrl = this.Url;

            instanceUrl = await new TextTemplate(this.Url).BindToData(dc.Context, dc.State).ConfigureAwait(false);

            // Bind each string token to the data in state
            if (instanceBody != null)
            {
                await ReplaceJTokenRecursively(dc, instanceBody);
            }

            // Set header
            if (instanceHeaders != null)
            {
                foreach (var unit in instanceHeaders)
                {
                    client.DefaultRequestHeaders.Add(
                        await new TextTemplate(unit.Key).BindToData(dc.Context, dc.State),
                        await new TextTemplate(unit.Value).BindToData(dc.Context, dc.State));
                }
            }


            HttpResponseMessage response = null;

            if (instanceBody != null && this.Method == HttpMethod.POST)
            {
                response = await client.PostAsync(instanceUrl, new StringContent(instanceBody.ToString(), Encoding.UTF8, "application/json"));
            }

            if (instanceBody != null && this.Method == HttpMethod.PATCH)
            {
                var request = new HttpRequestMessage(new System.Net.Http.HttpMethod("PATCH"), instanceUrl);
                request.Content = new StringContent(instanceBody.ToString(), Encoding.UTF8, "application/json");
                response = await client.SendAsync(request);
            }

            if (this.Method == HttpMethod.GET)
            {
                response = await client.GetAsync(instanceUrl);
            }

            object result = (object)await response.Content.ReadAsStringAsync();

            switch (this.ResponseType)
            {
                case ResponseTypes.Activity:
                    var activity = JsonConvert.DeserializeObject<Activity>((string)result);
                    await dc.Context.SendActivityAsync(activity, cancellationToken: cancellationToken).ConfigureAwait(false);
                    return await dc.EndDialogAsync(cancellationToken: cancellationToken);

                case ResponseTypes.Activities:
                    var activities = JsonConvert.DeserializeObject<Activity[]>((string)result);
                    await dc.Context.SendActivitiesAsync(activities, cancellationToken: cancellationToken).ConfigureAwait(false);
                    return await dc.EndDialogAsync(cancellationToken: cancellationToken);

                case ResponseTypes.Json:
                    // Try set with JOjbect for further retreiving
                    try
                    {
                        result = JToken.Parse((string)result);
                    }
                    catch
                    {
                        result = result.ToString();
                    }
                    return await dc.EndDialogAsync(result, cancellationToken: cancellationToken);

                case ResponseTypes.None:
                default:
                    return await dc.EndDialogAsync(cancellationToken: cancellationToken);

            }
        }
    }
}
