using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PollBuilder.Application.Interfaces;
using PollBuilder.Infrastructure.Identity;
using PollBuilder.MVC.ViewModels;

namespace PollBuilder.MVC.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class DashboardController : Controller
    {
        private readonly IPollService _pollService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            IPollService pollService,
            UserManager<ApplicationUser> userManager,
            ILogger<DashboardController> logger)
        {
            _pollService = pollService;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Display the user's dashboard with all their polls
        /// Shows active polls, closed polls, and draft polls
        /// </summary>
        [HttpGet("")]
        [HttpGet("index")]
        // FIX 1: Added <IActionResult> so the method can return the View
        public async Task<IActionResult> Index()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                // Fetch all polls created by this user
                var userPolls = await _pollService.GetPollsByCreatorAsync(user.Id);

                // FIX 2: Added <PollSummaryViewModel> inside the angle brackets
                var activePollSummaries = new List<PollSummaryViewModel>();

                foreach (var p in userPolls.Where(p => p.Status == "Created" || p.Status == "Open" || p.Status == "Ended"))
                {
                    // Ask the database for the real results for this specific poll
                    var results = await _pollService.GetPollResultsAsync(p.Code);

                    activePollSummaries.Add(new PollSummaryViewModel
                    {
                        PollId = p.Code,
                        Title = p.Title,
                        CreatedAt = p.LaunchDate,
                        Status = p.Status,
                        TotalResponses = results?.TotalPollVotes ?? 0
                    });
                }

                var draftPollSummaries = userPolls
                    .Where(p => p.Status == "Draft")
                    .Select(p => new PollSummaryViewModel
                    {
                        PollId = p.Code,
                        Title = p.Title,
                        CreatedAt = p.LaunchDate,
                        Status = p.Status,
                        TotalResponses = 0
                    })
                    .ToList();

                var viewModel = new DashboardViewModel
                {
                    CreatorName = user.UserName ?? "Creator",
                    ActivePolls = activePollSummaries,

                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading dashboard: {ex.Message}");
                return StatusCode(500, "An error occurred while loading your dashboard.");
            }
        }

        /// <summary>
        /// Show detailed stats for a single poll
        /// Allows the creator to see detailed vote breakdown
        /// </summary>
        [HttpGet("poll/{code}")]
        public async Task<IActionResult> PollStats(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return BadRequest("Poll code is required.");
            }

            try
            {
                // Fetch poll details
                var pollDto = await _pollService.GetPollByCodeAsync(code);
                if (pollDto == null)
                {
                    return NotFound("Poll not found.");
                }

                // TODO: Verify that the current user is the poll creator
                // For now, this is open to everyone (security issue)

                // Fetch poll results
                var resultsDto = await _pollService.GetPollResultsAsync(code);
                if (resultsDto == null)
                {
                    return NotFound("Poll results not found.");
                }

                // Map to ViewModel
                var viewModel = new PollResultsViewModel
                {
                    PollTitle = pollDto.Title,
                    TotalPollVotes = resultsDto.TotalPollVotes,
                    Questions = resultsDto.Questions.Select(q => new QuestionResultViewModel
                    {
                        QuestionText = q.Text,
                        Type = q.Type, // Pass the type through!

                        // FIX: If it's OpenText, count the text answers. Otherwise, sum the options.
                        TotalQuestionVotes = q.Type == PollBuilder.Domain.Enums.QuestionType.OpenText
        ? (q.TextResponses?.Count ?? 0)
        : q.Options.Sum(o => o.VoteCount),

                        // FIX: Pass the text responses through (assuming your DTO has a TextResponses list)
                        TextResponses = q.TextResponses ?? new List<string>(),

                        Options = q.Options.Select(o => new OptionResultViewModel
                        {
                            OptionText = o.Text,
                            VoteCount = o.VoteCount,
                            VotePercentage = resultsDto.TotalPollVotes > 0
                                ? (o.VoteCount * 100.0 / resultsDto.TotalPollVotes)
                                : 0
                        }).ToList()
                    }).ToList()
                };

                return View("~/Views/Results/LiveResults.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading poll stats for {code}: {ex.Message}");
                return StatusCode(500, "An error occurred while loading poll statistics.");
            }
        }



        /// <summary>
        /// Close a poll to prevent new votes
        /// </summary>
        [HttpPost("ClosePoll")]
        public async Task<IActionResult> ClosePoll(string code)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            bool success = await _pollService.ClosePollAsync(code, userId);

            if (!success)
            {
                return BadRequest("Could not close poll. It may not exist or you don't have permission.");
            }

            // Refresh the page to show the new "Ended" status!
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Delete a poll (only creator can do this)
        /// </summary>
        [HttpPost("DeletePoll")]

        public async Task<IActionResult> DeletePoll(string code)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            bool success = await _pollService.DeletePollAsync(code, userId);

            if (!success)
            {
                return BadRequest("Could not delete poll. It may not exist or you don't have permission.");
            }

            // Refresh the dashboard after deletion
            return RedirectToAction("Index");
        }
    }
}
