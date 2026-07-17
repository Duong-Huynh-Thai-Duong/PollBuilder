using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PollBuilder.Application.Interfaces;
using PollBuilder.MVC.ViewModels;

namespace PollBuilder.MVC.Controllers
{
    [Route("[controller]")]
    public class ResultsController : Controller
    {
        private readonly IPollService _pollService;
        private readonly ILogger<ResultsController> _logger;

        public ResultsController(IPollService pollService, ILogger<ResultsController> logger)
        {
            _pollService = pollService;
            _logger = logger;
        }

        /// <summary>
        /// Display live results for a poll
        /// Anyone can view results using just the poll code (no authentication required)
        /// </summary>
        [HttpGet("live-results/{code}")]
        [AllowAnonymous]
        public async Task<IActionResult> LiveResults(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return BadRequest("Poll code is required.");
            }

            try
            {
                // Fetch poll data to verify it exists
                var pollDto = await _pollService.GetPollByCodeAsync(code);
                if (pollDto == null)
                {
                    return NotFound("Poll not found. The poll code may be invalid or expired.");
                }

                // Fetch results data
                var resultsDto = await _pollService.GetPollResultsAsync(code);
                if (resultsDto == null)
                {
                    // Return an empty results page if no votes yet
                        var emptyViewModel = new PollResultsViewModel
                        {
                            PollTitle = pollDto.Title,
                            TotalPollVotes = 0,
                            Questions = new List<QuestionResultViewModel>()
                        };
                        return View(emptyViewModel);
                }

                // Map DTO to ViewModel
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

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading results for poll {code}: {ex.Message}");
                return StatusCode(500, "An error occurred while loading poll results.");
            }
        }

        /// <summary>
        /// API endpoint to get results as JSON (for AJAX/real-time updates)
        /// Used for SignalR or AJAX polling to get fresh data
        /// </summary>
        [HttpGet("api/results/{code}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetResultsJson(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return BadRequest(new { error = "Poll code is required." });
            }

            try
            {
                var resultsDto = await _pollService.GetPollResultsAsync(code);
                if (resultsDto == null)
                {
                    return NotFound(new { error = "Poll not found or no votes yet." });
                }

                return Json(resultsDto);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching results for poll {code}: {ex.Message}");
                return StatusCode(500, new { error = "An error occurred while fetching results." });
            }
        }

        /// <summary>
        /// Redirect old results route to the new live-results route
        /// For backward compatibility
        /// </summary>
        [HttpGet("redirect")]
        [AllowAnonymous]
        public IActionResult RedirectToPoll(string code)
        {
            return RedirectToAction("LiveResults", new { code });
        }
    }
}
