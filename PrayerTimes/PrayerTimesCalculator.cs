// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.
//
// This is a port of PrayerTimes calculator by Jameel Haffejee 
// Original  version here : http://tanzil.info/praytime/doc/manual/

using System;
using System.Collections.Generic;
using System.Globalization;

namespace PrayerTimes
{
    public record struct CalculatorParams(
        double Latitude, 
        double Longitude, 
        CalculationMethods CalculationMethod, 
        AsrJuristicMethods AsrJuristicMethod, 
        HighLatitudeAdjustmentMethods HighLatitudeAdjustmentMethod = HighLatitudeAdjustmentMethods.None);

    /// <summary>
    /// Prayer times calculator.
    /// </summary>
    public static class PrayerTimesCalculator
    {
        const int NumIterations = 1;    // number of iterations needed to compute times, this should never be more than 1;
        const int DhuhrMinutes = 0;     // minutes after mid-day for Dhuhr

        /// <summary>
        ///  methodParams[methodNum] = new Array(fa, ms, mv, is, iv);   
        ///     fa : fajr angle
        ///     ms : maghrib selector (0 = angle; 1 = minutes after sunset)
        ///     mv : maghrib parameter value (in angle or minutes)
        ///     is : isha selector (0 = angle; 1 = minutes after maghrib)
        ///     iv : isha parameter value (in angle or minutes)
        /// </summary>
        private static readonly Dictionary<CalculationMethods, double[]> _methodParams = new Dictionary<CalculationMethods, double[]> {
            { CalculationMethods.Jafari, new double[] { 16, 0, 4, 0, 14 } },
            { CalculationMethods.Karachi,new double[]{18, 1, 0, 0, 18} },
            { CalculationMethods.ISNA,new double[]{15, 1, 0, 0, 15} },
            { CalculationMethods.MWL,new double[]{18, 1, 0, 0, 17} },
            { CalculationMethods.Makkah,new double[]{19, 1, 0, 1, 90} },
            { CalculationMethods.Egypt,new double[]{19.5, 1, 0, 0, 17.5} },
            { CalculationMethods.Custom,new double[]{18, 1, 0, 0, 17} }};

        ///<summary>
        /// Returns the prayer times for a given date , the date format is specified as individual settings.
        /// </summary>
        /// <param name="date">Date time representing the date for which times should be calculated.</param>        
        /// <param name="timeZone">Time zone to use when calculating times. If omitted, time zone from date is used.</param>
        /// <returns>
        /// Times structure containing the Salaah times.
        /// </returns>
        public static Times GetPrayerTimes(DateTimeOffset date, CalculatorParams calculatorParams, 
            double? timeZone = null)
        {
            timeZone = EffectiveTimeZone(date, timeZone);
            var jDate = JulianDate(date.Year, date.Month, date.Day) - calculatorParams.Longitude / (15 * 24);
            var times = ComputeDayTimes(jDate, timeZone.Value, calculatorParams);
            times.Date = date;
            return times;
        }
       
        /// <summary>
        /// Converts a floating-point number representing time to a TimeSpan object.
        /// </summary>
        /// <param name="time">The floating-point number representing time.</param>
        /// <returns>A TimeSpan object representing the converted time.</returns>
        private static TimeSpan FloatToTimeSpan(double time)
        {
            time = FixHour(time + 0.5 / 60);  // add 0.5 minutes to round
            var hours = Math.Floor(time);
            var minutes = Math.Floor((time - hours) * 60);
            return new TimeSpan((int)hours, (int)minutes, 0);
        }    

        // References:
        // http://www.ummah.net/astronomy/saltime  
        // http://aa.usno.navy.mil/faq/docs/SunApprox.html
        // compute declination angle of sun and equation of time
        private static double[] SunPosition(double jd)
        {
            var D = jd - 2451545.0;
            var g = FixAngle(357.529 + 0.98560028 * D);
            var q = FixAngle(280.459 + 0.98564736 * D);
            var L = FixAngle(q + 1.915 * Sine(g) + 0.020 * Sine(2 * g));

            var R = 1.00014 - 0.01671 * Cosine(g) - 0.00014 * Cosine(2 * g);
            var e = 23.439 - 0.00000036 * D;

            var d = Arcsine(Sine(e) * Sine(L));
            var RA = Arctan(Cosine(e) * Sine(L), Cosine(L)) / 15;
            RA = FixHour(RA);
            var EqT = q / 15 - RA;

            return new double[] { d, EqT };
        }

