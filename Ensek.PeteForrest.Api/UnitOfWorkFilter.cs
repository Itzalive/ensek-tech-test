using Ensek.PeteForrest.Services.Data;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Ensek.PeteForrest.Api {
    public class UnitOfWorkFilter(IHttpContextAccessor httpContextAccessor) : IAsyncActionFilter {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
            var actionExecutedContext = await next();
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext == null) return;

            var dbContext = httpContext.RequestServices.GetRequiredService<MeterContext>();
            if (actionExecutedContext.Exception == null) {
                await dbContext.SaveChangesAsync();
            }
        }
    }
}