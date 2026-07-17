using System.Collections.Generic;
using PollBuilder.Domain.Enums; // Ensure you have this using statement

namespace PollBuilder.MVC.ViewModels
{
    public class QuestionResultViewModel
    {
        public string QuestionText { get; set; } = string.Empty;

        // Track what kind of question this is so the UI knows how to render it
        public QuestionType Type { get; set; }

        public int TotalQuestionVotes { get; set; }

        public List<OptionResultViewModel> Options { get; set; } = new List<OptionResultViewModel>();

        // NEW: This list will hold the actual text answers submitted by users
        public List<string> TextResponses { get; set; } = new List<string>();
    }
}