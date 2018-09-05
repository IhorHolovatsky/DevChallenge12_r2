using System;
using System.Net;
using System.Threading.Tasks;
using CDN.Domain.Constants;
using CssOptimizer.Domain.Exceptions;
using CssOptimizer.Domain.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CssOptimizer.Api.Filters
{
    public class GlobalExceptionFilter : IAsyncExceptionFilter
    {
        public Task OnExceptionAsync(ExceptionContext context)
        {
            if (IsKnownException(context))
                return Task.CompletedTask;
        


            context.Result = new JsonResult(GetExceptionDetails(context.Exception))
            {
                StatusCode = (int)HttpStatusCode.InternalServerError
            };

            return Task.CompletedTask;
        }

        #region Private methods

        /// <summary>
        /// Recursivelly get exception details
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private static object GetExceptionDetails(Exception e)
        {
            if (e == null) return null;

            return new
            {
                Message = "An error has occured.",
                ExceptionMessage = e.Message,
                ExceptionType = e.GetType().FullName,
                e.StackTrace,
                InnerException = GetExceptionDetails(e.InnerException)
            };
        }

        private static bool IsKnownException(ExceptionContext context)
        {
            var exception = context.Exception;

            switch (exception)
            {
                case InvalidRequestParameterException _:
                    context.Result = new JsonResult(new ResponseError(RequestErrorCodes.INVALID_REQUEST_URL_PARAMETER, exception.Message));
                    context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Conflict;
                    return true;
                case NoAvailableInstacesException _:
                    context.Result = new JsonResult(new ResponseError(RequestErrorCodes.NO_AVAILABLE_CHROME_INSTANCES, exception.Message));
                    context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Conflict;
                    return true;
            }

            return false;
        }

        #endregion
    }
}