// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json.Serialization;

namespace PrayerTimes
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CalculationMethods
    {
        Jafari,
        Karachi,
        ISNA,
        MWL,
        Makkah,
        Egypt,
        Custom
    }
}
