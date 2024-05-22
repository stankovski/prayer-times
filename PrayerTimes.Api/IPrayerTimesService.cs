namespace PrayerTimes.Api
{
    /// <summary>
    /// Represents a service for retrieving prayer times.
    /// </summary>
    public interface IPrayerTimesService
    {
        /// <summary>
        /// Retrieves the prayer times based on the provided request.
        /// </summary>
        /// <param name="request">The request containing the necessary information for retrieving the prayer times.</param>
        /// <returns>A response containing the prayer times.</returns>
        PrayerTimesResponse GetPrayerTimes(DateTime date, PrayerTimesRequest request);
        PrayerTimesResponseRange GetPrayerTimes(PrayerTimesRequestForRange request);
    }
}