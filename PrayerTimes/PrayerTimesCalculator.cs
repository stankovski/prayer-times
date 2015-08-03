// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.
//
// This is a port of PrayerTimes calculator by Jameel Haffejee 
// Original  version here : http://tanzil.info/praytime/doc/manual/

using System;
using System.Collections.Generic;

namespace PrayerTimes
{
    public class PrayerTimesCalculator
    {
        #region Constants
        // Adjusting Methods for Higher Latitudes
        int None = 0;    // No adjustment
        int MidNight = 1;    // middle of night
        int OneSeventh = 2;    // 1/7th of night
        int AngleBased = 3;    // angle/60th of night


        // Time Formats
        int Time24 = 0;    // 24-hour format
        int Time12 = 1;    // 12-hour format
        int Time12NS = 2;    // 12-hour format with no suffix
        int Float = 3;    // floating point number 

        // Time Names
        string[] timeNames = new string[]{
                "Fajr",
                "Sunrise",
                "Dhuhr",
                "Asr",
                "Sunset",
                "Maghrib",
                "Isha"
        };

        #endregion

        #region Global Variables
        int dhuhrMinutes = 0;           // minutes after mid-day for Dhuhr
        int adjustHighLats = 1;         // adjusting method for higher latitudes

        int timeFormat = 0;             // time format

        double lat;        // latitude 
        double lng;        // longitude 
        int timeZone;   // time-zone 
        double jDate;      // Julian date
        #endregion

        #region Technical Settings
        int numIterations = 1;          // number of iterations needed to compute times, this should never be more than 1;
        #endregion

        /// <summary>
        /// Calculation method.
        /// </summary>
        public CalculationMethods CalculationMethod
        {
            get; set;
        }

        /// <summary>
        /// Juristic method for Asr.
        /// </summary>
        public AsrJuristicMethods AsrJurusticMethod
        {
            get; set;
        }

        ///<summary>
        /// Returns the prayer times for a given date , the date format is specified as individual settings.
        /// </summary>
        /// <param name="year">Year to use when calculating times</param>        
        /// <param name="month">Month to use when calculating times</param>
        /// <param name="day">Day to use when calculating times</param>
        /// <param name="latitude">Latitude to use when calculating times</param>
        /// <param name="longitude">Longitude to use when calculating times</param>
        /// <param name="timeZone">Time zone to use when calculating times</param>
        /// <returns>
        /// A string Array containing the Salaah times,The time is in the 24 hour format.
        /// The array is structured as such.
        /// 0.Fajr
        /// 1.Sunrise
        /// 2.Dhuhr
        /// 3.Asr
        /// 4.Sunset
        /// 5.Maghrib
        /// 6.Isha
        /// </returns>
        public Times GetPrayerTimes(DateTimeOffset date, double latitude, double longitude, int? timeZone = null)
        {
            lat = latitude;
            lng = longitude;
            this.timeZone = effectiveTimeZone(date, timeZone);
            jDate = julianDate(date.Year, date.Month, date.Day) - longitude / (15 * 24);
            var times = computeDayTimes();
            times.Date = date;
            return times;
        }

        /// <summary>
        ///  methodParams[methodNum] = new Array(fa, ms, mv, is, iv);   
        ///     fa : fajr angle
        ///     ms : maghrib selector (0 = angle; 1 = minutes after sunset)
        ///     mv : maghrib parameter value (in angle or minutes)
        ///     is : isha selector (0 = angle; 1 = minutes after maghrib)
        ///     iv : isha parameter value (in angle or minutes)
        /// </summary>
        private readonly Dictionary<CalculationMethods, double[]> methodParams = new Dictionary<CalculationMethods, double[]> {
            { CalculationMethods.Jafari, new double[] { 16, 0, 4, 0, 14 } },
            { CalculationMethods.Karachi,new double[]{18, 1, 0, 0, 18} },
            { CalculationMethods.ISNA,new double[]{15, 1, 0, 0, 15} },
            { CalculationMethods.MWL,new double[]{18, 1, 0, 0, 17} },
            { CalculationMethods.Makkah,new double[]{19, 1, 0, 1, 90} },
            { CalculationMethods.Egypt,new double[]{19.5, 1, 0, 0, 17.5} },
            { CalculationMethods.Custom,new double[]{18, 1, 0, 0, 17} }};
        
