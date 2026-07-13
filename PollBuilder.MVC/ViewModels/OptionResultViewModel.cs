namespace PollBuilder.MVC.ViewModels
{
    public class OptionResultViewModel
    {
        public string OptionText { get; set; } = string.Empty;
        public int VoteCount { get; set; }

        // The magic property! Your HTML will just read this number to set the CSS width.
        public double VotePercentage { get; set; }
    }
}