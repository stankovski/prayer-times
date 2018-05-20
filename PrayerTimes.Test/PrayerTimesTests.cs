// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace PrayerTimes.Test
{
    public class PrayerTimesTest
    {
        [Fact]
        public void TestTimeForDst()
        {
            PrayerTimesCalculator calc = new PrayerTimesCalculator(47.660918, -122.136371);
            calc.CalculationMethod = CalculationMethods.ISNA;
            calc.AsrJurusticMethod = AsrJuristicMethods.Shafii;
            var times = calc.GetPrayerTimes(new DateTime(2015, 8, 3), -7);

            Assert.Equal(new DateTime(2015, 8, 3), times.Date);
            Assert.Equal(new TimeSpan(4,1,0), times.Fajr);
            Assert.Equal(new TimeSpan(5,48,0), times.Sunrise);
            Assert.Equal(new TimeSpan(13,15,0), times.Dhuhr);
            Assert.Equal(new TimeSpan(17,18,0), times.Asr);
            Assert.Equal(new TimeSpan(20,40,0), times.Maghrib);
            Assert.Equal(new TimeSpan(20,40,0), times.Sunset);
            Assert.Equal(new TimeSpan(22,28,0), times.Isha);
        }

        [Fact]
        public void TestTimeForDstAutoTimeZone()
        {
            PrayerTimesCalculator calc = new PrayerTimesCalculator(47.660918, -122.136371);
            calc.CalculationMethod = CalculationMethods.ISNA;
            calc.AsrJurusticMethod = AsrJuristicMethods.Shafii;
            var times = calc.GetPrayerTimes(new DateTime(2015, 8, 3, 0, 0, 0, DateTimeKind.Local));

            Assert.Equal(new DateTime(2015, 8, 3, 0, 0, 0, DateTimeKind.Local), times.Date);
            Assert.Equal(new TimeSpan(4, 1, 0), times.Fajr);
            Assert.Equal(new TimeSpan(5, 48, 0), times.Sunrise);
            Assert.Equal(new TimeSpan(13, 15, 0), times.Dhuhr);
            Assert.Equal(new TimeSpan(17, 18, 0), times.Asr);
            Assert.Equal(new TimeSpan(20, 40, 0), times.Maghrib);
            Assert.Equal(new TimeSpan(20, 40, 0), times.Sunset);
            Assert.Equal(new TimeSpan(22, 28, 0), times.Isha);
        }

        [Fact]
        public void TestTimeForNonDst()
        {
            PrayerTimesCalculator calc = new PrayerTimesCalculator(47.660918, -122.136371);
            calc.CalculationMethod = CalculationMethods.ISNA;
            calc.AsrJurusticMethod = AsrJuristicMethods.Shafii;
            var times = calc.GetPrayerTimes(new DateTime(2015, 2, 3), -7);

            Assert.Equal(new DateTime(2015, 2, 3), times.Date);
            Assert.Equal(new TimeSpan(6, 8, 0), times.Fajr);
            Assert.Equal(new TimeSpan(7, 37, 0), times.Sunrise);
            Assert.Equal(new TimeSpan(12, 22, 0), times.Dhuhr);
            Assert.Equal(new TimeSpan(14, 44, 0), times.Asr);
            Assert.Equal(new TimeSpan(17, 08, 0), times.Maghrib);
            Assert.Equal(new TimeSpan(17, 08, 0), times.Sunset);
            Assert.Equal(new TimeSpan(18, 36, 0), times.Isha);
        }

        [Fact]
        public void TestTimesForYear()
        {
            PrayerTimesCalculator calc = new PrayerTimesCalculator(47.660918, -122.136371);
            calc.CalculationMethod = CalculationMethods.ISNA;
            calc.AsrJurusticMethod = AsrJuristicMethods.Shafii;
            var times = new Times[365];
            for (int i = 0; i < 365; i ++)
            {
                var date = new DateTimeOffset(new DateTime(2015, 1, 1));
                times[i] = calc.GetPrayerTimes(date.AddDays(i), -7);
            }

            Assert.Equal(new DateTime(2015, 2, 3), times[33].Date);
            Assert.Equal(new TimeSpan(6, 8, 0), times[33].Fajr);
        }

        [Fact]
        public void TestGetFajrAngle()
        {
            PrayerTimesCalculator calculator = new PrayerTimesCalculator(49.744, 6.6188);

            var fajrAngle = calculator.GetFajrAngle(new TimeSpan(3, 33, 0), DateTimeOffset.Now);
            var ishaAngle = calculator.GetFajrAngle(new TimeSpan(23, 15, 0), DateTimeOffset.Now);

        }
    }
}
