using Microsoft.AspNetCore.Mvc;
using PollBuilder.Application.DTOs.Polls;
using PollBuilder.Application.Interfaces;

namespace PollBuilder.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PollsController : ControllerBase
    {
        private readonly IPollService _pollService;

        // Inject the service interface we built in the Application layer
        public PollsController(IPollService pollService)
        {
            _pollService = pollService;
        }

        /// <summary>
        /// POST: api/polls
        /// Receives the poll data and generates a unique short code.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreatePoll([FromBody] CreatePollDTO createPollDto)
        {
            // Failsafe: Ensure the incoming data matches our DTO rules
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Hand the data off to the manager/service
            var shortCode = await _pollService.CreatePollAsync(createPollDto);

            // Return a 201 Created HTTP status along with the new short code
            return CreatedAtAction(nameof(GetPoll), new { code = shortCode }, new { Code = shortCode });
        }

        /// <summary>
        /// GET: api/polls/{code}
        /// Fetches a poll and its questions/options by its 5-character short code.
        /// </summary>
        [HttpGet("{code}")]
        public async Task<IActionResult> GetPoll(string code)
        {
            // Call the service to run the EF Core .Include() query
            var poll = await _pollService.GetPollByCodeAsync(code);

            // If the service returns null, the code doesn't exist
            if (poll == null)
            {
                return NotFound(new { Message = $"No poll found with code: {code}" });
            }

            // If found, return a 200 OK status with the perfectly formatted JSON hierarchy
            return Ok(poll);
        }

        /// <summary>
        /// POST: api/polls/{code}/vote
        /// Submits a user's vote for a specific poll.
        /// </summary>
        [HttpPost("{code}/vote")]
        public async Task<IActionResult> SubmitVote(string code, [FromBody] SubmitVoteDTO voteDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var success = await _pollService.SubmitVoteAsync(code, voteDto);

            if (!success)
            {
                return NotFound(new { Message = $"Poll '{code}' not found or is currently inactive." });
            }

            return Ok(new { Message = "Vote successfully recorded!" });
        }

        /// <summary>
        /// GET: api/polls/{code}/results
        /// Fetches the aggregated vote counts for a specific poll.
        /// </summary>
        [HttpGet("{code}/results")]
        public async Task<IActionResult> GetPollResults(string code)
        {
            var results = await _pollService.GetPollResultsAsync(code);

            if (results == null)
            {
                return NotFound(new { Message = $"No poll found with code: {code}" });
            }

            return Ok(results);
        }
    }
}