using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PrayerTimes.Api
{
    public class TokenAuthenticationRequiredAttribute : TypeFilterAttribute
    {
        public TokenAuthenticationRequiredAttribute() : base(typeof(TokenAuthenticationFilter))
        {
        }
    }

    public class TokenAuthenticationFilter : IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var token = context.HttpContext.Request.Headers["Authorization"];
            if (string.IsNullOrWhiteSpace(token))
            {
                context.Result = new ObjectResult("Unauthorized")
                {
                    StatusCode = 401
                };
            }

            var stringToken = token.ToString();
            if (!stringToken.StartsWith("token"))
            {
                context.Result = new ObjectResult("Unauthorized")
                {
                    StatusCode = 401
                };
            }
        }
    }
}