using FunctionAppNotification.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace FunctionAppNotification
{
    public static class Notifier
    {
        #region Orchestrator Function
        [FunctionName("Notifier")]
        public static async Task<List<string>> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            Notification inputData = GetInputData(context);
            List<string> outputs = await SendNotifications(context, inputData);
            return outputs;
        }
        #endregion

        #region Activity Functions
        [FunctionName("Notifier_SendMail")]
        public static async Task<string> SendMailAsync([ActivityTrigger] Notification notification, ILogger log)
        {
            log.LogInformation($"Send mail {notification.Mail} -- {notification.Message}.");
            try
            {
                var client = new SendGridClient("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");
                var msg = new SendGridMessage()
                {
                    From = new EmailAddress("alveiro09@hotmail.com", "alveiro09@hotmail.com"),
                    Subject = "Hello World from the SendGrid CSharp SDK!",
                    PlainTextContent = "Hello, Email!",
                    HtmlContent = "<strong>Hello, Email!</strong>"
                };
                msg.AddTo(new EmailAddress(notification.Mail));
                var response = await client.SendEmailAsync(msg);
                return $"Result sending email to {notification.Mail} -- {response.StatusCode}";
            }
            catch (Exception exc)
            {
                return $"Error sending email to {notification.Mail} -- {exc.Message}";
            }
        }

        [FunctionName("Notifier_SendSMS")]
        public static string SendSMS([ActivityTrigger] Notification notification, ILogger log)
        {
            log.LogInformation($"Send sms {notification.PhoneNumber} -- {notification.Message}.");

            try
            {
                TwilioClient.Init("AC130d56c04f4c88440102756dca02e477", "a77b81d44fbe2f3811adde8e3b65c9b1");

                var messageSent = MessageResource.Create(
                    from: new Twilio.Types.PhoneNumber("+573163258533"),
                    body: notification.Message,
                    to: new Twilio.Types.PhoneNumber(notification.PhoneNumber)
                );
                return $"Result sending sms to {notification.PhoneNumber} -- {messageSent.Sid}";
            }
            catch (Exception exc)
            {
                return $"Error sending sms to {notification.PhoneNumber} -- {exc.Message}";
            }
        }

        #endregion

        [FunctionName("Notifier_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("Notifier", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        #region Auxiliar Methods
        private static Notification GetInputData(IDurableOrchestrationContext context)
        {
            return context.GetInput<Notification>();
        }

        private static async Task<List<string>> SendNotifications(IDurableOrchestrationContext context, Notification inputData)
        {
            List<string> outputs = new List<string>();
            if (inputData != null && !string.IsNullOrEmpty(inputData.Message))
            {
                if (!string.IsNullOrEmpty(inputData.Mail))
                    outputs.Add(await context.CallActivityAsync<string>("Notifier_SendMail", inputData));
                if (!string.IsNullOrEmpty(inputData.PhoneNumber))
                    outputs.Add(await context.CallActivityAsync<string>("Notifier_SendSMS", inputData));
            }
            return outputs;
        }
        #endregion
    }
}