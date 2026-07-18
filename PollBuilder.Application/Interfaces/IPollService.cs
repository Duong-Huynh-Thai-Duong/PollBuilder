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

        Task<bool> SubmitVoteAsync(string code, SubmitVoteDTO voteDto);

        Task<PollResultDTO?> GetPollResultsAsync(string code);

        /// <summary>
        /// Fetches all polls created by a specific user (creator).
        /// </summary>
        Task<List<PollResponseDTO>> GetPollsByCreatorAsync(string creatorId);

        Task<bool> ClosePollAsync(string code, string creatorId);

        Task<bool> DeletePollAsync(string code, string creatorId);

        Task<bool> HasUserVotedAsync(string pollCode, string voterToken);
    }
}