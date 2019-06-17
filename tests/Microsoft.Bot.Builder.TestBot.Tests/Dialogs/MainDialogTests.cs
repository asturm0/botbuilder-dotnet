﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Testing;
using Microsoft.Bot.Builder.Testing.XUnit;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.BotBuilderSamples.CognitiveModels;
using Microsoft.BotBuilderSamples.Services;
using Microsoft.BotBuilderSamples.Tests.Framework;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.BotBuilderSamples.Tests.Dialogs
{
    public class MainDialogTests : DialogTestsBase
    {
        private readonly BookingDialog _mockBookingDialog;
        private readonly Mock<IRecognizer> _mockLuisRecognizer;

        public MainDialogTests(ITestOutputHelper output)
            : base(output)
        {
            _mockLuisRecognizer = new Mock<IRecognizer>();

            var mockFlightBookingService = new Mock<IFlightBookingService>();
            mockFlightBookingService.Setup(x => x.BookFlight(It.IsAny<BookingDetails>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(true));
            _mockBookingDialog = DialogUtils.CreateMockDialog<BookingDialog>(null, new Mock<GetBookingDetailsDialog>().Object, mockFlightBookingService.Object).Object;
        }

        [Fact]
        public void DialogConstructor()
        {
            // TODO: check with the team if there's value in these types of test or if there's a better way of asserting the
            // dialog got composed properly.
            var sut = new MainDialog(MockLogger.Object, _mockLuisRecognizer.Object, _mockBookingDialog);

            Assert.Equal("MainDialog", sut.Id);
            Assert.IsType<TextPrompt>(sut.FindDialog("TextPrompt"));
            Assert.NotNull(sut.FindDialog("BookingDialog"));
            Assert.IsType<WaterfallDialog>(sut.FindDialog("WaterfallDialog"));
        }

        [Fact]
        public async Task ShowsMessageIfLuisNotConfigured()
        {
            // Arrange
            var sut = new MainDialog(MockLogger.Object, null, _mockBookingDialog);
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: new[] { new XUnitOutputMiddleware(Output) });

            // Act/Assert
            var reply = await testClient.SendActivityAsync<IMessageActivity>("hi");
            Assert.Equal("NOTE: LUIS is not configured. To enable all capabilities, add 'LuisAppId', 'LuisAPIKey' and 'LuisAPIHostName' to the appsettings.json file.", reply.Text);

            reply = testClient.GetNextReply<IMessageActivity>();
            Assert.Equal("What can I help you with today?", reply.Text);
        }

        [Fact]
        public async Task ShowsPromptIfLuisIsConfigured()
        {
            // Arrange
            var sut = new MainDialog(MockLogger.Object, _mockLuisRecognizer.Object, _mockBookingDialog);
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: new[] { new XUnitOutputMiddleware(Output) });

            // Act/Assert
            var reply = await testClient.SendActivityAsync<IMessageActivity>("hi");
            Assert.Equal("What can I help you with today?", reply.Text);
        }

        [Theory]
        [InlineData("I want to book a flight", "BookFlight", "BookingDialog mock invoked")]
        [InlineData("What's the weather like?", "GetWeather", "TODO: get weather flow here")]
        [InlineData("bananas", "None", "Sorry, I didn't get that. Please try asking in a different way (intent was None)")]
        public async Task TaskSelector(string utterance, string intent, string invokedDialogResponse)
        {
            _mockLuisRecognizer
                .Setup(x => x.RecognizeAsync<FlightBooking>(It.IsAny<ITurnContext>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new FlightBooking
                {
                    Intents = new Dictionary<FlightBooking.Intent, IntentScore>
                    {
                        { Enum.Parse<FlightBooking.Intent>(intent), new IntentScore() { Score = 1 } },
                    },
                    Entities = new FlightBooking._Entities(),
                }));

            var sut = new MainDialog(MockLogger.Object, _mockLuisRecognizer.Object, _mockBookingDialog);
            var testClient = new DialogTestClient(Channels.Test, sut, middlewares: new[] { new XUnitOutputMiddleware(Output) });

            var reply = await testClient.SendActivityAsync<IMessageActivity>("hi");
            Assert.Equal("What can I help you with today?", reply.Text);

            reply = await testClient.SendActivityAsync<IMessageActivity>(utterance);
            Assert.Equal(invokedDialogResponse, reply.Text);

            reply = testClient.GetNextReply<IMessageActivity>();
            Assert.Equal("What else can I do for you?", reply.Text);
        }
    }
}
