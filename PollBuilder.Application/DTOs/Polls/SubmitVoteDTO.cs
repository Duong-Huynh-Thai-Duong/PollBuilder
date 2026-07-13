namespace PollBuilder.Application.DTOs.Polls
{
    public class SubmitVoteDTO
    {
        // Optional: In case you want to track guest names or logged-in users later
        public string? VoterName { get; set; }

        // A list of the specific options the user selected
        public List<QuestionAnswerDTO> Answers { get; set; } = new List<QuestionAnswerDTO>();
    }

    public class QuestionAnswerDTO
    {
        public Guid QuestionId { get; set; }
        public Guid OptionId { get; set; }
    }
}