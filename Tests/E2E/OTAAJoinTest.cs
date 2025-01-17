// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoRaWan.Tests.E2E
{
    using System;
    using System.Globalization;
    using System.Threading.Tasks;
    using LoRaWan.Tests.Common;
    using Xunit;
    using Xunit.Sdk;
    using XunitRetryHelper;

    // Tests OTAA join requests
    // OTAA joins requires the following information:
    // - DevEUI: a globally unique end-device identifier
    // - AppEUI: application identifier
    // - AppKey: a AES-128 key
    [Collection(Constants.TestCollectionName)] // run in serial
    [Trait("Category", "SkipWhenLiveUnitTesting")]
    public sealed class OTAAJoinTest : IntegrationTestBaseCi
    {
        public OTAAJoinTest(IntegrationTestFixtureCi testFixture)
            : base(testFixture)
        {
        }

        // Ensures that an OTAA join will update the device twin
        // Uses Device1_OTAA
        [RetryFact]
        public async Task OTAA_Join_With_Valid_Device_Updates_DeviceTwin()
        {
            var device = TestFixtureCi.Device1_OTAA;
            LogTestStart(device);

            var twinBeforeJoin = await TestFixtureCi.GetTwinAsync(device.DeviceID);
            await ArduinoDevice.setDeviceModeAsync(LoRaArduinoSerial._device_mode_t.LWOTAA);
            await ArduinoDevice.setIdAsync(device.DevAddr, device.DeviceID, device.AppEui);
            await ArduinoDevice.setKeyAsync(device.NwkSKey, device.AppSKey, device.AppKey);

            await ArduinoDevice.SetupLora(TestFixtureCi.Configuration);

            var joinSucceeded = await ArduinoDevice.setOTAAJoinAsyncWithRetry(LoRaArduinoSerial._otaa_join_cmd_t.JOIN, 20000, 5);

            Assert.True(joinSucceeded, "Join failed");

            await Task.Delay(Constants.DELAY_FOR_SERIAL_AFTER_JOIN);

            // After join: Expectation on serial
            // +JOIN: Network joined
            // +JOIN: NetID 010000 DevAddr 02:9B:0D:3E
            // Assert.Contains("+JOIN: Network joined", this.lora.SerialLogs);
            await AssertUtils.ContainsWithRetriesAsync(
                (s) => s.StartsWith("+JOIN: NetID", StringComparison.Ordinal),
                ArduinoDevice.SerialLogs);

            // verify status in device twin
            await Task.Delay(TimeSpan.FromSeconds(60));
            var twinAfterJoin = await TestFixtureCi.GetTwinAsync(device.DeviceID);
            Assert.NotNull(twinAfterJoin);
            Assert.NotNull(twinAfterJoin.Properties.Reported);
            try
            {
                Assert.True(twinAfterJoin.Properties.Reported.Contains("FCntUp"), "Property FCntUp does not exist");
                Assert.True(twinAfterJoin.Properties.Reported.Contains("FCntDown"), "Property FCntDown does not exist");
                Assert.True(twinAfterJoin.Properties.Reported.Contains("NetId"), "Property NetId does not exist");
                Assert.True(twinAfterJoin.Properties.Reported.Contains("DevAddr"), "Property DevAddr does not exist");
                Assert.True(twinAfterJoin.Properties.Reported.Contains("DevNonce"), "Property DevNonce does not exist");
                Assert.True(twinAfterJoin.Properties.Reported.Contains("NwkSKey"), "Property NwkSKey does not exist");
                Assert.True(twinAfterJoin.Properties.Reported.Contains("AppSKey"), "Property AppSKey does not exist");
                Assert.True(twinAfterJoin.Properties.Reported.Contains("DevEUI"), "Property DevEUI does not exist");
                var devAddrBefore = (string)twinBeforeJoin.Properties.Reported["DevAddr"];
                var devAddrAfter = (string)twinAfterJoin.Properties.Reported["DevAddr"];
                var actualReportedDevEUI = (string)twinAfterJoin.Properties.Reported["DevEUI"];
                Assert.NotEqual(devAddrAfter, devAddrBefore);
                Assert.Equal(device.DeviceID, actualReportedDevEUI);
                Assert.True(twinBeforeJoin.Properties.Reported.Version < twinAfterJoin.Properties.Reported.Version, "Twin was not updated after join");
                Log($"[INFO] Twin was updated successfully. Version changed from {twinBeforeJoin.Properties.Reported.Version} to {twinAfterJoin.Properties.Reported.Version}");
            }
            catch (XunitException xunitException)
            {
                if (TestFixtureCi.Configuration.IoTHubAssertLevel == LogValidationAssertLevel.Warning)
                {
                    Log($"[WARN] {nameof(OTAA_Join_With_Valid_Device_Updates_DeviceTwin)} failed. {xunitException}");
                }
                else if (TestFixtureCi.Configuration.IoTHubAssertLevel == LogValidationAssertLevel.Error)
                {
                    throw;
                }
            }
        }

        // Ensure that a join with an invalid DevEUI fails
        // Does not need a real device, because the goal is no to have one that matches the DevEUI
        // Uses Device2_OTAA
        [RetryFact]
        public async Task OTAA_Join_With_Wrong_DevEUI_Fails()
        {
            var device = TestFixtureCi.Device2_OTAA;
            LogTestStart(device);
            await ArduinoDevice.setDeviceModeAsync(LoRaArduinoSerial._device_mode_t.LWOTAA);
            await ArduinoDevice.setIdAsync(device.DevAddr, device.DeviceID, device.AppEui);
            await ArduinoDevice.setKeyAsync(device.NwkSKey, device.AppSKey, device.AppKey);

            await ArduinoDevice.SetupLora(TestFixtureCi.Configuration);

            var joinSucceeded = await ArduinoDevice.setOTAAJoinAsyncWithRetry(LoRaArduinoSerial._otaa_join_cmd_t.JOIN, 20000, 3);
            Assert.False(joinSucceeded, "Join suceeded for invalid DevEUI");

            await ArduinoDevice.WaitForIdleAsync();
        }

        // Ensure that a join with an invalid AppKey fails
        // Uses Device3_OTAA
        [RetryFact]
        public async Task OTAA_Join_With_Wrong_AppKey_Fails()
        {
            var device = TestFixtureCi.Device3_OTAA;
            LogTestStart(device);
            var appKeyToUse = AppKey.Parse("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF");
            Assert.NotEqual(appKeyToUse, device.AppKey);
            await ArduinoDevice.setDeviceModeAsync(LoRaArduinoSerial._device_mode_t.LWOTAA);
            await ArduinoDevice.setIdAsync(device.DevAddr, device.DeviceID, device.AppEui);
            await ArduinoDevice.setKeyAsync(device.NwkSKey, device.AppSKey, appKeyToUse);

            await ArduinoDevice.SetupLora(TestFixtureCi.Configuration);

            var joinSucceeded = await ArduinoDevice.setOTAAJoinAsyncWithRetry(LoRaArduinoSerial._otaa_join_cmd_t.JOIN, 20000, 3);
            Assert.False(joinSucceeded, "Join suceeded for invalid AppKey (mic check should fail)");
            await TestFixtureCi.AssertNetworkServerModuleLogStartsWithAsync(
                $"{device.DeviceID}: join refused: invalid MIC",
                $"{device.DeviceID}: join request MIC invalid");

            await ArduinoDevice.WaitForIdleAsync();
        }

        // Ensure that a join with an invalid AppKey fails
        // Uses Device13_OTAA
        [RetryFact]
        public async Task OTAA_Join_With_Wrong_AppEUI_Fails()
        {
            var device = TestFixtureCi.Device13_OTAA;
            LogTestStart(device);

            var appEUIToUse = new JoinEui(0xFF7A00000000FCE3);
            Assert.NotEqual(appEUIToUse, device.AppEui);
            await ArduinoDevice.setDeviceModeAsync(LoRaArduinoSerial._device_mode_t.LWOTAA);
            await ArduinoDevice.setIdAsync(device.DevAddr, device.DeviceID, appEUIToUse);
            await ArduinoDevice.setKeyAsync(device.NwkSKey, device.AppSKey, device.AppKey);

            await ArduinoDevice.SetupLora(TestFixtureCi.Configuration);

            var joinSucceeded = await ArduinoDevice.setOTAAJoinAsyncWithRetry(LoRaArduinoSerial._otaa_join_cmd_t.JOIN, 20000, 3);
            Assert.False(joinSucceeded, "Join suceeded for invalid AppKey");

            await ArduinoDevice.WaitForIdleAsync();
        }

        [RetryFact]
        public Task Test_OTAA_Join_Send_And_Rejoin_With_Custom_RX2_DR_Single()
        {
            var device = TestFixtureCi.GetDeviceByPropertyName(nameof(TestFixtureCi.Device20_OTAA));
            LogTestStart(device);
            return Test_OTAA_Join_Send_And_Rejoin_With_Custom_RX2_DR(device);
        }

        [RetryFact]
        public Task Test_OTAA_Join_Send_And_Rejoin_With_Custom_RX2_DR_MultiGw()
        {
            var device = TestFixtureCi.GetDeviceByPropertyName(nameof(TestFixtureCi.Device20_OTAA_MultiGw));
            LogTestStart(device);
            return Test_OTAA_Join_Send_And_Rejoin_With_Custom_RX2_DR(device);
        }

        // Performs a OTAA join and sends 1 unconfirmed, 1 confirmed and rejoins
        private async Task Test_OTAA_Join_Send_And_Rejoin_With_Custom_RX2_DR(TestDeviceInfo device)
        {
            await ArduinoDevice.setDeviceModeAsync(LoRaArduinoSerial._device_mode_t.LWOTAA);
            await ArduinoDevice.setIdAsync(device.DevAddr, device.DeviceID, device.AppEui);
            await ArduinoDevice.setKeyAsync(device.NwkSKey, device.AppSKey, device.AppKey);

            await ArduinoDevice.SetupLora(TestFixtureCi.Configuration);

            var joinSucceeded = await ArduinoDevice.setOTAAJoinAsyncWithRetry(LoRaArduinoSerial._otaa_join_cmd_t.JOIN, 20000, 5);
            Assert.True(joinSucceeded, "Join failed");

            // wait 1 second after joined
            await Task.Delay(Constants.DELAY_FOR_SERIAL_AFTER_JOIN);

            if (device.IsMultiGw)
            {
                await TestFixtureCi.WaitForTwinSyncAfterJoinAsync(ArduinoDevice.SerialLogs, device.DevEui);
            }

            // Sends 1x unconfirmed messages
            TestFixtureCi.ClearLogs();

            var msg = PayloadGenerator.Next().ToString(CultureInfo.InvariantCulture);
            await ArduinoDevice.transferPacketAsync(msg, 10);

            await Task.Delay(Constants.DELAY_FOR_SERIAL_AFTER_SENDING_PACKET);

            // After transferPacket: Expectation from serial
            // +MSG: Done
            await AssertUtils.ContainsWithRetriesAsync("+MSG: Done", ArduinoDevice.SerialLogs);

            // 0000000000000004: valid frame counter, msg: 1 server: 0
            await TestFixtureCi.AssertNetworkServerModuleLogStartsWithAsync($"{device.DeviceID}: valid frame counter, msg:");

            // 0000000000000004: decoding with: DecoderValueSensor port: 8
            await TestFixtureCi.AssertNetworkServerModuleLogStartsWithAsync($"{device.DeviceID}: decoding with: {device.SensorDecoder} port:");

            // 0000000000000004: message '{"value": 51}' sent to hub
            await TestFixtureCi.AssertNetworkServerModuleLogStartsWithAsync($"{device.DeviceID}: message '{{\"value\":{msg}}}' sent to hub");

            // Ensure device payload is available
            // Data: {"value": 51}
            var expectedPayload = $"{{\"value\":{msg}}}";
            await TestFixtureCi.AssertIoTHubDeviceMessageExistsAsync(device.DeviceID, expectedPayload, new SearchLogOptions(expectedPayload));

            await Task.Delay(Constants.DELAY_BETWEEN_MESSAGES);

            TestFixtureCi.ClearLogs();

            msg = PayloadGenerator.Next().ToString(CultureInfo.InvariantCulture);
            await ArduinoDevice.transferPacketWithConfirmedAsync(msg, 10);

            await Task.Delay(Constants.DELAY_FOR_SERIAL_AFTER_SENDING_PACKET);

            // After transferPacketWithConfirmed: Expectation from serial
            // +CMSG: ACK Received
            await AssertUtils.ContainsWithRetriesAsync("+CMSG: ACK Received", ArduinoDevice.SerialLogs);

            // Checking than the communication occurs on DR 4 and RX2 as part of preferred windows RX2 and custom RX2 DR
            await AssertUtils.ContainsWithRetriesAsync(x => x.StartsWith("+CMSG: RXWIN2", StringComparison.Ordinal), ArduinoDevice.SerialLogs);
            // this test has a custom datarate for RX 2 of 3
            const string logMessage2 = "\"Rx2\":{\"DataRate\":3";
            await TestFixtureCi.AssertNetworkServerModuleLogExistsAsync(x => x.Contains(logMessage2, StringComparison.Ordinal), new SearchLogOptions(logMessage2));


            // 0000000000000004: decoding with: DecoderValueSensor port: 8
            await TestFixtureCi.AssertNetworkServerModuleLogStartsWithAsync($"{device.DeviceID}: decoding with: {device.SensorDecoder} port:");

            // 0000000000000004: message '{"value": 51}' sent to hub
            await TestFixtureCi.AssertNetworkServerModuleLogStartsWithAsync($"{device.DeviceID}: message '{{\"value\":{msg}}}' sent to hub");

            // Ensure device payload is available
            // Data: {"value": 51}
            expectedPayload = $"{{\"value\":{msg}}}";
            await TestFixtureCi.AssertIoTHubDeviceMessageExistsAsync(device.DeviceID, expectedPayload, new SearchLogOptions(expectedPayload));

            await Task.Delay(Constants.DELAY_BETWEEN_MESSAGES);

            // rejoin
            await ArduinoDevice.setDeviceModeAsync(LoRaArduinoSerial._device_mode_t.LWOTAA);
            await ArduinoDevice.setIdAsync(device.DevAddr, device.DeviceID, device.AppEui);
            await ArduinoDevice.setKeyAsync(device.NwkSKey, device.AppSKey, device.AppKey);
            await ArduinoDevice.SetupLora(TestFixtureCi.Configuration);
            var joinSucceeded2 = await ArduinoDevice.setOTAAJoinAsyncWithRetry(LoRaArduinoSerial._otaa_join_cmd_t.JOIN, 20000, 5);
            Assert.True(joinSucceeded2, "Rejoin failed");

            if (device.IsMultiGw)
            {
                const string joinRefusedMsg = "join refused";
                var joinRefused = await TestFixtureCi.AssertNetworkServerModuleLogExistsAsync((s) => s.IndexOf(joinRefusedMsg, StringComparison.Ordinal) != -1, new SearchLogOptions(joinRefusedMsg));
                Assert.True(joinRefused.Found);
            }
        }
    }
}
