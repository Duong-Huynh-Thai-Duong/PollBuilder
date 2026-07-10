namespace PollBuilder.Application.DTOs.Polls
{
    public class CreateQuestionDTO
    {
        public string Text { get; set; } = string.Empty;

        // We use an int here that maps to your QuestionType enum (0 = MultipleChoice, 3 = OpenText)
        public int Type { get; set; }

        public int Position { get; set; }

        // If it's a multiple-choice question, they will send a list of text options
        public List<string> Options { get; set; } = new List<string>();
    }
}