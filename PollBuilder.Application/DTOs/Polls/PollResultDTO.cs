namespace PollBuilder.Application.DTOs.Polls
{
    public class PollResultDTO
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int TotalPollVotes { get; set; } // Handy for calculating percentages!
        public List<QuestionResultDTO> Questions { get; set; } = new List<QuestionResultDTO>();
    }

    public class QuestionResultDTO
    {
        public Guid Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public List<OptionResultDTO> Options { get; set; } = new List<OptionResultDTO>();
    }

    public class OptionResultDTO
    {
        public Guid Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public int VoteCount { get; set; } // The magic number!
    }
}