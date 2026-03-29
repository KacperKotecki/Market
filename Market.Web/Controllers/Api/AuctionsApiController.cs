using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Market.Web.Authorization;
using Market.Web.Services;
using System.Security.Claims;

namespace Market.Web.Controllers;

[Route("api/auctions")]
[ApiController]
[Authorize]
public class AuctionsApiController : ControllerBase
{
    private readonly IAuctionProcessingService _auctionProcessingService;

    public AuctionsApiController(IAuctionProcessingService auctionProcessingService)
    {
        _auctionProcessingService = auctionProcessingService;
    }

    [HttpPost("draft-images")]
    [Seller]
    public async Task<IActionResult> UploadDraftImages([FromForm] List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest("Brak plików.");
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var tempFolder = Path.Combine(Directory.GetCurrentDirectory(), "market_uploads", "temp");
        if (!Directory.Exists(tempFolder))
        {
            Directory.CreateDirectory(tempFolder);
        }

        var tempPaths = new List<string>();
        foreach (var file in files)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var tempPath = Path.Combine(tempFolder, fileName);
            using (var stream = new FileStream(tempPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            tempPaths.Add(tempPath);
        }

        var auctionId = await _auctionProcessingService.CreateImageProcessingDraftAsync(userId, tempPaths);

        return Accepted(new { auctionId = auctionId });
    }

    [HttpGet("{id}/status")]
    public async Task<IActionResult> GetStatus(int id)
    {
        var statusDto = await _auctionProcessingService.GetAuctionStatusAsync(id);
        if (statusDto == null) return NotFound();

        return Ok(new
        {
            status = statusDto.Status,
            title = statusDto.Title,
            description = statusDto.Description,
            price = statusDto.Price,
            category = statusDto.Category,
            generatedByAi = statusDto.GeneratedByAi
        });
    }
}