using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PollBuilder.MVC.ViewModels
{
    public class PollBuilderViewModel
    {
        [Required(ErrorMessage = "Your survey needs a catchy title!")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "The title must be between 3 and 100 characters.")]
        public string FormTitle { get; set; } = "Untitled Request";


        public string? FormDescription { get; set; }

        // Ensures the creator doesn't try to submit an entirely empty board
        [MinLength(1, ErrorMessage = "You must add at least one question to publish this form.")]
        public List<QuestionViewModel> Questions { get; set; } = new List<QuestionViewModel>();
    }
}
