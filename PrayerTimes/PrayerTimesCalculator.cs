// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.
//
// This is a port of PrayerTimes calculator by Jameel Haffejee 
// Original  version here : http://tanzil.info/praytime/doc/manual/

using System;
using System.Collections.Generic;

namespace PrayerTimes
{
    /// <summary>
    /// Prayer times calculator.
    /// </summary>
    public class PrayerTimesCalculator
    {
        const int NumIterations = 1;    // number of iterations needed to compute times, this should never be more than 1;
        const int dhuhrMinutes = 0;     // minutes after mid-day for Dhuhr
        /// <summary>
        ///  methodParams[methodNum] = new Array(fa, ms, mv, is, iv);   
        ///     fa : fajr angle
        ///     ms : maghrib selector (0 = angle; 1 = minutes after sunset)
        ///     mv : maghrib parameter value (in angle or minutes)
        ///     is : isha selector (0 = angle; 1 = minutes after maghrib)
        ///     iv : isha parameter value (in angle or minutes)
        /// </summary>
        private readonly Dictionary<CalculationMethods, double[]> _methodParams = new Dictionary<CalculationMethods, double[]> {
            { CalculationMethods.Jafari, new double[] { 16, 0, 4, 0, 14 } },
            { CalculationMethods.Karachi,new double[]{18, 1, 0, 0, 18} },
            { CalculationMethods.ISNA,new double[]{15, 1, 0, 0, 15} },
            { CalculationMethods.MWL,new double[]{18, 1, 0, 0, 17} },
            { CalculationMethods.Makkah,new double[]{19, 1, 0, 1, 90} },
            { CalculationMethods.Egypt,new double[]{19.5, 1, 0, 0, 17.5} },
            { CalculationMethods.Custom,new double[]{18, 1, 0, 0, 17} }};

        private readonly double _latitude;        // latitude 
        private readonly double _longitude;        // longitude 

        /// <summary>
        /// Initializes a new instance of PrayerTimesCalculator.
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        public PrayerTimesCalculator(double latitude, double longitude)
        {
            _latitude = latitude;
            _longitude = longitude;
        }

        /// <summary>
        /// Gets or sets calculation method.
        /// </summary>
        public CalculationMethods CalculationMethod
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets juristic method for Asr.
        /// </summary>
        public AsrJuristicMethods AsrJurusticMethod
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets adjustment method for higher latitudes.
        /// </summary>
        public HighLatitudeAdjustmentMethods HighLatitudeAdjustmentMethod
        {
            get; set;
        }

        ///<summary>
        /// Returns the prayer times for a given date , the date format is specified as individual settings.
        /// </summary>
        /// <param name="date">Date time representing the date for which times should be calculated.</param>        
        /// <param name="timeZone">Time zone to use when calculating times. If omitted, time zone from date is used.</param>
        /// <returns>
        /// Times structure containing the Salaah times.
        /// </returns>
        public Times GetPrayerTimes(DateTimeOffset date, int? timeZone = null)
        {
            timeZone = EffectiveTimeZone(date, timeZone);
            var jDate = JulianDate(date.Year, date.Month, date.Day) - _longitude / (15 * 24);
            var times = ComputeDayTimes(jDate, timeZone.Value);
            times.Date = date;
            return times;
        }
       
        // convert float hours to 24h format
        private TimeSpan FloatToTimeSpan(double time)
        {
            time = this.fixhour(time + 0.5 / 60);  // add 0.5 minutes to round
            var hours = Math.Floor(time);
            var minutes = Math.Floor((time - hours) * 60);
            return new TimeSpan((int)hours, (int)minutes, 0);
        }    

