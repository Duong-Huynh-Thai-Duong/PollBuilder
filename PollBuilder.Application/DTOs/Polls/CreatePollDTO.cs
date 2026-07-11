namespace PollBuilder.Application.DTOs.Polls
{
    public class CreatePollDTO
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Links the poll to the Identity user who created it
        public string? CreatorId { get; set; }

        // The list of questions attached to this new poll
        public List<CreateQuestionDTO> Questions { get; set; } = new List<CreateQuestionDTO>();
    }
}