using Freito.Api.Filters;
using Freito.Domain.Quoting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Freito.Tests;

public class MissingExchangeRateExceptionFilterTests
{
    [Fact]
    public void MissingExchangeRate_ReturnsActionableUnprocessableEntity()
    {
        var context = new ExceptionContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>())
        {
            Exception = new MissingExchangeRateException("THB"),
        };

        new MissingExchangeRateExceptionFilter().OnException(context);

        var response = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, response.StatusCode);
        Assert.True(context.ExceptionHandled);
        var errors = Assert.IsType<string[]>(response.Value!.GetType().GetProperty("errors")!.GetValue(response.Value));
        Assert.Contains("THB", errors[0]);
        Assert.Contains("Admin master data", errors[0]);
    }
}
