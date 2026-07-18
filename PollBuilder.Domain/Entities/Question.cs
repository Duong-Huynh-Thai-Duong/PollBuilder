using PollBuilder.Domain.Enums;

namespace PollBuilder.Domain.Entities
{
    public class Question
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Foreign Key to Poll
        public Guid PollId { get; set; }
        public Poll? Poll { get; set; }

        public string Text { get; set; } = string.Empty;
        public QuestionType Type { get; set; } = QuestionType.MultipleChoice;
        public int Position { get; set; }

        // Navigation properties
        public ICollection<Option> Options { get; set; } = new List<Option>();
        public ICollection<Vote> Votes { get; set; } = new List<Vote>();
    }
}