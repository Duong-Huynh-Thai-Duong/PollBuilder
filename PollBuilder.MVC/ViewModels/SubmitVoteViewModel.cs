using System.ComponentModel.DataAnnotations;

namespace PollBuilder.MVC.ViewModels
{
    // The box that safely catches the voter's answers
    public class SubmitVoteViewModel
    {
        [Required]
        public string PollId { get; set; } = string.Empty;

        // A dictionary to catch their answers. 
        // Example: Key = "Question_1_ID", Value = "Option_B_ID"
        [Required(ErrorMessage = "You must answer all required questions.")]
        public Dictionary<string, string> SelectedAnswers { get; set; } = new Dictionary<string, string>();
    }
}