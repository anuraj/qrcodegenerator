using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static QRCoder.PayloadGenerator;
using QRCoder;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using System.Net;

namespace dotnetthoughts.web
{
    public static class QRGeneratorFunction
    {
        [OpenApiOperation(operationId: "QRGenerator",
            tags: new[] { "QRGenerator" },
            Summary = "Generate QR Code for the URL",
            Description = "Generate QR Code for the URL",
            Visibility = OpenApiVisibilityType.Important)]
        [OpenApiParameter(name: "url",
            In = ParameterLocation.Query,
            Required = true,
            Type = typeof(Uri),
            Summary = "The URL to generate QR code",
            Description = "The URL to generate QR code",
            Visibility = OpenApiVisibilityType.Important)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK,
            contentType: "image/png",
            bodyType: typeof(FileResult),
            Summary = "The QR code image file",
            Description = "The QR code image file")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest,
            Summary = "If the URL is missing or invalid URL",
            Description = "If the URL is missing or invalid URL")]
        [FunctionName("QRGenerator")]
        public static async Task<IActionResult> Run(
                            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
                            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string url = req.Query["url"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            url = url ?? data?.name;
            if (string.IsNullOrEmpty(url))
            {
                return new BadRequestResult();
            }
            var isAbsoluteUrl = Uri.TryCreate(url, UriKind.Absolute, out Uri resultUrl);
            if (!isAbsoluteUrl)
            {
                return new BadRequestResult();
            }

            var generator = new Url(resultUrl.AbsoluteUri);
            var payload = generator.ToString();

            using (var qrGenerator = new QRCodeGenerator())
            {
                var qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new PngByteQRCode(qrCodeData);
                var qrCodeAsPng = qrCode.GetGraphic(20);
                return new FileContentResult(qrCodeAsPng, "image/png");
            }
        }
    }
}
