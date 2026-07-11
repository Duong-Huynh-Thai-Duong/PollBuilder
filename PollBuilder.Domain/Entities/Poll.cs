using PollBuilder.Domain.Enums;

namespace PollBuilder.Domain.Entities
{
    public class Poll
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Identity Link
        public string? CreatorId { get; set; }

        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        public PollStatus Status { get; set; } = PollStatus.Created;
        public bool IsActive { get; set; } = true;

        public DateTime LaunchDate { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiryDate { get; set; }

        // Navigation property: A poll now has many Questions
        public ICollection<Question> Questions { get; set; } = new List<Question>();
    }
}