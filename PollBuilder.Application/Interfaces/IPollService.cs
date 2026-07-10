using PollBuilder.Application.DTOs.Polls;

namespace PollBuilder.Application.Interfaces
{
    public interface IPollService
    {
        /// <summary>
        /// Takes the user's input, saves it to the database, and returns the generated 5-character short code.
        /// This fulfills the "POST /polls" requirement.
        /// </summary>
        Task<string> CreatePollAsync(CreatePollDTO createPollDto);

        // Note: We will also need a method here to fetch a poll by its code (GET /polls/{code}),
        // but we will add that once we create the DTO to send the data back to the user!
    }
}