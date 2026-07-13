using Microsoft.EntityFrameworkCore;
using PollBuilder.Application.DTOs.Polls;
using PollBuilder.Application.Interfaces;
using PollBuilder.Domain.Entities;
using PollBuilder.Domain.Enums;
using PollBuilder.Infrastructure.Data;

namespace PollBuilder.Infrastructure.Services
{
    public class PollService : IPollService
    {
        private readonly ApplicationDbContext _context;

        // "Inject" the database context so we can talk to SQL Server
        public PollService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> CreatePollAsync(CreatePollDTO createPollDto)
        {
            // 1. Generate a unique 5-character short code
            string shortCode = GenerateShortCode();

            // Safety check: ensure the code doesn't already exist in the database
            while (await _context.Polls.AnyAsync(p => p.Code == shortCode))
            {
                shortCode = GenerateShortCode();
            }

            // 2. Map the DTO to our pure Domain Entities
            var poll = new Poll
            {
                Title = createPollDto.Title,
                Description = createPollDto.Description,
                CreatorId = createPollDto.CreatorId,
                Code = shortCode,
                LaunchDate = DateTime.UtcNow,
                Status = PollStatus.Created,
                IsActive = true
            };

            // Loop through the questions provided by the user
            foreach (var qDto in createPollDto.Questions)
            {
                var question = new Question
                {
                    Text = qDto.Text,
                    Type = (QuestionType)qDto.Type,
                    Position = qDto.Position
                };

                // Only add predefined options if it's a MultipleChoice or YesNo question
                if (question.Type == QuestionType.MultipleChoice || question.Type == QuestionType.YesNo)
                {
                    int optionPosition = 1;
                    foreach (var optText in qDto.Options)
                    {
                        question.Options.Add(new Option
                        {
                            Text = optText,
                            Position = optionPosition++
                        });
                    }
                }

                poll.Questions.Add(question);
            }

            // 3. Save everything to the database
            _context.Polls.Add(poll);
            await _context.SaveChangesAsync();

            return shortCode;
        }

        public async Task<PollResponseDTO?> GetPollByCodeAsync(string code)
        {
            // 1. The EF Core Query
            var poll = await _context.Polls
                .Include(p => p.Questions)           // Join the Questions table
                    .ThenInclude(q => q.Options)     // Join the Options table attached to those Questions
                .FirstOrDefaultAsync(p => p.Code == code);

            // 2. Failsafe: Did we find it?
            if (poll == null)
            {
                return null;
            }

            // 3. Map the raw database entities into our safe Response DTOs
            var response = new PollResponseDTO
            {
                Code = poll.Code,
                Title = poll.Title,
                Description = poll.Description,
                LaunchDate = poll.LaunchDate,
                Status = poll.Status.ToString(), // Converts the Enum (0, 1) into readable text ("Created", "Active")

                // Map Questions and sort them by Position
                Questions = poll.Questions.OrderBy(q => q.Position).Select(q => new QuestionResponseDTO
                {
                    Id = q.Id,
                    Text = q.Text,
                    Type = (int)q.Type,
                    Position = q.Position,

                    // Map Options and sort them by Position
                    Options = q.Options.OrderBy(o => o.Position).Select(o => new OptionResponseDTO
                    {
                        Id = o.Id,
                        Text = o.Text,
                        Position = o.Position
                    }).ToList()
                }).ToList()
            };

            return response;
        }

        public async Task<bool> SubmitVoteAsync(string code, SubmitVoteDTO voteDto)
        {
            // 1. Find the poll to make sure it exists and is active
            var poll = await _context.Polls
                .Include(p => p.Questions)
                .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(p => p.Code == code);

            if (poll == null || !poll.IsActive)
            {
                return false; // Poll not found or closed
            }

            // 2. Loop through each answer submitted by the user
            foreach (var answer in voteDto.Answers)
            {
                // Security Check: Ensure the Question actually belongs to this Poll
                var question = poll.Questions.FirstOrDefault(q => q.Id == answer.QuestionId);

                // Security Check: Ensure the Option actually belongs to that Question
                if (question != null && question.Options.Any(o => o.Id == answer.OptionId))
                {
                    // Create the new Vote record
                    var vote = new Vote
                    {
                        QuestionId = answer.QuestionId,
                        OptionId = answer.OptionId,
                        // If your Vote entity requires a PollId or timestamp, assign them here:
                        // PollId = poll.Id,
                        // SubmittedAt = DateTime.UtcNow
                    };

                    _context.Set<Vote>().Add(vote);
                }
            }

            // 3. Commit all valid votes to the SQL database at once
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<PollResultDTO?> GetPollResultsAsync(string code)
        {
            // 1. Fetch the poll hierarchy
            var poll = await _context.Polls
                .Include(p => p.Questions)
                .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(p => p.Code == code);

            if (poll == null)
            {
                return null;
            }

            // 2. Fetch all votes that belong to any question in this poll
            var questionIds = poll.Questions.Select(q => q.Id).ToList();
            var pollVotes = await _context.Set<Vote>()
                .Where(v => questionIds.Contains(v.QuestionId))
                .ToListAsync();

            // 3. Map to DTO and calculate counts in memory
            var result = new PollResultDTO
            {
                Title = poll.Title,
                Description = poll.Description,
                TotalPollVotes = pollVotes.Count, // Total votes calculated instantly
                Questions = poll.Questions.OrderBy(q => q.Position).Select(q => new QuestionResultDTO
                {
                    Id = q.Id,
                    Text = q.Text,
                    Options = q.Options.OrderBy(o => o.Position).Select(o => new OptionResultDTO
                    {
                        Id = o.Id,
                        Text = o.Text,
                        // Count votes for this specific option from our memory list
                        VoteCount = pollVotes.Count(v => v.OptionId == o.Id)
                    }).ToList()
                }).ToList()
            };

            return result;
        }
        // Helper method to generate the random 5-character string
        private string GenerateShortCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();

            return new string(Enumerable.Repeat(chars, 5)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}