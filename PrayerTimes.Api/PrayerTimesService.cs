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
    }
}