        // compute equation of time
        private static double EquationOfTime(double jd)
        {
            return SunPosition(jd)[1];
        }

        // compute declination angle of sun
        private static double SunDeclination(double jd)
        {
            return SunPosition(jd)[0];
        }

        // compute mid-day (Dhuhr, Zawal) time
        private static double ComputeMidDay(double jDate, double t)
        {
            var T = EquationOfTime(jDate + t);
            var Z = FixHour(12 - T);
            return Z;
        }

        // compute time for a given angle G
        private static double ComputeTime(double jDate, double G, double t, double lat)
        {
            var D = SunDeclination(jDate + t);
            var Z = ComputeMidDay(jDate, t);
            double V = ((double)(1 / 15d)) * InverseCosine((-Sine(G) - Sine(D) * Sine(lat)) /
                            (Cosine(D) * Cosine(lat)));
            return Z + (G > 90 ? -V : V);
        }

        // compute the time of Asr
        private static double ComputeAsr(AsrJuristicMethods method, double jDate, double t, double lat)  // Shafii: step=1, Hanafi: step=2
        {
            var step = 1;
            if (method == AsrJuristicMethods.Hanafi)
            {
                step = 2;
            }
            var D = SunDeclination(jDate + t);
            var G = -InverseCotan(step + Tan(Math.Abs(lat - D)));
            return ComputeTime(jDate, G, t, lat);
        }

        // compute prayer times at given julian date
        private static double[] ComputeTimes(double jDate, double[] times, CalculatorParams calculatorParams)
        {
            var t = DayPortion(times);

            var Fajr = ComputeTime(jDate, 180 - _methodParams[calculatorParams.CalculationMethod][0], t[0], calculatorParams.Latitude);
            var Sunrise = ComputeTime(jDate, 180 - 0.833, t[1], calculatorParams.Latitude);
            var Dhuhr = ComputeMidDay(jDate, t[2]);
            var Asr = ComputeAsr(calculatorParams.AsrJuristicMethod, jDate, t[3], calculatorParams.Latitude);
            var Sunset = ComputeTime(jDate, 0.833, t[4], calculatorParams.Latitude);
            var Maghrib = ComputeTime(jDate, _methodParams[calculatorParams.CalculationMethod][2], t[5], calculatorParams.Latitude);
            var Isha = ComputeTime(jDate, _methodParams[calculatorParams.CalculationMethod][4], t[6], calculatorParams.Latitude);

            return new double[] { Fajr, Sunrise, Dhuhr, Asr, Sunset, Maghrib, Isha };
        }


        // compute prayer times at given julian date
        private static Times ComputeDayTimes(double jDate, double timeZone, CalculatorParams calculatorParams)
        {
            double[] times = new double[] { 5, 6, 12, 13, 18, 18, 18 }; //default times

            for (var i = 1; i <= NumIterations; i++)
                times = ComputeTimes(jDate, times, calculatorParams);

            times = AdjustTimes(timeZone, times, calculatorParams);
            return AdjustTimesFormat(times);
        }


        // adjust times in a prayer time array
        private static double[] AdjustTimes(double timeZone, double[] times, CalculatorParams calculationParams)
        {
            for (var i = 0; i < 7; i++)
            {
                times[i] += timeZone - calculationParams.Longitude / 15;
            }
            times[2] += DhuhrMinutes / 60; //Dhuhr

            if (_methodParams[calculationParams.CalculationMethod][1] == 1) // Maghrib
            {
                times[5] = times[4] + _methodParams[calculationParams.CalculationMethod][2] / 60;
            }

            if (_methodParams[calculationParams.CalculationMethod][3] == 1) // Isha
            {
                times[6] = times[5] + _methodParams[calculationParams.CalculationMethod][4] / 60;
            }

            if (calculationParams.HighLatitudeAdjustmentMethod != HighLatitudeAdjustmentMethods.None)
            {
                times = AdjustHighLatTimes(times, calculationParams);
            }
            return times;
        }


        /// <summary>
        /// Represents the prayer times for a specific date.
        /// </summary>
        private static Times AdjustTimesFormat(double[] times)
        {
            Times returnData = new Times();

            returnData.Fajr = FloatToTimeSpan(times[0]);
            returnData.Sunrise = FloatToTimeSpan(times[1]);
            returnData.Dhuhr = FloatToTimeSpan(times[2]);
            returnData.Asr = FloatToTimeSpan(times[3]);
            returnData.Sunset = FloatToTimeSpan(times[4]);
            returnData.Maghrib = FloatToTimeSpan(times[5]);
            returnData.Isha = FloatToTimeSpan(times[6]);
            return returnData;
        }


