using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using BookwormFunctions.PorterLib;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace BookwormFunctions.Http
{
    public class HttpPorter : IAsyncDisposable
    {
        protected Porter _porter;

        public HttpPorter(Porter porter)
        {
            _porter = porter;
        }

        [FunctionName("HttpPorter")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "name" })]
		[OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
		[OpenApiParameter(name: "FromLang", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Language of the given text. If the parameter is empty than it will be detected using the text.")]
        [OpenApiParameter(name: "ToLang", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Target language. If empty it's considered the same as FromLang which means that words will be explained - not translated")]
		[OpenApiRequestBody(contentType: "application/json", bodyType: typeof(string), Description = "Text to analyze. Either JSON with TextForAnalsis object or raw text.")]
        [OpenApiRequestBody(contentType: "text/plain", bodyType: typeof(string), Description = "Text to analyze. Either JSON with TextForAnalsis object or raw text.")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "CorrelationID containing response")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
			{
                log.LogInformation("C# HTTP trigger function processed a request.");
                var corellationID = Guid.NewGuid().ToString();
                await _porter.Run(req, corellationID).ConfigureAwait(false);

                return new OkObjectResult(new Dictionary<string, string> { ["correlationId"] = corellationID });
            }
            catch(Exception ex)
			{
                return new BadRequestObjectResult(ex.Message);
			}
        }

        public ValueTask DisposeAsync() =>
            _porter.DisposeAsync();
    }
}

