namespace PrayerTimes.Api
{
    public record struct PrayerTimesResponse(
        DateTimeOffset Date, 
        TimeSpan Fajr, 
        TimeSpan Sunrise, 
        TimeSpan Dhuhr, 
        TimeSpan Asr, 
        TimeSpan Sunset, 
        TimeSpan Maghrib, 
        TimeSpan Isha)
    {
        public static PrayerTimesResponse FromTimes(Times times)
            => new PrayerTimesResponse(times.Date, times.Fajr, 
            times.Sunrise, times.Dhuhr, times.Asr, times.Sunset, 
            times.Maghrib, times.Isha);
    }

    public record struct PrayerTimesResponseRange(IList<PrayerTimesResponse> PrayerTimes)
    {
        public static PrayerTimesResponseRange FromTimes(IEnumerable<Times> times) 
            => new PrayerTimesResponseRange(
                times.Select(t => PrayerTimesResponse.FromTimes(t)).ToList());
        
    }
}