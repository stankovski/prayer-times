using Batoulapps.Adhan;
using Batoulapps.Adhan.Internal;
using Microsoft.AspNetCore.Mvc;

namespace PrayerTimes.Api
{
    public class PrayerTimesService : IPrayerTimesService
    {
        /// <summary>
        /// Retrieves the prayer times based on the provided request.
        /// </summary>
        /// <param name="request">The request containing the necessary information for retrieving the prayer times.</param>
        /// <returns>A response containing the prayer times.</returns>
        public PrayerTimesResponse GetPrayerTimes(DateTime date, PrayerTimesRequest request) {
            var times = PrayerTimesCalculator.GetPrayerTimes(date, 
                new CalculatorParams(request.Latitude, request.Longitude, 
                    request.CalculationMethod, 
                    request.AsrJuristicMethod, 
                    request.HighLatitudeAdjustmentMethod), request.TimeZone);
            return PrayerTimesResponse.FromTimes(times);
        }

        public PrayerTimesResponseRange GetPrayerTimes(PrayerTimesRequestForRange request)
        {
            if (request.FromDate > request.ToDate)
                throw new ArgumentException("The 'from' date must be before the 'to' date.");

            if (request.ToDate.Subtract(request.FromDate).Days > 365)
                throw new ArgumentException("The date range must not exceed 365 days.");

            if (request.FromDate < new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero))
                throw new ArgumentException("The 'from' date must be after 1980.");

            var calcParams = new CalculatorParams(request.Latitude, request.Longitude, 
                request.CalculationMethod, 
                request.AsrJuristicMethod, 
                request.HighLatitudeAdjustmentMethod);
            var start = request.FromDate;
            var end = request.ToDate;
            var times = Enumerable.Range(0, 1 + end.Subtract(start).Days)
                .Select(offset => start.AddDays(offset))
                .Select(date => PrayerTimesCalculator.GetPrayerTimes(date, calcParams, request.TimeZone));
        
            return PrayerTimesResponseRange.FromTimes(times);
        }

        public static Times GetPrayerTimes(DateTimeOffset date, CalculatorParams calculatorParams, 
            double? timeZone = null)
        {
            Coordinates coordinates = new Coordinates(calculatorParams.Latitude, calculatorParams.Longitude);
            DateComponents dateComponents = DateComponents.From(date.Date);
            CalculationParameters parameters = CalculationMethod.NORTH_AMERICA.GetParameters();

            var timeZoneInfo = TimeZoneInfo.Local;

            var prayerTimes = new Batoulapps.Adhan.PrayerTimes(coordinates, dateComponents, parameters);
            var times = new Times
            {
                Fajr = prayerTimes.Fajr.TimeOfDay,
                Sunrise = prayerTimes.Sunrise.TimeOfDay,
                Dhuhr = prayerTimes.Dhuhr.TimeOfDay,
                Asr = prayerTimes.Asr.TimeOfDay,
                Maghrib = prayerTimes.Maghrib.TimeOfDay,
                Isha = prayerTimes.Isha.TimeOfDay
            };
            Console.WriteLine("Fajr   : " + TimeZoneInfo.ConvertTimeFromUtc(prayerTimes.Fajr, timeZoneInfo));
            Console.WriteLine("Sunrise: " + TimeZoneInfo.ConvertTimeFromUtc(prayerTimes.Sunrise, timeZoneInfo));
            Console.WriteLine("Dhuhr  : " + TimeZoneInfo.ConvertTimeFromUtc(prayerTimes.Dhuhr, timeZoneInfo));
            Console.WriteLine("Asr    : " + TimeZoneInfo.ConvertTimeFromUtc(prayerTimes.Asr, timeZoneInfo));
            Console.WriteLine("Maghrib: " + TimeZoneInfo.ConvertTimeFromUtc(prayerTimes.Maghrib, timeZoneInfo));
            Console.WriteLine("Isha   : " + TimeZoneInfo.ConvertTimeFromUtc(prayerTimes.Isha, timeZoneInfo));
            return times;
        }
    }
}