        // References:
        // http://www.ummah.net/astronomy/saltime  
        // http://aa.usno.navy.mil/faq/docs/SunApprox.html
        // compute declination angle of sun and equation of time
        private double[] SunPosition(double jd)
        {
            var D = jd - 2451545.0;
            var g = this.fixangle(357.529 + 0.98560028 * D);
            var q = this.fixangle(280.459 + 0.98564736 * D);
            var L = this.fixangle(q + 1.915 * this.dsin(g) + 0.020 * this.dsin(2 * g));

            var R = 1.00014 - 0.01671 * this.dcos(g) - 0.00014 * this.dcos(2 * g);
            var e = 23.439 - 0.00000036 * D;

            var d = this.darcsin(this.dsin(e) * this.dsin(L));
            var RA = this.darctan2(this.dcos(e) * this.dsin(L), this.dcos(L)) / 15;
            RA = this.fixhour(RA);
            var EqT = q / 15 - RA;

            return new double[] { d, EqT };
        }

        // compute equation of time
        private double EquationOfTime(double jd)
        {
            return this.SunPosition(jd)[1];
        }

        // compute declination angle of sun
        private double SunDeclination(double jd)
        {
            return this.SunPosition(jd)[0];
        }

        // compute mid-day (Dhuhr, Zawal) time
        private double ComputeMidDay(double jDate, double t)
        {
            var T = this.EquationOfTime(jDate + t);
            var Z = this.fixhour(12 - T);
            return Z;
        }

        // compute time for a given angle G
        private double ComputeTime(double jDate, double G, double t)
        {
            var D = this.SunDeclination(jDate + t);
            var Z = this.ComputeMidDay(jDate, t);
            double V = ((double)(1 / 15d)) * this.darccos((-this.dsin(G) - this.dsin(D) * this.dsin(this._latitude)) /
                            (this.dcos(D) * this.dcos(this._latitude)));
            return Z + (G > 90 ? -V : V);
        }

        // compute the time of Asr
        private double ComputeAsr(AsrJuristicMethods method, double jDate, double t)  // Shafii: step=1, Hanafi: step=2
        {
            var step = 1;
            if (method == AsrJuristicMethods.Hanafi)
            {
                step = 2;
            }
            var D = this.SunDeclination(jDate + t);
            var G = -this.darccot(step + this.dtan(Math.Abs(this._latitude - D)));
            return this.ComputeTime(jDate, G, t);
        }

        // compute prayer times at given julian date
        private double[] ComputeTimes(double jDate, double[] times)
        {
            var t = this.DayPortion(times);

            var Fajr = this.ComputeTime(jDate, 180 - this._methodParams[this.CalculationMethod][0], t[0]);
            var Sunrise = this.ComputeTime(jDate, 180 - 0.833, t[1]);
            var Dhuhr = this.ComputeMidDay(jDate, t[2]);
            var Asr = this.ComputeAsr(this.AsrJurusticMethod, jDate, t[3]);
            var Sunset = this.ComputeTime(jDate, 0.833, t[4]); ;
            var Maghrib = this.ComputeTime(jDate, this._methodParams[this.CalculationMethod][2], t[5]);
            var Isha = this.ComputeTime(jDate, this._methodParams[this.CalculationMethod][4], t[6]);

            return new double[] { Fajr, Sunrise, Dhuhr, Asr, Sunset, Maghrib, Isha };
        }


        // compute prayer times at given julian date
        private Times ComputeDayTimes(double jDate, int timeZone)
        {
            double[] times = new double[] { 5, 6, 12, 13, 18, 18, 18 }; //default times

            for (var i = 1; i <= NumIterations; i++)
                times = this.ComputeTimes(jDate, times);

            times = this.AdjustTimes(timeZone, times);
            return this.AdjustTimesFormat(times);
        }


        // adjust times in a prayer time array
        private double[] AdjustTimes(int timeZone, double[] times)
        {
            for (var i = 0; i < 7; i++)
            {
                times[i] += timeZone - this._longitude / 15;
            }
            times[2] += dhuhrMinutes / 60; //Dhuhr

            if (this._methodParams[this.CalculationMethod][1] == 1) // Maghrib
            {
                times[5] = times[4] + this._methodParams[this.CalculationMethod][2] / 60;
            }

            if (this._methodParams[this.CalculationMethod][3] == 1) // Isha
            {
                times[6] = times[5] + this._methodParams[this.CalculationMethod][4] / 60;
            }

            if (this.HighLatitudeAdjustmentMethod != HighLatitudeAdjustmentMethods.None)
            {
                times = this.AdjustHighLatTimes(times);
            }
            return times;
        }


