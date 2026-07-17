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

        // FIX 1: Restored the <VotingController> type to the logger
        private readonly ILogger<VotingController> _logger;
        private const string VOTER_TOKEN_COOKIE = "PollBuilder_VoterToken";

        public VotingController(IPollService pollService, ILogger<VotingController> logger)
        {
            _pollService = pollService;
            _logger = logger;
        }

        [HttpGet("take-poll/{code}")]
        [AllowAnonymous]
        // FIX 2: Added <IActionResult> so the method can return a View or BadRequest
        public async Task<IActionResult> TakePoll(string code)
        {
            if (string.IsNullOrEmpty(code)) return BadRequest("Poll code is required.");

            var pollDto = await _pollService.GetPollByCodeAsync(code);
            if (pollDto == null) return NotFound("Poll not found.");
            if (pollDto.Status != "Created" && pollDto.Status != "Active") return BadRequest("This poll is closed.");

            if (!Request.Cookies.ContainsKey(VOTER_TOKEN_COOKIE))
            {
                var voterToken = Guid.NewGuid().ToString();
                Response.Cookies.Append(VOTER_TOKEN_COOKIE, voterToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddMonths(1)
                });
            }

            var viewModel = new TakePollViewModel
            {
                PollId = code,
                Title = pollDto.Title,
                CreatorName = "Poll Creator",
                Questions = pollDto.Questions.Select(q => new DisplayQuestionViewModel
                {
                    QuestionId = q.Id.ToString(),
                    Text = q.Text,
                    Type = q.Type,
                    AvailableOptions = q.Options.ToDictionary(o => o.Id.ToString(), o => o.Text)
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpPost("submit-vote")]
        [AllowAnonymous]
        // FIX 2: Added <IActionResult> here as well
        public async Task<IActionResult> SubmitVote(SubmitVoteViewModel model)
        {
            if (!ModelState.IsValid || model.SelectedAnswers == null)
            {
                return BadRequest("You must select at least one answer.");
            }

            var voterToken = Request.Cookies[VOTER_TOKEN_COOKIE] ?? Guid.NewGuid().ToString();

            var submitVoteDto = new SubmitVoteDTO
            {
                VoterName = voterToken,
                Answers = model.SelectedAnswers.Select(kvp =>
                {
                    bool isGuidOption = Guid.TryParse(kvp.Value, out Guid parsedOptionId);

                    return new QuestionAnswerDTO
                    {
                        QuestionId = Guid.Parse(kvp.Key),
                        OptionId = isGuidOption ? parsedOptionId : null,
                        OpinionText = isGuidOption ? null : kvp.Value
                    };
                }).ToList()
            };

            bool success = await _pollService.SubmitVoteAsync(model.PollId, submitVoteDto);

            if (!success) return BadRequest("Failed to submit vote. You may have already voted, or the poll is closed.");

            return RedirectToAction("LiveResults", "Results", new { code = model.PollId });
        }
    }
}