        // set the angle for calculating Fajr
        private void setFajrAngle(double angle)
        {
            this.setCustomParams(new double?[] { angle, null, null, null, null });
        }

        // set the angle for calculating Maghrib
        private void setMaghribAngle(double angle)
        {
            this.setCustomParams(new double?[] { null, 0, angle, null, null });
        }

        // set the angle for calculating Isha
        private void setIshaAngle(double angle)
        {
            this.setCustomParams(new double?[] { null, null, null, 0, angle });
        }


        // set the minutes after mid-day for calculating Dhuhr
        private void setDhuhrMinutes(int minutes)
        {
            this.dhuhrMinutes = minutes;
        }

        // set the minutes after Sunset for calculating Maghrib
        private void setMaghribMinutes(int minutes)
        {
            this.setCustomParams(new double?[] { null, 1, minutes, null, null });
        }

        // set the minutes after Maghrib for calculating Isha
        private void setIshaMinutes(int minutes)
        {
            this.setCustomParams(new double?[] { null, null, null, 1, minutes });
        }

        // set custom values for calculation parameters
        private void setCustomParams(double?[] userParams)
        {
            for (var i = 0; i < 5; i++)
            {
                if (userParams[i] == null)
                    this.methodParams[CalculationMethods.Custom][i] = this.methodParams[this.CalculationMethod][i];
                else
                    this.methodParams[CalculationMethods.Custom][i] = userParams[i].Value;
            }
            this.CalculationMethod = CalculationMethods.Custom;
        }

        // convert float hours to 24h format
        private TimeSpan floatToTimeSpan(double time)
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
        private double[] sunPosition(double jd)
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
        private double equationOfTime(double jd)
        {
            return this.sunPosition(jd)[1];
        }

        // compute declination angle of sun
        private double sunDeclination(double jd)
        {
            return this.sunPosition(jd)[0];
        }

        // compute mid-day (Dhuhr, Zawal) time
        private double computeMidDay(double t)
        {
            var T = this.equationOfTime(this.jDate + t);
            var Z = this.fixhour(12 - T);
            return Z;
        }

        // compute time for a given angle G
        private double computeTime(double G, double t)
        {
            var D = this.sunDeclination(this.jDate + t);
            var Z = this.computeMidDay(t);
            double V = ((double)(1 / 15d)) * this.darccos((-this.dsin(G) - this.dsin(D) * this.dsin(this.lat)) /
                            (this.dcos(D) * this.dcos(this.lat)));
            return Z + (G > 90 ? -V : V);
        }

        // compute the time of Asr
        private double computeAsr(AsrJuristicMethods method, double t)  // Shafii: step=1, Hanafi: step=2
        {
            var step = 1;
            if (method == AsrJuristicMethods.Hanafi)
            {
                step = 2;
            }
            var D = this.sunDeclination(this.jDate + t);
            var G = -this.darccot(step + this.dtan(Math.Abs(this.lat - D)));
            return this.computeTime(G, t);
        }

        // compute prayer times at given julian date
        private double[] computeTimes(double[] times)
        {
            var t = this.dayPortion(times);

            var Fajr = this.computeTime(180 - this.methodParams[this.CalculationMethod][0], t[0]);
            var Sunrise = this.computeTime(180 - 0.833, t[1]);
            var Dhuhr = this.computeMidDay(t[2]);
            var Asr = this.computeAsr(this.AsrJurusticMethod, t[3]);
            var Sunset = this.computeTime(0.833, t[4]); ;
            var Maghrib = this.computeTime(this.methodParams[this.CalculationMethod][2], t[5]);
            var Isha = this.computeTime(this.methodParams[this.CalculationMethod][4], t[6]);

            return new double[] { Fajr, Sunrise, Dhuhr, Asr, Sunset, Maghrib, Isha };
        }


        // compute prayer times at given julian date
        private Times computeDayTimes()
        {
            double[] times = new double[] { 5, 6, 12, 13, 18, 18, 18 }; //default times

            for (var i = 1; i <= this.numIterations; i++)
                times = this.computeTimes(times);

            times = this.adjustTimes(times);
            return this.adjustTimesFormat(times);
        }


