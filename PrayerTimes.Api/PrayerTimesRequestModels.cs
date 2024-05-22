using System.Text.Json.Serialization;
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
        CalculationMethods CalculationMethod, 
        [FromBody]
        AsrJuristicMethods AsrJuristicMethod, 
        [FromBody]
        HighLatitudeAdjustmentMethods HighLatitudeAdjustmentMethod = HighLatitudeAdjustmentMethods.None);

    public record PrayerTimesRequestForRange(
        [FromBody]
        DateTime FromDate,
        [FromBody]
        DateTime ToDate,
        [FromBody]
        double TimeZone,
        [FromBody]
        double Latitude, 
        [FromBody]
        double Longitude, 
        [FromBody]
        CalculationMethods CalculationMethod, 
        [FromBody]
        AsrJuristicMethods AsrJuristicMethod, 
        [FromBody]
        HighLatitudeAdjustmentMethods HighLatitudeAdjustmentMethod = HighLatitudeAdjustmentMethods.None);
}