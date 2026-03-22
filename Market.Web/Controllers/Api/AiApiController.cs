using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Market.Web.Authorization;
using Market.Web.Core.Models;
using Market.Web.Services.AI;
using Market.Web.Services;
using Market.Web.Core.Exceptions;
using System.Collections.Generic;

namespace Market.Web.Controllers.Api;

[Route("api/ai")]
[ApiController]
[Authorize]
public class AiApiController : ControllerBase
{
    private readonly IAuctionProcessingService _auctionProcessingService;

    public AiApiController(IAuctionProcessingService auctionProcessingService)
    {
        _auctionProcessingService = auctionProcessingService;
    }

    [HttpPost("generate/{auctionId}")]
    [Seller]
    public async Task<IActionResult> Generate([FromRoute] int auctionId)
    {
        try
        {
            await _auctionProcessingService.ScheduleAiGenerationAsync(auctionId);
            return Accepted(new { status = "AiProcessing processing started" });
        }
        catch (KeyNotFoundException)
        {
            return NotFound("Auction draft not found.");
        }
        catch (AiProcessingConflictException ex)
        {
            return Conflict(ex.Message);
        }
        catch (AuctionProcessingException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}