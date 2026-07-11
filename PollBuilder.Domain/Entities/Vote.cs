namespace PollBuilder.Domain.Entities
{
    public class Vote
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Foreign Key to Question (Always required)
        public Guid QuestionId { get; set; }
        public Question? Question { get; set; }

        // Foreign Key to Option (Nullable for OpenText questions)
        public Guid? OptionId { get; set; }
        public Option? Option { get; set; }

        // Identifiers
        public string? UserId { get; set; } // If a logged-in creator votes
        public string VoterToken { get; set; } = string.Empty; // Anonymous session ID

        // For Merit Requirement: Open text responses
        public string? OpinionText { get; set; }
    }
}