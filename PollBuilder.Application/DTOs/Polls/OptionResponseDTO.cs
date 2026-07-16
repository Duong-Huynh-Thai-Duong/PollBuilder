namespace PollBuilder.Application.DTOs.Polls
{
    public class OptionResponseDTO
    {
        public Guid Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public int Position { get; set; }
    }
}