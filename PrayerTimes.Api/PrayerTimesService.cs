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
            var calcParams = request.CalculationMethod.GetParameters();
            calcParams.Madhab = request.AsrJuristicMethod;
            calcParams.HighLatitudeRule = request.HighLatitudeAdjustmentMethod;
            var dateTime = new DateTimeOffset(date, TimeSpan.FromHours(request.TimeZone));
            var times = GetPrayerTimes(date, calcParams, request.Latitude, request.Longitude);
            return PrayerTimesResponse.FromTimes(dateTime, times.Item2);
        }

        public PrayerTimesResponseRange GetPrayerTimes(PrayerTimesRequestForRange request)
        {
            if (request.FromDate > request.ToDate)
                throw new ArgumentException("The 'from' date must be before the 'to' date.");

            if (request.ToDate.Subtract(request.FromDate).Days > 365)
                throw new ArgumentException("The date range must not exceed 365 days.");

            if (request.FromDate < new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero))
                throw new ArgumentException("The 'from' date must be after 1980.");

            var calcParams = request.CalculationMethod.GetParameters();
            calcParams.Madhab = request.AsrJuristicMethod;
            calcParams.HighLatitudeRule = request.HighLatitudeAdjustmentMethod;
            var start = request.FromDate;
            var end = request.ToDate;
            var times = Enumerable.Range(0, 1 + end.Subtract(start).Days)
                .Select(offset => start.AddDays(offset))
                .Select(date => GetPrayerTimes(date, calcParams, request.Latitude, request.Longitude));
        
            return PrayerTimesResponseRange.FromTimes(times);
        }

        public static (DateTimeOffset, Batoulapps.Adhan.PrayerTimes) GetPrayerTimes(DateTimeOffset date, CalculationParameters calculatorParams, double latitude, double longitude)
        {
            Coordinates coordinates = new Coordinates(latitude, longitude);
            DateComponents dateComponents = DateComponents.From(date.Date);
            CalculationParameters parameters = calculatorParams;
            return (date, new Batoulapps.Adhan.PrayerTimes(coordinates, dateComponents, parameters));
        }
    }
}