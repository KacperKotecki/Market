using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Market.Web.Authorization;
using Market.Web.Core.Models;
using Hangfire;
using Market.Web.Services;
using System.Security.Claims;
using Market.Web.Repositories;

namespace Market.Web.Controllers;

[Route("api/auctions")]
[ApiController]
[Authorize]
public class AuctionsApiController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBackgroundJobClient _jobClient;

    public AuctionsApiController(IUnitOfWork unitOfWork, IBackgroundJobClient jobClient)
    {
        _unitOfWork = unitOfWork;
        _jobClient = jobClient;
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
            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var tempPath = Path.Combine(tempFolder, fileName);
            using (var stream = new FileStream(tempPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            tempPaths.Add(tempPath);
        }

        var auction = new Auction
        {
            AuctionStatus = AuctionStatus.ImagesProcessing,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(7), // Just putting a default
            Title = string.Empty,
            Description = string.Empty,
            Category = string.Empty
        };

        await _unitOfWork.Auctions.AddAsync(auction);
        await _unitOfWork.CompleteAsync(); 

        _jobClient.Enqueue<IAuctionImageWorker>(x => x.ProcessImagesJobAsync(auction.Id, tempPaths.ToArray()));

        return Accepted(new { auctionId = auction.Id });
    }
}