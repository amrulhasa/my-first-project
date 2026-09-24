using BDTechMarket.Models;
using BDTechMarket.Models.ViewModels;
using BDTechMarket.Services.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BDTechMarket.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IEmailSender emailSender,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        // =========================================================
        // LOGIN
        // =========================================================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(
                    "Index",
                    "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;

            return View(new LoginVM());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginVM model,
            string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email.Trim();

            var user =
                await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid email or password.");

                return View(model);
            }

            var result =
                await _signInManager.PasswordSignInAsync(
                    user,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: true);

            if (result.Succeeded)
            {
                // Return to the originally requested local URL.
                if (!string.IsNullOrWhiteSpace(returnUrl) &&
                    Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                // Admin → Admin dashboard.
                if (await _userManager.IsInRoleAsync(
                        user,
                        "Admin"))
                {
                    return RedirectToAction(
                        "Index",
                        "Admin");
                }

                // Normal user → Home.
                return RedirectToAction(
                    "Index",
                    "Home");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Your account has been temporarily locked because of multiple failed login attempts. Please try again later.");
            }
            else if (result.IsNotAllowed)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Sign in is currently not allowed for this account.");
            }
            else
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid email or password.");
            }

            return View(model);
        }

        // =========================================================
        // REGISTER
        // =========================================================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(
                    "Index",
                    "Home");
            }

            return View(new RegisterVM());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            RegisterVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email =
                model.Email.Trim().ToLowerInvariant();

            var fullName =
                model.FullName.Trim();

            var phone =
                model.PhoneNumber.Trim();

            // -------------------------
            // Check duplicate email
            // -------------------------

            var existingUser =
                await _userManager.FindByEmailAsync(email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "An account with this email address already exists.");

                return View(model);
            }

            // -------------------------
            // Create user
            // -------------------------

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                PhoneNumber = phone,

                // No email verification is required.
                EmailConfirmed = true
            };

            var createResult =
                await _userManager.CreateAsync(
                    user,
                    model.Password);

            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                return View(model);
            }

            // -------------------------
            // Make sure User role exists
            // -------------------------

            if (!await _roleManager.RoleExistsAsync("User"))
            {
                var roleResult =
                    await _roleManager.CreateAsync(
                        new IdentityRole("User"));

                if (!roleResult.Succeeded)
                {
                    _logger.LogError(
                        "Could not create User role for new account {Email}",
                        email);
                }
            }

            // -------------------------
            // Add User role
            // -------------------------

            var addRoleResult =
                await _userManager.AddToRoleAsync(
                    user,
                    "User");

            if (!addRoleResult.Succeeded)
            {
                _logger.LogError(
                    "Could not add User role to {Email}",
                    email);

                foreach (var error in addRoleResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                return View(model);
            }

            // -------------------------
            // Registration complete
            // -------------------------

            TempData["success"] =
                "Your account has been created successfully. Please sign in.";

            return RedirectToAction(
                nameof(Login));
        }

        // =========================================================
        // FORGOT PASSWORD
        // =========================================================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordVM());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(
            ForgotPasswordVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email =
                model.Email.Trim();

            var user =
                await _userManager.FindByEmailAsync(email);

            // Always show the same confirmation page.
            // This prevents email/account enumeration.
            if (user == null)
            {
                return View(
                    "ForgotPasswordConfirmation");
            }

            var token =
                await _userManager.GeneratePasswordResetTokenAsync(
                    user);

            var resetLink =
                Url.Action(
                    nameof(ResetPassword),
                    "Account",
                    new
                    {
                        userId = user.Id,
                        token
                    },
                    Request.Scheme);

            try
            {
                await _emailSender.SendEmailAsync(
                    user.Email!,
                    "Reset your BDTechMarket password",
                    BuildPasswordResetEmail(
                        user.FullName ?? "Customer",
                        resetLink!));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send password reset email to {Email}",
                    email);
            }

            return View(
                "ForgotPasswordConfirmation");
        }

        // =========================================================
        // RESET PASSWORD
        // =========================================================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ResetPassword(
            string? userId,
            string? token)
        {
            if (string.IsNullOrWhiteSpace(userId) ||
                string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(
                    "Invalid password reset link.");
            }

            return View(
                new ResetPasswordVM
                {
                    UserId = userId,
                    Token = token
                });
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            ResetPasswordVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user =
                await _userManager.FindByIdAsync(
                    model.UserId);

            if (user == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid password reset request.");

                return View(model);
            }

            var result =
                await _userManager.ResetPasswordAsync(
                    user,
                    model.Token,
                    model.Password);

            if (result.Succeeded)
            {
                return View(
                    "ResetPasswordConfirmation");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            return View(model);
        }

        // =========================================================
        // LOGOUT
        // =========================================================

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction(
                "Index",
                "Home");
        }

        // =========================================================
        // ACCESS DENIED
        // =========================================================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // =========================================================
        // PASSWORD RESET EMAIL TEMPLATE
        // =========================================================

        private static string BuildPasswordResetEmail(
            string fullName,
            string resetLink)
        {
            var safeName =
                System.Net.WebUtility.HtmlEncode(
                    fullName);

            var safeLink =
                System.Net.WebUtility.HtmlEncode(
                    resetLink);

            return $"""
                <!DOCTYPE html>
                <html>
                <body style="font-family: Arial, sans-serif; background:#f5f6f8; padding:30px;">
                    <div style="max-width:600px; margin:auto; background:white; padding:35px; border-radius:12px;">

                        <h2 style="color:#0d6efd;">
                            Reset your BDTechMarket password
                        </h2>

                        <p>
                            Hello {safeName},
                        </p>

                        <p>
                            We received a request to reset your
                            BDTechMarket password.
                        </p>

                        <p style="text-align:center; margin:30px 0;">

                            <a href="{safeLink}"
                               style="background:#0d6efd;
                                      color:white;
                                      padding:12px 24px;
                                      text-decoration:none;
                                      border-radius:6px;">

                                Reset Password

                            </a>

                        </p>

                        <p>
                            If you did not request this password reset,
                            you can safely ignore this email.
                        </p>

                        <p>
                            Regards,<br/>
                            BDTechMarket Team
                        </p>

                    </div>
                </body>
                </html>
                """;
        }
    }
}