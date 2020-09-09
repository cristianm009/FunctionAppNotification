// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
using FunctionAppNotification.Models;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FunctionAppNotification
{
    public static class NotifyByEvent
    {
        [FunctionName("NotifyByEvent")]
        public static void Run([EventGridTrigger] EventGridEvent eventGridEvent, ILogger log)
        {
            log.LogInformation(eventGridEvent.Data.ToString());
            Notification notificationData = GetInputData(eventGridEvent);
            log.LogInformation(notificationData.Mail ?? "No mail");
        }

        #region Auxiliar Methods
        private static Notification GetInputData(EventGridEvent eventGridEvent)
        {
            string eventGridData = eventGridEvent.Data.ToString();
            return JsonConvert.DeserializeObject<Notification>(eventGridData);
        }

        private static async Task<string> CallNotifyFunctionAsync(Notification notification)
        {
            using (var httpClient = new HttpClient())
            {
                var notificationData = JsonConvert.SerializeObject(notification);
                var notificationContent = new StringContent(notificationData, UnicodeEncoding.UTF8, "application/json");
                var response = await httpClient.PostAsync("", notificationContent);

                response.EnsureSuccessStatusCode();

                string content = await response.Content.ReadAsStringAsync();
            }
            return null;
        }

        #endregion
    }
}
