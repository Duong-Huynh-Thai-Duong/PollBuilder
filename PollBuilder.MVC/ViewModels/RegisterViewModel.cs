using System.ComponentModel.DataAnnotations;

namespace PollBuilder.MVC.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Please enter an email address.")]
        [EmailAddress(ErrorMessage = "That doesn't look like a valid email.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "A password is required.")]
        [StringLength(100, ErrorMessage = "Your password must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)] // This hides the characters as they type!
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The passwords do not match. Try again!")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}