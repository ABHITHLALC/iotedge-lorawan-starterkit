// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoRaWan.Tests.Unit.IoTHubImpl
{
    using System;
    using global::LoRaTools;
    using Microsoft.Azure.Devices.Shared;

    internal sealed class FakeIoTHubDeviceTwinTests : IDeviceTwin
    {
        public string ETag => throw new NotImplementedException();

        public TwinProperties Properties => throw new NotImplementedException();

        public TwinCollection Tags => throw new NotImplementedException();

        public string DeviceId => throw new NotImplementedException();

        public string GetGatewayID()
        {
            throw new NotImplementedException();
        }

        public string GetNwkSKey()
        {
            throw new NotImplementedException();
        }
    }
}
