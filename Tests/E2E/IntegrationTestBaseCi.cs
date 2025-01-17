// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoRaWan.Tests.E2E
{
    using System;
    using System.Globalization;
    using System.Text;
    using LoRaWan.Tests.Common;
    using Xunit;

    public class IntegrationTestBaseCi : IntegrationTestBase, IClassFixture<IntegrationTestFixtureCi>, IDisposable
    {
        private bool isDisposed; // To detect redundant calls

        protected IntegrationTestFixtureCi TestFixtureCi => (IntegrationTestFixtureCi)TestFixture;

        protected LoRaArduinoSerial ArduinoDevice { get; }

        public IntegrationTestBaseCi(IntegrationTestFixtureCi testFixture)
            : base(testFixture)
        {
            ArduinoDevice = testFixture.ArduinoDevice;
        }

        protected static string ToHexString(string str)
        {
            var sb = new StringBuilder();

            var bytes = Encoding.UTF8.GetBytes(str);
            foreach (var t in bytes)
            {
                sb.Append(t.ToString("X2", CultureInfo.InvariantCulture));
            }

            return sb.ToString(); // returns: "48656C6C6F20776F726C64" for "Hello world"
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                if (disposing)
                {
                    // Before starting a new test, wait 5 seconds to ensure serial port is not receiving dirty data
                    if (ArduinoDevice != null)
                        ArduinoDevice.WaitForIdleAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
                }

                this.isDisposed = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }
    }
}
