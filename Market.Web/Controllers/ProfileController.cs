using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Market.Web.Core.Exceptions;
using Market.Web.Core.Models;
using Market.Web.Core.ViewModels;
using Market.Web.Services;

namespace Market.Web.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IProfileService _profileService;

        public ProfileController(
            UserManager<ApplicationUser> userManager, 
            IProfileService profileService)
        {
            _userManager = userManager;
            _profileService = profileService;
        }

        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var model = await _profileService.GetEditProfileViewModelAsync(user.Id, user.Email!);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            
            ModelState.Remove("Email"); 

            if (!model.HasCompanyProfile)
            {
                ModelState.Remove("CompanyName");
                ModelState.Remove("NIP");
                ModelState.Remove("InvoiceAddress.Street");
                ModelState.Remove("InvoiceAddress.City");
                ModelState.Remove("InvoiceAddress.PostalCode");
                ModelState.Remove("InvoiceAddress.Country");
            }

            if (!ModelState.IsValid)
            {
                model.Email = user.Email!; 
                return View(model);
            }

            await _profileService.UpdateProfileAsync(user.Id, model);

            TempData["SuccessMessage"] = "Profil został zaktualizowany.";
            return RedirectToAction("Index", "Auctions");
        }

        [HttpGet]
        public async Task<IActionResult> MyFinances()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var model = await _profileService.GetFinancesViewModelAsync(user.Id);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WithdrawFunds()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            try
            {
                var result = await _profileService.WithdrawFundsAsync(user.Id);

                if (!result.Success)
                {
                    TempData["Error"] = result.Message;
                }
                else
                {
                    var userProfile = await _profileService.GetByUserIdAsync(user.Id);
                    TempData["SuccessMessage"] = $"Zlecono wypłatę {result.Amount:N2} PLN na konto {userProfile?.PrivateIBAN}.";
                }
            }
            catch (ConcurrencyException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(MyFinances));
        }
    }
}