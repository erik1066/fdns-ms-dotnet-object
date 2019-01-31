
using System;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using Foundation.ObjectService.Exceptions;

#pragma warning disable 1591 // disables the warnings about missing Xml code comments

namespace Foundation.ObjectService.WebUI
{
    public sealed class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate next;

        public ErrorHandlingMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var code = HttpStatusCode.InternalServerError; // 500 if unexpected

            if      (exception is FormatException)                 code = HttpStatusCode.BadRequest;
            else if (exception is JsonReaderException)             code = HttpStatusCode.BadRequest;
            else if (exception is MongoWriteException)             code = HttpStatusCode.BadRequest;
            else if (exception is ImmutableCollectionException)    code = HttpStatusCode.BadRequest;
            else if (exception is BsonSerializationException)      code = HttpStatusCode.BadRequest;

            var numericCode = (int)code;

            var result = JsonConvert.SerializeObject(new ProblemDetails() 
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = code.ToString(),
                Status = numericCode,
                Detail = exception.Message
            });

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = numericCode;
            return context.Response.WriteAsync(result);
        }
    }
}

#pragma warning restore 1591