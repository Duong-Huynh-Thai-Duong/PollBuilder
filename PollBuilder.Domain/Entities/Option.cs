namespace PollBuilder.Domain.Entities
{
    public class Option
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Foreign Key to Question
        public Guid QuestionId { get; set; }
        public Question? Question { get; set; }

        public string Text { get; set; } = string.Empty;
        public int Position { get; set; }

        // Navigation property
        public ICollection<Vote> Votes { get; set; } = new List<Vote>();
    }
}