        /// <summary>
        /// Adjusts the prayer times for high latitude locations.
        /// </summary>
        /// <param name="times">The array of prayer times.</param>
        /// <param name="calculationMethod">The calculation method for prayer times.</param>
        /// <returns>The adjusted prayer times for high latitude locations.</returns>
        private static double[] AdjustHighLatTimes(double[] times, CalculatorParams calculatorParams)
        {
            var nightTime = TimeDiff(times[4], times[1]); // sunset to sunrise

            // Adjust Fajr
            var FajrDiff = NightPortion(_methodParams[calculatorParams.CalculationMethod][0], calculatorParams.HighLatitudeAdjustmentMethod) * nightTime;
            if (double.IsNaN(times[0]) || TimeDiff(times[0], times[1]) > FajrDiff)
                times[0] = times[1] - FajrDiff;

            // Adjust Isha
            var IshaAngle = (_methodParams[calculatorParams.CalculationMethod][3] == 0) ? _methodParams[calculatorParams.CalculationMethod][4] : 18;
            var IshaDiff = NightPortion(IshaAngle, calculatorParams.HighLatitudeAdjustmentMethod) * nightTime;
            if (double.IsNaN(times[6]) || TimeDiff(times[4], times[6]) > IshaDiff)
                times[6] = times[4] + IshaDiff;

            // Adjust Maghrib
            var MaghribAngle = (_methodParams[calculatorParams.CalculationMethod][1] == 0) ? _methodParams[calculatorParams.CalculationMethod][2] : 4;
            var MaghribDiff = NightPortion(MaghribAngle, calculatorParams.HighLatitudeAdjustmentMethod) * nightTime;
            if (double.IsNaN(times[5]) || TimeDiff(times[4], times[5]) > MaghribDiff)
                times[5] = times[4] + MaghribDiff;

            return times;
        }

        /// <summary>
        /// Calculates the portion of the night based on the specified angle.
        /// </summary>
        /// <param name="angle">The angle used to calculate the night portion.</param>
        /// <returns>The calculated portion of the night.</returns>
        private static double NightPortion(double angle, HighLatitudeAdjustmentMethods adjustmentMethods)
        {
            if (adjustmentMethods == HighLatitudeAdjustmentMethods.AngleBased)
                return 1 / 60 * angle;
            if (adjustmentMethods == HighLatitudeAdjustmentMethods.MidNight)
                return 1 / 2d;
            if (adjustmentMethods == HighLatitudeAdjustmentMethods.OneSeventh)
                return 1 / 7d;

            return 0;
        }

        
        /// <summary>
        /// Calculates the day portion for each prayer time.
        /// </summary>
        /// <param name="times">An array of prayer times.</param>
        /// <returns>An array of day portions for each prayer time.</returns>
        private static double[] DayPortion(double[] times)
        {
            for (var i = 0; i < 7; i++)
                times[i] /= 24;
            return times;
        }
        
        #region Utility Functions
        /// <summary>
        /// Calculates the time difference between two given times.
        /// </summary>
        /// <param name="time1">The first time.</param>
        /// <param name="time2">The second time.</param>
        /// <returns>The time difference between <paramref name="time1"/> and <paramref name="time2"/>.</returns>
        private static double TimeDiff(double time1, double time2)
        {
            return FixHour(time2 - time1);
        }
       
        
        /// <summary>
        /// Calculates the Julian Date for a given year, month, and day.
        /// </summary>
        /// <param name="year">The year.</param>
        /// <param name="month">The month.</param>
        /// <param name="day">The day.</param>
        /// <returns>The Julian Date.</returns>
        private static double JulianDate(int year, int month, int day)
        {
            double A = Math.Floor((double)(year / 100));
            double B = Math.Floor(A / 4);
            double C = 2 - A + B;
            double Dd = day;
            double Ee = Math.Floor(365.25 * (year + 4716));
            double F = Math.Floor(30.6001 * (month + 1));

            double JD = C + Dd + Ee + F - 1524.5;

            return JD;
        }

