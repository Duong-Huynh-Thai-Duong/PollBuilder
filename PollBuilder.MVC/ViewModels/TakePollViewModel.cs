namespace PollBuilder.MVC.ViewModels
{
    // The master box sent to the public voter
    public class TakePollViewModel
    {
        public string PollId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string CreatorName { get; set; } = string.Empty;

        // The list of questions they need to answer
        public List<DisplayQuestionViewModel> Questions { get; set; } = new List<DisplayQuestionViewModel>();
    }

    // A lightweight read-only question for the voter
    public class DisplayQuestionViewModel
    {
        public string QuestionId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;

        public int Type { get; set; }

        // A dictionary is perfect here: OptionId is the Key, OptionText is the Value
        public Dictionary<string, string> AvailableOptions { get; set; } = new Dictionary<string, string>();
    }
}