using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PollBuilder.Application.DTOs.Polls;
using PollBuilder.Application.Interfaces;
using PollBuilder.Infrastructure.Identity;
using PollBuilder.MVC.ViewModels;

namespace PollBuilder.MVC.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class CreatorController : Controller
    {
        private readonly IPollService _pollService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CreatorController> _logger;

        public CreatorController(
            IPollService pollService,
            UserManager<ApplicationUser> userManager,
            ILogger<CreatorController> logger)
        {
            _pollService = pollService;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Display the poll creation form (GET)
        /// </summary>
        [HttpGet("create-poll")]
        [AllowAnonymous]
        public IActionResult CreatePoll()
        {
            var viewModel = new PollBuilderViewModel();
            return View(viewModel);
        }

        /// <summary>
        /// Handle poll creation form submission (POST)
        /// Maps the front-end form data to CreatePollDTO and saves to database
        /// </summary>
        [HttpPost("create-poll")]
        public async Task<IActionResult> CreatePoll([FromForm] PollBuilderViewModel model)
        {
            try
            {
                // Validation: Ensure at least one question with at least one option
                if (model.Questions == null || model.Questions.Count == 0)
                {
                    ModelState.AddModelError("", "You must add at least one question to your poll.");
                    return View(model);
                }

                // Get the current user ID (for logged-in creators)
                var userId = User.Identity?.IsAuthenticated == true 
                    ? await _userManager.GetUserIdAsync(await _userManager.GetUserAsync(User))
                    : null;

                // Map ViewModel to DTO
                var createPollDto = new CreatePollDTO
                {
                    Title = model.FormTitle,
                    Description = model.FormTitle, // You can add a Description field to the ViewModel later
                    CreatorId = userId,
                    Questions = model.Questions.Select((q, index) => new CreateQuestionDTO
                    {
                        Text = q.QuestionText,
                        Type = 0, // MultipleChoice (0)
                        Position = index + 1,
                        Options = q.Options.Select(o => o.OptionText).ToList()
                    }).ToList()
                };

                // Call the service to save the poll
                string pollCode = await _pollService.CreatePollAsync(createPollDto);

                // Redirect to the poll results/sharing page
                return RedirectToAction("PollCreated", new { code = pollCode });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating poll: {ex.Message}");
                ModelState.AddModelError("", "An error occurred while creating your poll. Please try again.");
                return View(model);
            }
        }

        /// <summary>
        /// Show confirmation page after poll creation
        /// </summary>
        [HttpGet("poll-created")]
        [AllowAnonymous]
        public async Task<IActionResult> PollCreated(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return RedirectToAction("Index", "Home");
            }

            var poll = await _pollService.GetPollByCodeAsync(code);
            if (poll == null)
            {
                return NotFound("Poll not found.");
            }

            // Store the poll link for display
            ViewBag.PollLink = $"{Request.Scheme}://{Request.Host}/voting/take-poll/{code}";
            ViewBag.PollCode = code;

            return View(poll);
        }

        /// <summary>
        /// Display survey creation form (POST version for the future)
        /// </summary>
        [HttpGet("create-survey")]
        [AllowAnonymous]
        public IActionResult CreateSurvey()
        {
            var viewModel = new PollBuilderViewModel();
            return View(viewModel);
        }

        /// <summary>
        /// Handle survey creation form submission
        /// Similar to CreatePoll but with survey-specific logic
        /// </summary>
        [HttpPost("create-survey")]
        public async Task<IActionResult> CreateSurvey([FromForm] PollBuilderViewModel model)
        {
            // Implementation similar to CreatePoll, but with survey-specific validation
            try
            {
                if (model.Questions == null || model.Questions.Count == 0)
                {
                    ModelState.AddModelError("", "You must add at least one question to your survey.");
                    return View(model);
                }

                var userId = User.Identity?.IsAuthenticated == true
                    ? await _userManager.GetUserIdAsync(await _userManager.GetUserAsync(User))
                    : null;

                var createPollDto = new CreatePollDTO
                {
                    Title = model.FormTitle,
                    Description = model.FormTitle,
                    CreatorId = userId,
                    Questions = model.Questions.Select((q, index) => new CreateQuestionDTO
                    {
                        Text = q.QuestionText,
                        Type = 0,
                        Position = index + 1,
                        Options = q.Options.Select(o => o.OptionText).ToList()
                    }).ToList()
                };

                string pollCode = await _pollService.CreatePollAsync(createPollDto);
                return RedirectToAction("PollCreated", new { code = pollCode });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating survey: {ex.Message}");
                ModelState.AddModelError("", "An error occurred while creating your survey. Please try again.");
                return View(model);
            }
        }

        /// <summary>
        /// Close/deactivate a poll (only creator can do this)
        /// </summary>
        [HttpPost("close-poll")]
        public async Task<IActionResult> ClosePoll(string code)
        {
            // TODO: Implement poll closing logic
            // This requires adding a ClosePollAsync method to IPollService
            return Ok(new { message = "Poll closed successfully." });
        }
    }
}
