using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebAPI.Filters
{
    /// <summary>
    /// Konverta genericki 403 Forbud odgovor u stilizirani JSON
    /// sa jasnom porukom (korisiti se za _> NotFirstLogin policy).
    /// </summary>
    public sealed class FirstLoginProblemDetailsFilter : IAsyncResultFilter
    {
        public Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            if (context.Result is ForbidResult)
            {
                context.Result = new ObjectResult(new ProblemDetails
                {
                    Title = "Password change required",
                    Detail = "You must change your password before continuing.",
                    Status = StatusCodes.Status403Forbidden
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
            return next();
        }
    }
}
