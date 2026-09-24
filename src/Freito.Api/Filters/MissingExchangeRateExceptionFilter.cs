using Freito.Domain.Quoting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Freito.Api.Filters;

public sealed class MissingExchangeRateExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not MissingExchangeRateException exception) return;

        context.Result = new ObjectResult(new
        {
            errors = new[] { $"{exception.Message} Add or update this currency's exchange rate in Admin master data." },
        })
        {
            StatusCode = StatusCodes.Status422UnprocessableEntity,
        };
        context.ExceptionHandled = true;
    }
}
