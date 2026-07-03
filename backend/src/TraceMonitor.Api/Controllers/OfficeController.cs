using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraceMonitor.Api.Contracts;

namespace TraceMonitor.Api.Controllers;

[ApiController]
[Route("api/office")]
[Authorize]
public class OfficeController(IConfiguration config) : ControllerBase
{
    [HttpGet]
    public ActionResult<OfficeLocationDto> Get()
    {
        var section = config.GetSection("Office");
        return new OfficeLocationDto(
            section.GetValue<double>("Lat"),
            section.GetValue<double>("Lon"),
            section.GetValue<string>("Address") ?? "");
    }
}
