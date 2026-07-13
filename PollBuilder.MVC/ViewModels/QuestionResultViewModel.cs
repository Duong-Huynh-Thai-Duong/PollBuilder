using System.Collections.Generic;

namespace PollBuilder.MVC.ViewModels
{
    public class QuestionResultViewModel
    {
        public string QuestionText { get; set; } = string.Empty;

        // Some voters might skip questions, so we track votes per-question too!
        public int TotalQuestionVotes { get; set; }

        public List<OptionResultViewModel> Options { get; set; } = new List<OptionResultViewModel>();
    }
}