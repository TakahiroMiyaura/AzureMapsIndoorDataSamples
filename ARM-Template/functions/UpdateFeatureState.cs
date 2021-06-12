// Copyright (c) 2021 Takahiro Miyaura
// Released under the MIT license
// http://opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LinkToAzureMapsFunctionApps
{
    public static class UpdateFeatureState
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static string geography = Environment.GetEnvironmentVariable("GEOGRAPHY");
        private static string statesetId = Environment.GetEnvironmentVariable("STATE_SET_ID");
        private static string datasetId = Environment.GetEnvironmentVariable("DATA_SET_ID");

        private static string azureMapsSubscriptionKey =
            Environment.GetEnvironmentVariable("AZURE_MAPS_SUBSCRIPTION_KEY");
        
        [FunctionName("updatemapsfeaturestate")]
        public static async Task Run([EventGridTrigger] EventGridEvent eventGridEvent, ILogger log)
        {
            if (!eventGridEvent.EventType.Contains("telemetry"))
            {
                try
                {
                    var turbineId = eventGridEvent.Subject;
                    var eventGridData = (JObject) JsonConvert.DeserializeObject(eventGridEvent.Data.ToString());
                    var data = eventGridData.SelectToken("data");
                    var patch = data.SelectToken("patch");
                    var alert = false;
                    foreach (var token in patch)
                    {
                        if (token["path"].ToString() == "/Alert")
                        {
                            alert = token["value"].ToObject<bool>();
                        }
                    }

                    log.LogInformation($"try get featureId for {turbineId}");
                    var url =
                        $"https://{geography}.atlas.microsoft.com/wfs/datasets/{datasetId}/collections/unit/items?subscription-key={azureMapsSubscriptionKey}&api-version=1.0&limit=1&name={turbineId}";
                    var request = new HttpRequestMessage(HttpMethod.Get, url);
                    var responseMessage = await httpClient.SendAsync(request);
                    if (!responseMessage.IsSuccessStatusCode)
                    {
                        log.LogInformation($"Status Code:{responseMessage.StatusCode}");
                        log.LogInformation(await responseMessage.Content.ReadAsStringAsync());
                        return;
                    }

                    var jsonObj = JObject.Parse(await responseMessage.Content.ReadAsStringAsync());
                    var featureId = jsonObj["features"].First["id"].Value<string>();
                    log.LogInformation($"detect featureId of {turbineId} : {featureId}");

                    log.LogInformation($"setting alert to: {alert}");
                    var property = new Dictionary<object, List<Dictionary<object, object>>>
                    {
                        {
                            "states", new List<Dictionary<object, object>>
                            {
                                new Dictionary<object, object>
                                {
                                    {"keyName", "alert"},
                                    {"value", alert},
                                    {"eventTimestamp", DateTime.Now.ToString("s")}
                                }
                            }
                        }
                    };
                    url =
                        $"https://{geography}.atlas.microsoft.com/featureState/state?api-version=1.0&statesetID={statesetId}&featureID={featureId}&subscription-key={azureMapsSubscriptionKey}";

                    request = new HttpRequestMessage(HttpMethod.Post, url);
                    var jsonStr = JsonConvert.SerializeObject(property, Formatting.Indented);
                    request.Content = new StringContent(jsonStr, Encoding.UTF8, "application/json");
                    log.LogInformation(url);
                    log.LogInformation(jsonStr);
                    responseMessage = await httpClient.SendAsync(request);
                    if (!responseMessage.IsSuccessStatusCode)
                    {
                        log.LogInformation($"Status Code:{responseMessage.StatusCode}");
                        log.LogInformation(await responseMessage.Content.ReadAsStringAsync());
                    }
                }
                catch (Exception e)
                {
                    log.LogInformation(e.Message);
                }
            }
        }
    }
}