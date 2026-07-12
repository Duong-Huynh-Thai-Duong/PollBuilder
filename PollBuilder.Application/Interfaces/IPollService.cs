using PollBuilder.Application.DTOs.Polls;

namespace PollBuilder.Application.Interfaces
{
    public interface IPollService
    {
        Task<string> CreatePollAsync(CreatePollDTO createPollDto);

        /// <summary>
        /// Fetches a poll and all its nested questions/options using the 5-character short code.
        /// </summary>
        Task<PollResponseDTO?> GetPollByCodeAsync(string code);
    }
}