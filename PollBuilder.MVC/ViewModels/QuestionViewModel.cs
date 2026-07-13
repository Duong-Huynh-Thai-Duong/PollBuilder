using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PollBuilder.MVC.ViewModels
{
    public class QuestionViewModel
    {
        // A temporary ID so your frontend JavaScript knows which card it's interacting with
        public string TemporaryId { get; set; } = Guid.NewGuid().ToString();

        [Required(ErrorMessage = "Don't forget to ask the actual question!")]
        [StringLength(250, ErrorMessage = "Let's keep the question under 250 characters.")]
        public string QuestionText { get; set; } = string.Empty;

        // Forces the creator to provide at least two choices
        [MinLength(2, ErrorMessage = "Every question needs at least two options.")]
        public List<OptionViewModel> Options { get; set; } = new List<OptionViewModel>();
    }
}