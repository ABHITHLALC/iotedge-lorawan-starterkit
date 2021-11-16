// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoraKeysManagerFacade
{
    using System;

    internal static class EUIValidator
    {
        private const string InvalidZero = "0000000000000000";

        /// <summary>
        /// DevEUI are required to be IEEE EUI-64.
        /// </summary>
        /// <param name="devEUI">devEUI to validate.</param>
        /// <exception cref="ArgumentException">If an invalid devEUI string was passed.</exception>
        internal static void ValidateDevEUI(string devEUI)
        {
            const int ExpectedLength = 16;

            if (!string.IsNullOrEmpty(devEUI))
            {
                devEUI = devEUI.Trim();
                if (devEUI.Length == ExpectedLength && devEUI != InvalidZero)
                {
                    for (var i = 0; i < devEUI.Length; i++)
                    {
                        var c = devEUI[i];
                        var isHex = c is (>= '0' and <= '9') or (>= 'a' and <= 'f') or (>= 'A' and <= 'F');
                        if (!isHex)
                        {
                            throw new ArgumentException($"Invalid DevEUI '{devEUI}'");
                        }
                    }

                    return; // valid
                }
            }

            throw new ArgumentException($"Invalid DevEUI '{devEUI}'");
        }
    }
}