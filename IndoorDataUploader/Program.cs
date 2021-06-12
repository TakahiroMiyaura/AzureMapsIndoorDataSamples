// Copyright (c) 2021 Takahiro Miyaura
// Released under the MIT license
// http://opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace IndoorDataUploader
{
    internal class Program
    {
    #region Main

        private static async Task Main(string[] args)
        {
            //for samples
            configuration.InputDWGPackagePath = @"Resources\Plant.zip";
            configuration.InputStateSetPath = @"Resources\StateSet.json";

            if (args.Length >= 2)
            {
                if (args[0].Equals("/?") || args[0].Equals("-?"))
                {
                    Console.WriteLine(
                        $"Usage: {AppDomain.CurrentDomain.FriendlyName} <dwg package path> <stateSet Json path>");
                    return;
                }

                configuration.InputDWGPackagePath = args[0];
                configuration.InputStateSetPath = args[1];
            }

            var isSucceed = true;
            try
            {
                var udId = await UploadDwgFile(configuration.InputDWGPackagePath);
                if (udId.Equals("Failed"))
                {
                    isSucceed = false;
                    return;
                }

                var conversionId = await ConvertDrawingPackage(udId);
                if (conversionId.Equals("Failed"))
                {
                    isSucceed = false;
                    return;
                }

                configuration.DataSetId = await CreateDataSet(conversionId);
                if (configuration.DataSetId.Equals("Failed"))
                {
                    isSucceed = false;
                    return;
                }

                configuration.TileSetId = await CreateTileSet(configuration.DataSetId);
                if (configuration.TileSetId.Equals("Failed"))
                {
                    isSucceed = false;
                }

                configuration.StyleSetId =
                    await CreateStateSet(configuration.DataSetId, configuration.InputStateSetPath);
                if (configuration.StyleSetId.Equals("Failed"))
                {
                    isSucceed = false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                isSucceed = false;
            }
            finally
            {
                if (isSucceed)
                {
                    var json = JsonSerializer.Serialize(new
                    {
                        datasetId = configuration.DataSetId,
                        tilesetId = configuration.TileSetId,
                        statesetId = configuration.StyleSetId
                    }, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    Console.WriteLine(json);
                }
            }
        }

    #endregion

    #region private fields

        private static readonly char[] bars = {'／', '―', '＼', '｜'};
        private static HttpClient httpClient;
        private static readonly Configuration configuration = new Configuration();

    #endregion

    #region Operations for Azure Maps

        private static async Task<string> UploadDwgFile(string filePath)
        {
            Console.WriteLine("step.1 Upload Dwg Packages.... ");
            var requestURL =
                $"https://{Configuration.AZURE_MAPS_HOST}/mapData/upload?api-version={Configuration.API_VERSION}&dataFormat=zip&subscription-key={Configuration.AZURE_MAPS_SUBSCRIPTION_KEY}";
            var request = new HttpRequestMessage(HttpMethod.Post,
                requestURL);
            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            request.Content = new StreamContent(stream);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            var uploadRes = await SendRequest(request);
            if (!uploadRes.IsSuccessStatusCode) return "Failed";
            var tempPath = uploadRes.Headers.Location.LocalPath;
            var UploadOperationId = tempPath.Substring(tempPath.LastIndexOf("/") + 1);


            Console.WriteLine($"step.2 Check Upload Status.... > OperationId:{UploadOperationId}");
            requestURL =
                $"https://{Configuration.AZURE_MAPS_HOST}/mapData/operations/{UploadOperationId}?api-version={Configuration.API_VERSION}&subscription-key={Configuration.AZURE_MAPS_SUBSCRIPTION_KEY}";
            var resourceLocation = string.Empty;
            Dictionary<string, JsonElement> resourceRes;
            var isSucceed = false;
            do
            {
                request = new HttpRequestMessage(HttpMethod.Get, requestURL);
                var uploadStatusRes = await SendRequest(request);
                if (!uploadStatusRes.IsSuccessStatusCode) return "Failed";
                var uploadStatusContent = await uploadStatusRes.Content.ReadAsStringAsync();
                resourceRes = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(uploadStatusContent);
                isSucceed = resourceRes["status"].GetString() == "Succeeded";

                await ProgressChar(isSucceed);
            } while (!isSucceed);

            requestURL = resourceRes["resourceLocation"].GetString() +
                         $"&subscription-key={Configuration.AZURE_MAPS_SUBSCRIPTION_KEY}";

            WriteResults(resourceRes);


            Console.WriteLine("step.3 Get meta data UDID....");
            request = new HttpRequestMessage(HttpMethod.Get, requestURL);
            var getUDIDRes = await SendRequest(request);
            if (!getUDIDRes.IsSuccessStatusCode) return "Failed";
            var resourceUDIDContent = await getUDIDRes.Content.ReadAsStringAsync();
            resourceRes = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(resourceUDIDContent);
            return resourceRes["udid"].GetString();
        }

        private static async Task<string> ConvertDrawingPackage(string udid)
        {
            string requestURL;
            HttpRequestMessage request;
            bool isSucceed;
            Console.WriteLine("step.4 Convert Drawing Package....");
            requestURL =
                $"https://{Configuration.AZURE_MAPS_HOST}/conversion/convert?subscription-key={Configuration.AZURE_MAPS_SUBSCRIPTION_KEY}&api-version={Configuration.API_VERSION}&udid={udid}&inputType=DWG";
            request = new HttpRequestMessage(HttpMethod.Post, requestURL);
            var conversionRes = await SendRequest(request);
            if (!conversionRes.IsSuccessStatusCode) return "Failed";
            var locationLocalPath = conversionRes.Headers.Location.LocalPath;
            var convertOperationId = locationLocalPath.Substring(locationLocalPath.LastIndexOf("/") + 1);

            Console.WriteLine($"step.5 Check Convert Drawing Package Status.... > {convertOperationId}");
            requestURL =
                $"https://{Configuration.AZURE_MAPS_HOST}/conversion/operations/{convertOperationId}?api-version={Configuration.API_VERSION}&subscription-key={Configuration.AZURE_MAPS_SUBSCRIPTION_KEY}";
            isSucceed = false;
            Dictionary<string, JsonElement> resourceRes;
            do
            {
                request = new HttpRequestMessage(HttpMethod.Get, requestURL);
                var createDataSetStatusRes = await SendRequest(request);
                if (!createDataSetStatusRes.IsSuccessStatusCode) return "Failed";
                var conversionStatusResContent = await createDataSetStatusRes.Content.ReadAsStringAsync();
                resourceRes = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(conversionStatusResContent);
                isSucceed = resourceRes["status"].GetString() == "Succeeded";
                if (resourceRes["status"].GetString() == "Failed")
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Convert Failed");
                    var errorProp = resourceRes["error"];
                    WriteErrors(errorProp);

                    Console.ResetColor();
                    return "Failed";
                }

                if (resourceRes.ContainsKey("warning"))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Convert warning");
                    var errorProp = resourceRes["warning"];
                    WriteErrors(errorProp);

                    Console.ResetColor();
                }

                await ProgressChar(isSucceed);
            } while (!isSucceed);

            var tempPath = new Uri(resourceRes["resourceLocation"].GetString()).LocalPath;
            var createDataSetId = tempPath.Substring(tempPath.LastIndexOf("/") + 1);

            WriteResults(resourceRes, createDataSetId);

            return createDataSetId;
        }

        private static async Task<string> CreateDataSet(string conversionId)
        {
            string requestURL;
            HttpRequestMessage request;
            string tempPath;
            bool isSucceed;
            Dictionary<string, JsonElement> resourceRes;
            Console.WriteLine($"step.6 Create Data Set.... > conversionID:{conversionId}");
            requestURL =
                $"https://{Configuration.AZURE_MAPS_HOST}/dataset/create?subscription-key={Configuration.AZURE_MAPS_SUBSCRIPTION_KEY}&api-version={Configuration.API_VERSION}&conversionID={conversionId}&type=facility";
            request = new HttpRequestMessage(HttpMethod.Post, requestURL);
            var createDataSetRes = await SendRequest(request);
            if (!createDataSetRes.IsSuccessStatusCode) return "Failed";
            tempPath = createDataSetRes.Headers.Location.LocalPath;
            var dataSetCreateOperationId = tempPath.Substring(tempPath.LastIndexOf("/") + 1);


            Console.WriteLine($"step.7 Check Create Data Set Status.... > OperationId:{dataSetCreateOperationId}");
            requestURL =
                $"https://{Configuration.AZURE_MAPS_HOST}/dataset/operations/{dataSetCreateOperationId}?api-version={Configuration.API_VERSION}&subscription-key={Configuration.AZURE_MAPS_SUBSCRIPTION_KEY}";
            isSucceed = false;
            do
            {
                request = new HttpRequestMessage(HttpMethod.Get, requestURL);
                var conversionStatusRes = await SendRequest(request);
                if (!conversionStatusRes.IsSuccessStatusCode) return "Failed";
                var conversionStatusResContent = await conversionStatusRes.Content.ReadAsStringAsync();
                resourceRes = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(conversionStatusResContent);
                isSucceed = resourceRes["status"].GetString() == "Succeeded";

                await ProgressChar(isSucceed);
            } while (!isSucceed);

            tempPath = new Uri(resourceRes["resourceLocation"].GetString()).LocalPath;
            var datasetiId = tempPath.Substring(tempPath.LastIndexOf("/") + 1);

            WriteResults(resourceRes, datasetiId);

            return datasetiId;
        }

        private static async Task<string> CreateTileSet(string datasetId)
        {
            string requestURL;
            HttpRequestMessage request;
            string tempPath;
            bool isSucceed;
            Dictionary<string, JsonElement> resourceRes;
            Console.WriteLine($"step.8 Create Tile Set....");
            requestURL =
                $"https://{Configuration.AZURE_MAPS_HOST}/tileset/create/vector?api-version={Configuration.API_VERSION}&datasetID={datasetId}&subscription-key={Configuration.AZURE_MAPS_SUBSCRIPTION_KEY}";
            request = new HttpRequestMessage(HttpMethod.Post, requestURL);
            var createTileSetRes = await SendRequest(request);
            if (!createTileSetRes.IsSuccessStatusCode) return "Failed";
            tempPath = createTileSetRes.Headers.Location.LocalPath;
            var tileSetCreateOperationId = tempPath.Substring(tempPath.LastIndexOf("/") + 1);


            Console.WriteLine($"step.9 Check Create Tile Set Status.... > OperationId:{tileSetCreateOperationId}");
            requestURL =
                $"https://{Configuration.AZURE_MAPS_HOST}/tileset/operations/{tileSetCreateOperationId}?api-version={Configuration.API_VERSION}&subscription-key={Configuration.AZURE_MAPS_SUBSCRIPTION_KEY}";
            isSucceed = false;
            do
            {
                request = new HttpRequestMessage(HttpMethod.Get, requestURL);
                var createTileSetStatusRes = await SendRequest(request);
                if (!createTileSetStatusRes.IsSuccessStatusCode) return "Failed";
                var conversionStatusResContent = await createTileSetStatusRes.Content.ReadAsStringAsync();
                resourceRes = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(conversionStatusResContent);
                isSucceed = resourceRes["status"].GetString() == "Succeeded";

                await ProgressChar(isSucceed);
            } while (!isSucceed);

            tempPath = new Uri(resourceRes["resourceLocation"].GetString()).LocalPath;
            var tilesetId = tempPath.Substring(tempPath.LastIndexOf("/") + 1);

            WriteResults(resourceRes, tilesetId);

            return tilesetId;
        }

        private static async Task<string> CreateStateSet(string datasetId, string filePath)
        {
            Console.WriteLine("step.10 Create StateSet.... ");
            var requestURL =
                $"https://{Configuration.AZURE_MAPS_HOST}/featureState/stateset?api-version={Configuration.API_VERSION}&datasetId={datasetId}&subscription-key={Configuration.AZURE_MAPS_SUBSCRIPTION_KEY}";
            var request = new HttpRequestMessage(HttpMethod.Post,
                requestURL);
            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            request.Content = new StreamContent(stream);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var stateSetRes = await SendRequest(request);
            if (!stateSetRes.IsSuccessStatusCode) return "Failed";
            var resourceUDIDContent = await stateSetRes.Content.ReadAsStringAsync();
            var resourceRes = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(resourceUDIDContent);
            var statesetId = resourceRes["statesetId"].GetString();
            WriteResults(resourceRes, statesetId);

            return statesetId;
        }

    #endregion

    #region Utils

        private static async Task<HttpResponseMessage> SendRequest(HttpRequestMessage request)
        {
            if (httpClient == null)
                httpClient = new ServiceCollection()
                    .AddHttpClient()
                    .BuildServiceProvider()
                    .GetService<IHttpClientFactory>()
                    .CreateClient();

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"{response.StatusCode}:{await response.Content.ReadAsStringAsync()}");
            }

            return response;
        }

        private static async Task ProgressChar(bool isSucceed)
        {
            for (var counter = 0; counter < (isSucceed ? 0 : 8); counter++)
            {
                Console.Write(bars[counter % 4]);
                await Task.Delay(new TimeSpan(0, 0, 0, 0, 500));
                Console.SetCursorPosition(0, Console.CursorTop);
            }
        }

        private static void WriteErrors(JsonElement errorProp, string prefix = " >>>")
        {
            foreach (var element in errorProp.EnumerateObject())
            {
                if (!element.NameEquals("details"))
                    Console.WriteLine($"{prefix} {element.Name} : {element.Value}");
            }

            if (errorProp.TryGetProperty("details", out var detailsProp))
            {
                foreach (var detailsElement in detailsProp.EnumerateArray())
                {
                    WriteErrors(detailsElement, prefix + ">>>");
                }
            }
        }

        private static void WriteResults(Dictionary<string, JsonElement> resourceRes, string Id = null)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            if (resourceRes.ContainsKey("status"))
            {
                Console.WriteLine($"{resourceRes["status"]}");
            }

            if (Id != null)
            {
                Console.WriteLine($" > Id:{Id}");
            }

            Console.ResetColor();
            Console.WriteLine();
        }

    #endregion
    }
}