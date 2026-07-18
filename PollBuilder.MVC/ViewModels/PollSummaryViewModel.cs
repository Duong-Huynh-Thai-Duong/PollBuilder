namespace PollBuilder.MVC.ViewModels
{
    public class PollSummaryViewModel
    {
        public string PollId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;

        // Formats the date so your HTML can show "Created on Oct 12"
        public DateTime CreatedAt { get; set; }

        // Helps you color-code the cards (e.g., Green for "Active", Gray for "Draft")
        public string Status { get; set; } = string.Empty;

        public int TotalResponses { get; set; }
    }
}
