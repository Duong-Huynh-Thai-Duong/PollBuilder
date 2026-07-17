using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PollBuilder.Application.DTOs.Polls;
using PollBuilder.Application.Interfaces;
using PollBuilder.MVC.ViewModels;

namespace PollBuilder.MVC.Controllers
{
    [Route("[controller]")]
    public class VotingController : Controller
    {
        private readonly IPollService _pollService;
        private readonly ILogger<VotingController> _logger;
        private const string VOTER_TOKEN_COOKIE = "PollBuilder_VoterToken";

        public VotingController(IPollService pollService, ILogger<VotingController> logger)
        {
            _pollService = pollService;
            _logger = logger;
        }

        /// <summary>
        /// Display the voting/poll-taking page (GET)
        /// Maps /voting/take-poll/{code} to show poll questions and options
        /// </summary>
        [HttpGet("take-poll/{code}")]
        [AllowAnonymous]
        public async Task<IActionResult> TakePoll(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return BadRequest("Poll code is required.");
            }

            try
            {
                // Fetch poll data from the database
                var pollDto = await _pollService.GetPollByCodeAsync(code);

                if (pollDto == null)
                {
                    return NotFound("Poll not found. The poll code may be invalid or expired.");
                }

                // Check if poll is still active
                if (pollDto.Status != "Created" && pollDto.Status != "Active")
                {
                    return BadRequest("This poll is no longer accepting votes.");
                }

                // Generate or retrieve voter token for anonymous tracking
                if (!Request.Cookies.ContainsKey(VOTER_TOKEN_COOKIE))
                {
                    var voterToken = Guid.NewGuid().ToString();
                    Response.Cookies.Append(VOTER_TOKEN_COOKIE, voterToken, new Microsoft.AspNetCore.Http.CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict,
                        Expires = DateTimeOffset.UtcNow.AddMonths(1)
                    });
                }

                // Map DTO to ViewModel for the view
                var viewModel = new TakePollViewModel
                {
                    PollId = code, // Store the short code instead of the database ID
                    Title = pollDto.Title,
                    CreatorName = "Poll Creator", // TODO: Fetch actual creator name if needed
                    Questions = pollDto.Questions.Select(q => new DisplayQuestionViewModel
                    {
                        QuestionId = q.Id.ToString(),
                        Text = q.Text,
                        AvailableOptions = q.Options.ToDictionary(o => o.Id.ToString(), o => o.Text)
                    }).ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading poll {code}: {ex.Message}");
                return StatusCode(500, "An error occurred while loading the poll.");
            }
        }

        /// <summary>
        /// Handle vote submission (POST)
        /// Receives SelectedAnswers dictionary and creates Vote records
        /// </summary>
        [HttpPost("submit-vote")]
        [AllowAnonymous]
        public async Task<IActionResult> SubmitVote([FromForm] string pollCode, [FromForm] Dictionary<string, string> selectedAnswers)
        {
            try
            {
                if (string.IsNullOrEmpty(pollCode))
                {
                    return BadRequest("Poll code is required.");
                }

                if (selectedAnswers == null || selectedAnswers.Count == 0)
                {
                    return BadRequest("You must select at least one answer.");
                }

                // Get the voter token from cookie (for anonymous tracking)
                var voterToken = Request.Cookies[VOTER_TOKEN_COOKIE] ?? Guid.NewGuid().ToString();

                // Build the DTO for submission
                var submitVoteDto = new SubmitVoteDTO
                {
                    VoterName = voterToken, // Store voterToken as VoterName for now
                    Answers = selectedAnswers.Select(kvp => new QuestionAnswerDTO
                    {
                        QuestionId = Guid.Parse(kvp.Key),
                        OptionId = Guid.Parse(kvp.Value)
                    }).ToList()
                };

                // Submit the vote
                bool success = await _pollService.SubmitVoteAsync(pollCode, submitVoteDto);

                if (!success)
                {
                    return BadRequest("Failed to submit vote. The poll may be closed or invalid.");
                }

                // Redirect to results page
                return RedirectToAction("LiveResults", "Results", new { code = pollCode });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error submitting vote: {ex.Message}");
                return StatusCode(500, "An error occurred while submitting your vote.");
            }
        }

        /// <summary>
        /// Alternative route mapping for form action asp-action="SubmitVote"
        /// This handles the traditional ASP.NET Core form submission pattern
        /// </summary>
        [HttpPost("submit-vote")]
        [AllowAnonymous]
        public async Task<IActionResult> SubmitVoteForm(SubmitVoteViewModel model)
        {
            try
            {
                if (model?.SelectedAnswers == null || model.SelectedAnswers.Count == 0)
                {
                    ModelState.AddModelError("", "You must select at least one answer.");
                    return BadRequest(ModelState);
                }

                var voterToken = Request.Cookies[VOTER_TOKEN_COOKIE] ?? Guid.NewGuid().ToString();

                // Extract the poll code from the model (you need to add this to SubmitVoteViewModel)
                string pollCode = TempData["PollCode"]?.ToString() ?? "";

                var submitVoteDto = new SubmitVoteDTO
                {
                    VoterName = voterToken, // Store voterToken as VoterName for now
                    Answers = model.SelectedAnswers
                        .Where(kvp => kvp.Value != null)
                        .Select(kvp => new QuestionAnswerDTO
                        {
                            QuestionId = Guid.Parse(kvp.Key),
                            OptionId = Guid.Parse(kvp.Value)
                        }).ToList()
                };

                bool success = await _pollService.SubmitVoteAsync(pollCode, submitVoteDto);

                if (!success)
                {
                    return BadRequest("Failed to submit vote. The poll may be closed or invalid.");
                }

                return RedirectToAction("LiveResults", "Results", new { code = pollCode });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error submitting vote: {ex.Message}");
                ModelState.AddModelError("", "An error occurred while submitting your vote.");
                return BadRequest(ModelState);
            }
        }
    }
}
