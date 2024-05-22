using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace PrayerTimes.Api.Controllers
{
    [ApiController]
    [Route("api/prayer-times")]
    [Produces("application/json")]
    [TokenAuthenticationRequired]
    public class PrayerTimesController : ControllerBase
    {
        private readonly IPrayerTimesService _prayerTimesService;

        public PrayerTimesController(IPrayerTimesService prayerTimesService)
        {
            _prayerTimesService = prayerTimesService;
        }

        [HttpPost]
        [Route("{date}")]
        public ActionResult<PrayerTimesResponse> GetPrayerTimes(
            [FromRoute] DateTime date,
            [FromBody]
            PrayerTimesRequest request,
            [FromQuery, Required] string apiVersion)
        {
            var prayerTimes = _prayerTimesService.GetPrayerTimes(date, request);
            return Ok(prayerTimes);
        }

        [HttpPost]
        [Route("range")]
        public ActionResult<PrayerTimesResponseRange> GetPrayerTimesForDateRange(
            [FromBody]
            PrayerTimesRequestForRange request,
            [FromQuery, Required] string apiVersion)
        {
            var prayerTimes = _prayerTimesService.GetPrayerTimes(request);
            return Ok(prayerTimes);
        }
    }
}