        // adjust times in a prayer time array
        private double[] adjustTimes(double[] times)
        {
            for (var i = 0; i < 7; i++)
                times[i] += this.timeZone - this.lng / 15;
            times[2] += this.dhuhrMinutes / 60; //Dhuhr
            if (this.methodParams[this.CalculationMethod][1] == 1) // Maghrib
                times[5] = times[4] + this.methodParams[this.CalculationMethod][2] / 60;
            if (this.methodParams[this.CalculationMethod][3] == 1) // Isha
                times[6] = times[5] + this.methodParams[this.CalculationMethod][4] / 60;

            if (this.adjustHighLats != this.None)
                times = this.adjustHighLatTimes(times);
            return times;
        }


        // convert times array to given time format
        private Times adjustTimesFormat(double[] times)
        {
            Times returnData = new Times();

            if (this.timeFormat == this.Float)
                return returnData;
            returnData.Fajr = floatToTimeSpan(times[0]);
            returnData.Sunrise = floatToTimeSpan(times[1]);
            returnData.Dhuhr = floatToTimeSpan(times[2]);
            returnData.Asr = floatToTimeSpan(times[3]);
            returnData.Sunset = floatToTimeSpan(times[4]);
            returnData.Maghrib = floatToTimeSpan(times[5]);
            returnData.Isha = floatToTimeSpan(times[6]);
            return returnData;
        }


        // adjust Fajr, Isha and Maghrib for locations in higher latitudes
        private double[] adjustHighLatTimes(double[] times)
        {
            var nightTime = this.timeDiff(times[4], times[1]); // sunset to sunrise

            // Adjust Fajr
            var FajrDiff = this.nightPortion(methodParams[this.CalculationMethod][0]) * nightTime;
            if (double.IsNaN(times[0]) || this.timeDiff(times[0], times[1]) > FajrDiff)
                times[0] = times[1] - FajrDiff;

            // Adjust Isha
            var IshaAngle = (this.methodParams[this.CalculationMethod][3] == 0) ? this.methodParams[this.CalculationMethod][4] : 18;
            var IshaDiff = this.nightPortion(IshaAngle) * nightTime;
            if (double.IsNaN(times[6]) || this.timeDiff(times[4], times[6]) > IshaDiff)
                times[6] = times[4] + IshaDiff;

            // Adjust Maghrib
            var MaghribAngle = (this.methodParams[this.CalculationMethod][1] == 0) ? this.methodParams[this.CalculationMethod][2] : 4;
            var MaghribDiff = this.nightPortion(MaghribAngle) * nightTime;
            if (double.IsNaN(times[5]) || this.timeDiff(times[4], times[5]) > MaghribDiff)
                times[5] = times[4] + MaghribDiff;

            return times;
        }


        // the night portion used for adjusting times in higher latitudes
        private double nightPortion(double angle)
        {
            if (this.adjustHighLats == this.AngleBased)
                return 1 / 60 * angle;
            if (this.adjustHighLats == this.MidNight)
                return 1 / 2d;
            if (this.adjustHighLats == this.OneSeventh)
                return 1 / 7d;

            return 0;
        }


        // convert hours to day portions 
        private double[] dayPortion(double[] times)
        {
            for (var i = 0; i < 7; i++)
                times[i] /= 24;
            return times;
        }
        
        #region Misc Functions
        // compute the difference between two times 
        private double timeDiff(int time1, int time2)
        {
            return this.fixhour(time2 - time1);
        }
        private double timeDiff(double time1, double time2)
        {
            return this.fixhour(time2 - time1);
        }

        // add a leading 0 if necessary
        private string twoDigitsFormat(int num)
        {
            return (num < 10) ? "0" + num.ToString() : num.ToString();
        }
        
        // calculate julian date from a calendar date
        private double julianDate(int year, int month, int day)
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


        // convert a calendar date to julian date (second method)
        private double calcJD(int year, int month, int day)
        {
            var J1970 = 2440588.0;
            TimeSpan TS = new TimeSpan(year, month - 1, day);
            var ms = TS.TotalMilliseconds;   // # of milliseconds since midnight Jan 1, 1970
            var days = Math.Floor((double)ms / (1000 * 60 * 60 * 24));
            return J1970 + days - 0.5;
        }

        // return effective timezone for a given date
        private int effectiveTimeZone(DateTimeOffset date, int? timeZone)
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