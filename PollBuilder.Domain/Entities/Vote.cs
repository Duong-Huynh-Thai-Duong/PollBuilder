using System;
using System.Collections.Generic;
using System.Text;

namespace PollBuilder.Domain.Entities
{
    public class Vote
    {
        public string VoterFingerprint { get; set; } = string.Empty;

        public DateTime VotedAt { get; set; } = DateTime.UtcNow;

        public Guid PollOptionId { get; set; }
        public PollOption? PollOption { get; set; }
    }
}
