using Batoulapps.Adhan.Internal;

namespace PrayerTimes.Api
{
    public record struct PrayerTimesResponse(
        DateTimeOffset Date, 
        DateTime Fajr, 
        DateTime Sunrise, 
        DateTime Dhuhr, 
        DateTime Asr, 
        DateTime Maghrib, 
        DateTime Isha)
    {
        public static PrayerTimesResponse FromTimes(DateTimeOffset date, Batoulapps.Adhan.PrayerTimes times)
            => new PrayerTimesResponse(date, times.Fajr, 
            times.Sunrise, times.Dhuhr, times.Asr,
            times.Maghrib, times.Isha);
    }

    public record struct PrayerTimesResponseRange(IList<PrayerTimesResponse> PrayerTimes)
    {
        public static PrayerTimesResponseRange FromTimes(IEnumerable<(DateTimeOffset, Batoulapps.Adhan.PrayerTimes)> times)
            => new PrayerTimesResponseRange(
                times.Select(t => PrayerTimesResponse.FromTimes(t.Item1, t.Item2)).ToList());
        
    }
}