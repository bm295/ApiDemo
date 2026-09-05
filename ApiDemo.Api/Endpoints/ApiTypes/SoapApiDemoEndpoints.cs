using FunctionalProgramming.Services;
using System.Text;

namespace FunctionalProgramming.Endpoints.ApiTypes;

public sealed class SoapApiDemoEndpoints : IApiDemoEndpointMapper
{
    public void Map(RouteGroupBuilder api)
    {
        api.MapPost("/soap", async (HttpContext context) =>
        {
            var xmlRequest = await new StreamReader(context.Request.Body).ReadToEndAsync(context.RequestAborted);
            var escaped = System.Security.SecurityElement.Escape(xmlRequest);

            var xmlResponse = $"""
                              <?xml version="1.0" encoding="utf-8"?>
                              <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
                                <soap:Body>
                                  <DemoSoapResponse xmlns="https://functionalprogramming.demo/soap">
                                    <Message>SOAP endpoint received your XML request.</Message>
                                    <Echo>{escaped}</Echo>
                                  </DemoSoapResponse>
                                </soap:Body>
                              </soap:Envelope>
                              """;

            context.Response.ContentType = "text/xml; charset=utf-8";
            await context.Response.WriteAsync(xmlResponse);
        });
    }
}
