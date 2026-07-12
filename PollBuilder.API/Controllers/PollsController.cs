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
        /// Placeholder for fetching a poll by its short code.
        /// </summary>
        [HttpGet("{code}")]
        public async Task<IActionResult> GetPoll(string code)
        {
            // We will implement the fetching logic here next!
            return Ok($"This will eventually return the data for poll: {code}");
        }
    }
}