        /// <summary>
        /// Calculates the effective time zone offset for a given date and time zone.
        /// If the time zone is null, it uses the offset of the provided date.
        /// Adjusts the offset for daylight saving time if applicable.
        /// </summary>
        /// <param name="date">The date and time for which to calculate the effective time zone.</param>
        /// <param name="timeZone">The time zone offset in hours. If null, the offset of the provided date is used.</param>
        /// <returns>The effective time zone offset in hours.</returns>
        private static double EffectiveTimeZone(DateTimeOffset date, double? timeZone)
        {
            if (timeZone == null)
                timeZone = date.Offset.Hours;

            int dstOffset = 1;
            if (date.LocalDateTime.IsDaylightSavingTime())
            {
                dstOffset = 0;
            }

            return timeZone.Value - dstOffset;
        }

        /// <summary>
        /// Calculates the sine of an angle in degrees.
        /// </summary>
        /// <param name="d">The angle in degrees.</param>
        /// <returns>The sine of the angle.</returns>
        private static double Sine(double d)
        {
            return Math.Sin(DegreeToRadian(d));
        }

        /// <summary>
        /// Calculates the cosine of the specified angle in degrees.
        /// </summary>
        /// <param name="d">The angle in degrees.</param>
        /// <returns>The cosine of the angle.</returns>
        private static double Cosine(double d)
        {
            return Math.Cos(DegreeToRadian(d));
        }

        /// <summary>
        /// Calculates the tangent of an angle in degrees.
        /// </summary>
        /// <param name="d">The angle in degrees.</param>
        /// <returns>The tangent of the angle.</returns>
        private static double Tan(double d)
        {
            return Math.Tan(DegreeToRadian(d));
        }

        /// <summary>
        /// Calculates the arcsine of a number and converts the result to degrees.
        /// </summary>
        /// <param name="x">The number to calculate the arcsine of.</param>
        /// <returns>The arcsine of the specified number in degrees.</returns>
        private static double Arcsine(double x)
        {
            return RadianToDegrees(Math.Asin(x));
        }

        /// <summary>
        /// Calculates the inverse cosine of a specified number and converts the result to degrees.
        /// </summary>
        /// <param name="x">The number to calculate the inverse cosine of.</param>
        /// <returns>The inverse cosine of <paramref name="x"/> in degrees.</returns>
        private static double InverseCosine(double x)
        {
            return RadianToDegrees(Math.Acos(x));
        }

        /// <summary>
        /// Calculates the arctangent of the specified coordinates and returns the result in degrees.
        /// </summary>
        /// <param name="y">The y-coordinate of the point.</param>
        /// <param name="x">The x-coordinate of the point.</param>
        /// <returns>The arctangent of the specified coordinates in degrees.</returns>
        private static double Arctan(double y, double x)
        {
            return RadianToDegrees(Math.Atan2(y, x));
        }

        /// <summary>
        /// Calculates the inverse cotangent of the specified value.
        /// </summary>
        /// <param name="x">The value for which to calculate the inverse cotangent.</param>
        /// <returns>The inverse cotangent of the specified value.</returns>
        private static double InverseCotan(double x)
        {
            return RadianToDegrees(Math.Atan(1 / x));
        }

        /// <summary>
        /// Converts degrees to radians.
        /// </summary>
        /// <param name="d">The angle in degrees.</param>
        /// <returns>The angle in radians.</returns>
        private static double DegreeToRadian(double d)
        {
            return (d * Math.PI) / 180.0;
        }

        /// <summary>
        /// Converts an angle from radians to degrees.
        /// </summary>
        /// <param name="r">The angle in radians.</param>
        /// <returns>The angle in degrees.</returns>
        private static double RadianToDegrees(double r)
        {
            return (r * 180.0) / Math.PI;
        }

        /// <summary>
        /// Fixes the angle to be within the range of 0 to 360 degrees.
        /// </summary>
        /// <param name="a">The angle to be fixed.</param>
        /// <returns>The fixed angle within the range of 0 to 360 degrees.</returns>
        private static double FixAngle(double a)
        {
            a = a - 360.0 * (Math.Floor(a / 360.0));
            a = a < 0 ? a + 360.0 : a;
            return a;
        }

        /// <summary>
        /// Fixes the hour value to be within the range of 0 to 24.
        /// </summary>
        /// <param name="a">The hour value to be fixed.</param>
        /// <returns>The fixed hour value.</returns>
        private static double FixHour(double a)
        {
            a = a - 24.0 * (Math.Floor(a / 24.0));
            a = a < 0 ? a + 24.0 : a;
            return a;
        }
        #endregion
    }
}