namespace PollBuilder.Application.DTOs.Polls
{
    public class QuestionResponseDTO
    {
        public Guid Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public int Type { get; set; }
        public int Position { get; set; }

        // This will hold the options belonging to this specific question
        public List<OptionResponseDTO> Options { get; set; } = new List<OptionResponseDTO>();
    }
}