        // convert times array to given time format
        private Times AdjustTimesFormat(double[] times)
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


        // adjust Fajr, Isha and Maghrib for locations in higher latitudes
        private double[] AdjustHighLatTimes(double[] times)
        {
            var nightTime = this.TimeDiff(times[4], times[1]); // sunset to sunrise

            // Adjust Fajr
            var FajrDiff = this.NightPortion(_methodParams[this.CalculationMethod][0]) * nightTime;
            if (double.IsNaN(times[0]) || this.TimeDiff(times[0], times[1]) > FajrDiff)
                times[0] = times[1] - FajrDiff;

            // Adjust Isha
            var IshaAngle = (this._methodParams[this.CalculationMethod][3] == 0) ? this._methodParams[this.CalculationMethod][4] : 18;
            var IshaDiff = this.NightPortion(IshaAngle) * nightTime;
            if (double.IsNaN(times[6]) || this.TimeDiff(times[4], times[6]) > IshaDiff)
                times[6] = times[4] + IshaDiff;

            // Adjust Maghrib
            var MaghribAngle = (this._methodParams[this.CalculationMethod][1] == 0) ? this._methodParams[this.CalculationMethod][2] : 4;
            var MaghribDiff = this.NightPortion(MaghribAngle) * nightTime;
            if (double.IsNaN(times[5]) || this.TimeDiff(times[4], times[5]) > MaghribDiff)
                times[5] = times[4] + MaghribDiff;

            return times;
        }

        // the night portion used for adjusting times in higher latitudes
        private double NightPortion(double angle)
        {
            if (this.HighLatitudeAdjustmentMethod == HighLatitudeAdjustmentMethods.AngleBased)
                return 1 / 60 * angle;
            if (this.HighLatitudeAdjustmentMethod == HighLatitudeAdjustmentMethods.MidNight)
                return 1 / 2d;
            if (this.HighLatitudeAdjustmentMethod == HighLatitudeAdjustmentMethods.OneSeventh)
                return 1 / 7d;

            return 0;
        }

        // convert hours to day portions 
        private double[] DayPortion(double[] times)
        {
            for (var i = 0; i < 7; i++)
                times[i] /= 24;
            return times;
        }
        
        #region Utility Functions
        private double TimeDiff(double time1, double time2)
        {
            return this.fixhour(time2 - time1);
        }
       
        // calculate julian date from a calendar date
        private double JulianDate(int year, int month, int day)
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

        // return effective timezone for a given date
        private int EffectiveTimeZone(DateTimeOffset date, int? timeZone)
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

        // degree sin
        private double dsin(double d)
        {
            return Math.Sin(this.dtr(d));
        }

        // degree cos
        private double dcos(double d)
        {
            return Math.Cos(this.dtr(d));
        }

        // degree tan
        private double dtan(double d)
        {
            return Math.Tan(this.dtr(d));
        }

        // degree arcsin
        private double darcsin(double x)
        {
            return this.rtd(Math.Asin(x));
        }

        // degree arccos
        private double darccos(double x)
        {
            return this.rtd(Math.Acos(x));
        }

        // degree arctan
        private double darctan(double x)
        {
            return this.rtd(Math.Atan(x));
        }

        // degree arctan2
        private double darctan2(double y, double x)
        {
            return this.rtd(Math.Atan2(y, x));
        }

        // degree arccot
        private double darccot(double x)
        {
            return this.rtd(Math.Atan(1 / x));
        }

        // degree to radian
        private double dtr(double d)
        {
            return (d * Math.PI) / 180.0;
        }

        // radian to degree
        private double rtd(double r)
        {
            return (r * 180.0) / Math.PI;
        }

        // range reduce angle in degrees.
        private double fixangle(double a)
        {
            a = a - 360.0 * (Math.Floor(a / 360.0));
            a = a < 0 ? a + 360.0 : a;
            return a;
        }

        // range reduce hours to 0..23
        private double fixhour(double a)
        {
            a = a - 24.0 * (Math.Floor(a / 24.0));
            a = a < 0 ? a + 24.0 : a;
            return a;
        }
        #endregion
    }
}