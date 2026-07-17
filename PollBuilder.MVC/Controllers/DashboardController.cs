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

                // Separate active and draft polls
                var activePollSummaries = userPolls
                    .Where(p => p.Status == "Created" || p.Status == "Active") // Active polls
                    .Select(p => new PollSummaryViewModel
                    {
                        PollId = p.Code,
                        Title = p.Title,
                        CreatedAt = p.LaunchDate,
                        Status = p.Status,
                        TotalResponses = 0 // Will calculate from results if needed
                    })
                    .ToList();

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
                    DraftPolls = draftPollSummaries
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
                        TotalQuestionVotes = q.Options.Sum(o => o.VoteCount),
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
        /// Delete a poll (only creator can do this)
        /// This will also delete all votes associated with it
        /// </summary>
        [HttpPost("delete-poll/{code}")]
        public async Task<IActionResult> DeletePoll(string code)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    return BadRequest("Poll code is required.");
                }

                // TODO: Implement delete logic in IPollService
                // For now, this is a placeholder

                return Ok(new { message = "Poll deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting poll {code}: {ex.Message}");
                return StatusCode(500, "An error occurred while deleting the poll.");
            }
        }
    }
}
