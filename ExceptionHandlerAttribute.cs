using ASB.API.APIPlatform.Producer.Exceptions;
using Microsoft.Rest;
using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Filters;

namespace ASB.API.TrueRewardsRedemptionExp
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ExceptionHandlerAttribute : ExceptionFilterAttribute
    {
        public override void OnException(HttpActionExecutedContext context)
        {
            var ex = context.Exception;

            var statusCode = HttpStatusCode.InternalServerError;

            if (ex is NotFoundException)
            {
                statusCode = HttpStatusCode.NotFound;
            }

            if (ex is ValidationErrorException)
            {
                statusCode = HttpStatusCode.BadRequest;
            }

            if (ex is ForbiddenException)
            {
                statusCode = HttpStatusCode.Forbidden;
            }

            if (ex is UnauthorizedException)
            {
                statusCode = HttpStatusCode.Unauthorized;
            }

            if (ex is HttpOperationException)
            {
                statusCode = (ex as HttpOperationException).Response.StatusCode;
            }

            var response = context.Request.CreateResponse(
                    statusCode, ex.Message
                );

            throw new HttpResponseException(response);
        }
    }
}