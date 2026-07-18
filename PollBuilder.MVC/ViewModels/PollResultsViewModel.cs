namespace PollBuilder.MVC.ViewModels
{
    public class PollResultsViewModel
    {
        public string PollId { get; set; } = string.Empty;
        public string PollTitle { get; set; } = string.Empty;

        // Great for a "Total Responses: 1,500" header at the top of your page
        public int TotalPollVotes { get; set; }

        public List<QuestionResultViewModel> Questions { get; set; } = new List<QuestionResultViewModel>();
    }
}