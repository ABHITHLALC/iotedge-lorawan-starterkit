﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoraKeysManagerFacade.IoTCentralImp
{
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class DesiredProperties
    {
        public string GatewayID { get; set; }

        public string DevAddr { get; set; }

        public string NwkSKey { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JToken> AdditionalData { get; set; } = new Dictionary<string, JToken>();
    }
}