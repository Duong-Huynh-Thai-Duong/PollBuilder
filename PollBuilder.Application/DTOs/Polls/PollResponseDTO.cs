namespace PollBuilder.Application.DTOs.Polls
{
    public class PollResponseDTO
    {
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime LaunchDate { get; set; }

        // We can send the enum back as a readable string (e.g., "Created" or "Active")
        public string Status { get; set; } = string.Empty;

        // The full list of questions attached to this poll
        public List<QuestionResponseDTO> Questions { get; set; } = new List<QuestionResponseDTO>();
    }
}