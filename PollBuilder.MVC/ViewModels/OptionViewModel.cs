using System.ComponentModel.DataAnnotations;

namespace PollBuilder.MVC.ViewModels
{
    public class OptionViewModel
    {
        [Required(ErrorMessage = "Option text cannot be blank.")]
        [StringLength(100, ErrorMessage = "Options should be concise (under 100 characters).")]
        public string OptionText { get; set; } = string.Empty;
    }
}