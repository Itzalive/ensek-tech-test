using Ensek.PeteForrest.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ensek.PeteForrest.Infrastructure.Behaviours
{
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