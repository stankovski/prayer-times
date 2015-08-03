// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace PrayerTimes
{
    public class Times
    {
        public DateTimeOffset Date { get; set; }
        public TimeSpan Fajr { get; set; }
        public TimeSpan Sunrise { get; set; }
        public TimeSpan Dhuhr { get; set; }
        public TimeSpan Asr { get; set; }
        public TimeSpan Sunset { get; set; }
        public TimeSpan Maghrib { get; set; }
        public TimeSpan Isha { get; set; }
    }
}
