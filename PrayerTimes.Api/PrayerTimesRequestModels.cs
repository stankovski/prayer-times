using System.Text.Json.Serialization;
using Batoulapps.Adhan;
using Microsoft.AspNetCore.Mvc;

namespace PrayerTimes.Api
{
    public record PrayerTimesRequest(
        [FromBody]
        double TimeZone,
        [FromBody]
        double Latitude, 
        [FromBody]
        double Longitude, 
        [FromBody]
        CalculationMethod CalculationMethod, 
        [FromBody]
        Madhab AsrJuristicMethod, 
        [FromBody]
        HighLatitudeRule HighLatitudeAdjustmentMethod);

    public record PrayerTimesRequestForRange(
        [FromBody]
        DateTimeOffset FromDate,
        [FromBody]
        DateTimeOffset ToDate,
        [FromBody]
        double TimeZone,
        [FromBody]
        double Latitude, 
        [FromBody]
        double Longitude, 
        [FromBody]
        CalculationMethod CalculationMethod, 
        [FromBody]
        Madhab AsrJuristicMethod, 
        [FromBody]
        HighLatitudeRule HighLatitudeAdjustmentMethod);
}