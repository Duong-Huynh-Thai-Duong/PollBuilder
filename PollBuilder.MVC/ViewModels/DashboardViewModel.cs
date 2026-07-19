namespace PollBuilder.MVC.ViewModels
{
    public class DashboardViewModel
    {
        // Great for a "Welcome back, Mun!" header
        public string CreatorName { get; set; } = string.Empty;

        // Splitting these into two lists makes it incredibly easy to draw 
        // an "Active" section and a "Drafts" section on your HTML page!
        public List<PollSummaryViewModel> ActivePolls { get; set; } = new List<PollSummaryViewModel>();

    }
}