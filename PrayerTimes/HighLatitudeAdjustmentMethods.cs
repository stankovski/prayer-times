// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace PrayerTimes
{
    public enum HighLatitudeAdjustmentMethods
    {
        None = 0,    // No adjustment
        MidNight = 1,    // middle of night
        OneSeventh = 2,    // 1/7th of night
        AngleBased = 3,    // angle/60th of night
    }
}
