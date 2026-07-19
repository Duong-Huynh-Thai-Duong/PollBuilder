using System.ComponentModel.DataAnnotations;

namespace PollBuilder.MVC.ViewModels
{
    public class QuestionViewModel
    {
        public string TemporaryId { get; set; } = Guid.NewGuid().ToString();

        [Required(ErrorMessage = "Don't forget to ask the actual question!")]
        [StringLength(250, ErrorMessage = "Let's keep the question under 250 characters.")]
        public string QuestionText { get; set; } = string.Empty;

        // This maps perfectly to Yun's PollBuilder.Domain.Enums.QuestionType
        [Required]
        public string QuestionType { get; set; } = "MultipleChoice";

        public List<OptionViewModel> Options { get; set; } = new List<OptionViewModel>();
    }
}