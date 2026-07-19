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

        [HttpGet("take-poll/{code}")]
        [AllowAnonymous]
        public async Task<IActionResult> TakePoll(string code)
        {
            if (string.IsNullOrEmpty(code)) return BadRequest("Poll code is required.");

            var pollDto = await _pollService.GetPollByCodeAsync(code);
            if (pollDto == null) return NotFound("Poll not found.");
            if (pollDto.Status != "Created" && pollDto.Status != "Active") return BadRequest("This poll is closed.");

            // 1. Get or Generate the Voter Token
            string voterToken;
            if (!Request.Cookies.ContainsKey(VOTER_TOKEN_COOKIE))
            {
                voterToken = Guid.NewGuid().ToString();
                Response.Cookies.Append(VOTER_TOKEN_COOKIE, voterToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddMonths(1)
                });
            }
            else
            {
                voterToken = Request.Cookies[VOTER_TOKEN_COOKIE]!;
            }

            // 2. Check if this token has already voted using your string code
            bool alreadyVoted = await _pollService.HasUserVotedAsync(code, voterToken);
            if (alreadyVoted)
            {
                TempData["ErrorMessage"] = "You have already voted on this poll!";
                return RedirectToAction("LiveResults", "Results", new { code = code });
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
        public async Task<IActionResult> SubmitVote(SubmitVoteViewModel model)
        {
            if (!ModelState.IsValid || model.SelectedAnswers == null)
            {
                return BadRequest("You must select at least one answer.");
            }

            // 1. Grab their token from the cookie
            var voterToken = Request.Cookies[VOTER_TOKEN_COOKIE];
            if (string.IsNullOrEmpty(voterToken))
            {
                return BadRequest("Invalid voting session. Please enable cookies and try again.");
            }

            // 2. Double-check they haven't voted already (prevents multi-click spam)
            bool alreadyVoted = await _pollService.HasUserVotedAsync(model.PollId, voterToken);
            if (alreadyVoted)
            {
                TempData["ErrorMessage"] = "You have already voted on this poll!";
                return RedirectToAction("LiveResults", "Results", new { code = model.PollId });
            }

            // 3. Your original DTO mapping logic (passing voterToken to VoterName)
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