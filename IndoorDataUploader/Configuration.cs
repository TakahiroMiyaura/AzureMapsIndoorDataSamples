// Copyright (c) 2021 Takahiro Miyaura
// Released under the MIT license
// http://opensource.org/licenses/mit-license.php

namespace IndoorDataUploader
{
    public class Configuration
    {
        public const string AZURE_MAPS_SUBSCRIPTION_KEY = "hyl5xEK1st2ldO5u3qo21-zV_AiPtAcUNLFlenrJbcg";

        public const string AZURE_MAPS_HOST = "us.atlas.microsoft.com";

        public const string API_VERSION = "1.0";


        public string InputDWGPackagePath { get; set; }
        public string InputStateSetPath { get; set; }
        public string DataSetId { get; set; }
        public string TileSetId { get; set; }
        public string StyleSetId { get; set; }
    }
}