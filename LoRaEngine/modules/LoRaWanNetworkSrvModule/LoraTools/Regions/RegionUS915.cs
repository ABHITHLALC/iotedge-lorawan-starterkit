// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoRaTools.Regions
{
    using System;
    using System.Collections.Generic;
    using LoRaTools.LoRaPhysical;
    using LoRaTools.Utils;
    using LoRaWan;
    using static LoRaWan.DataRateIndex;
    using static LoRaWan.Metric;

    public class RegionUS915 : Region
    {
        // Frequencies calculated according to formula:
        // 923.3 + upstreamChannelNumber % 8 * 0.6,
        // rounded to first decimal point
        private static readonly Hertz[] DownstreamChannelFrequencies =
        {
            Mega(923.3),
            Mega(923.9),
            Mega(924.5),
            Mega(925.1),
            Mega(925.7),
            Mega(926.3),
            Mega(926.9),
            Mega(927.5)
       };

        public RegionUS915()
            : base(LoRaRegionType.US915)
        {
            DRtoConfiguration.Add(DR0, (LoRaDataRate.SF10BW125, MaxPayloadSize: 19));
            DRtoConfiguration.Add(DR1, (LoRaDataRate.SF9BW125, MaxPayloadSize: 61));
            DRtoConfiguration.Add(DR2, (LoRaDataRate.SF8BW125, MaxPayloadSize: 133));
            DRtoConfiguration.Add(DR3, (LoRaDataRate.SF7BW125, MaxPayloadSize: 250));
            DRtoConfiguration.Add(DR4, (LoRaDataRate.SF8BW500, MaxPayloadSize: 250));
            DRtoConfiguration.Add(DR8, (LoRaDataRate.SF12BW500, MaxPayloadSize: 61));
            DRtoConfiguration.Add(DR9, (LoRaDataRate.SF11BW500, MaxPayloadSize: 137));
            DRtoConfiguration.Add(DR10, (LoRaDataRate.SF10BW500, MaxPayloadSize: 250));
            DRtoConfiguration.Add(DR11, (LoRaDataRate.SF9BW500, MaxPayloadSize: 250));
            DRtoConfiguration.Add(DR12, (LoRaDataRate.SF8BW500, MaxPayloadSize: 250));
            DRtoConfiguration.Add(DR13, (LoRaDataRate.SF7BW500, MaxPayloadSize: 250));

            for (uint i = 0; i < 14; i++)
            {
                TXPowertoMaxEIRP.Add(i, 30 - i);
            }

            RX1DROffsetTable = new[]
            {
                new[] { DR10, DR9,  DR8,  DR8  },
                new[] { DR11, DR10, DR9,  DR8  },
                new[] { DR12, DR11, DR10, DR9  },
                new[] { DR13, DR12, DR11, DR10 },
                new[] { DR13, DR13, DR12, DR11 },
            };

            var upstreamValidDataranges = new HashSet<DataRate>
            {
                LoRaDataRate.SF10BW125, // 0
                LoRaDataRate.SF9BW125,  // 1
                LoRaDataRate.SF8BW125,  // 2
                LoRaDataRate.SF7BW125,  // 3
                LoRaDataRate.SF8BW500,  // 4
            };

            var downstreamValidDataranges = new HashSet<DataRate>
            {
                LoRaDataRate.SF12BW500, // 8
                LoRaDataRate.SF11BW500, // 9
                LoRaDataRate.SF10BW500, // 10
                LoRaDataRate.SF9BW500,  // 11
                LoRaDataRate.SF8BW500,  // 12
                LoRaDataRate.SF7BW500   // 13
            };

            MaxADRDataRate = DR3;
            RegionLimits = new RegionLimits((Min: Mega(902.3), Max: Mega(927.5)), upstreamValidDataranges, downstreamValidDataranges, DR0, DR8);
        }

        /// <summary>
        /// Logic to get the correct downstream transmission frequency for region US915.
        /// </summary>
        /// <param name="upstreamChannel">the channel at which the message was transmitted.</param>
        /// <param name="deviceJoinInfo">Join info for the device, if applicable.</param>
        [Obsolete("#655 - This Rxpk based implementation will go away as soon as the complete LNS implementation is done.")]
        public override bool TryGetDownstreamChannelFrequency(Rxpk upstreamChannel, out double frequency, DeviceJoinInfo deviceJoinInfo = null)
        {
            if (upstreamChannel is null) throw new ArgumentNullException(nameof(upstreamChannel));

            if (!IsValidUpstreamRxpk(upstreamChannel))
                throw new LoRaProcessingException($"Invalid upstream channel: {upstreamChannel.Freq}, {upstreamChannel.Datr}.");

            int upstreamChannelNumber;
            // if DR4 the coding are different.
            if (upstreamChannel.Datr == LoRaDataRate.SF8BW500.XpkDatr)
            {
                // ==DR4
                upstreamChannelNumber = 64 + (int)Math.Round((upstreamChannel.Freq - 903) / 1.6, 0, MidpointRounding.AwayFromZero);
            }
            else
            {
                // if not DR4 other encoding
                upstreamChannelNumber = (int)Math.Round((upstreamChannel.Freq - 902.3) / 0.2, 0, MidpointRounding.AwayFromZero);
            }

            frequency = DownstreamChannelFrequencies[upstreamChannelNumber % 8].InMega;
            return true;
        }

        /// <summary>
        /// Logic to get the correct downstream transmission frequency for region US915.
        /// </summary>
        /// <param name="upstreamFrequency">Frequency on which the message was transmitted.</param>
        /// <param name="upstreamDataRate">Data rate at which the message was transmitted.</param>
        /// <param name="deviceJoinInfo">Join info for the device, if applicable.</param>
        public override bool TryGetDownstreamChannelFrequency(Hertz upstreamFrequency, out Hertz downstreamFrequency, DataRateIndex? upstreamDataRate, DeviceJoinInfo deviceJoinInfo = null)
        {
            if (upstreamDataRate is null) throw new ArgumentNullException(nameof(upstreamDataRate));

            if (!IsValidUpstreamFrequency(upstreamFrequency))
                throw new LoRaProcessingException($"Invalid upstream frequency {upstreamFrequency}", LoRaProcessingErrorCode.InvalidFrequency);

            if (!IsValidUpstreamDataRate(upstreamDataRate.Value))
                throw new LoRaProcessingException($"Invalid upstream data rate {upstreamDataRate}", LoRaProcessingErrorCode.InvalidDataRate);

            int upstreamChannelNumber;
            upstreamChannelNumber = upstreamDataRate == DR4 ? 64 + (int)Math.Round((upstreamFrequency.InMega - 903) / 1.6, 0, MidpointRounding.AwayFromZero)
                                                            : (int)Math.Round((upstreamFrequency.InMega - 902.3) / 0.2, 0, MidpointRounding.AwayFromZero);
            downstreamFrequency = DownstreamChannelFrequencies[upstreamChannelNumber % 8];
            return true;
        }

        /// <summary>
        /// Returns the default RX2 receive window parameters - frequency and data rate.
        /// </summary>
        /// <param name="deviceJoinInfo">Join info for the device, if applicable.</param>
        public override RX2ReceiveWindow GetDefaultRX2ReceiveWindow(DeviceJoinInfo deviceJoinInfo = null) => new RX2ReceiveWindow(Mega(923.3), DR8);
    }
}
