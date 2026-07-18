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

        public PollService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> CreatePollAsync(CreatePollDTO createPollDto)
        {
            // 1. Generate a unique 5-character short code
            string shortCode = GenerateShortCode();

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
                IsActive = true,
                Questions = new List<Question>() // <-- CRASH FIX 1: Initialize list to prevent EF Core NullReferenceException
            };

            foreach (var qDto in createPollDto.Questions)
            {
                var question = new Question
                {
                    Text = qDto.Text,
                    Type = (QuestionType)qDto.Type,
                    Position = qDto.Position,
                    Options = new List<Option>() // <-- CRASH FIX 2: Initialize list
                };


                // FIX: Added QuestionType.Rating so the 1-5 Star options get saved to the database!
                if (question.Type == QuestionType.MultipleChoice || question.Type == QuestionType.YesNo || question.Type == QuestionType.Rating)
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
            var poll = await _context.Polls
                .Include(p => p.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(p => p.Code == code);

            if (poll == null) return null;

            var response = new PollResponseDTO
            {
                Code = poll.Code,
                Title = poll.Title,
                Description = poll.Description,
                LaunchDate = poll.LaunchDate,
                Status = poll.Status.ToString(),

                Questions = poll.Questions.OrderBy(q => q.Position).Select(q => new QuestionResponseDTO
                {
                    Id = q.Id,
                    Text = q.Text,
                    Type = (int)q.Type,
                    Position = q.Position,
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

            if (poll == null || !poll.IsActive) return false;

            // --- NEW: ENFORCE 'VOTE ONCE' RUBRIC REQUIREMENT ---
            // Your VotingController passes the secure cookie token inside voteDto.VoterName
            string voterToken = voteDto.VoterName ?? string.Empty;
            var questionIds = poll.Questions.Select(q => q.Id).ToList();

            // Check if a vote from this specific token already exists for any question in this poll
            bool alreadyVoted = await _context.Set<Vote>()
                .AnyAsync(v => questionIds.Contains(v.QuestionId) && v.VoterToken == voterToken);

            if (alreadyVoted && !string.IsNullOrEmpty(voterToken))
            {
                return false; // Reject the vote! They are trying to vote twice.
            }
            // ----------------------------------------------------

            // 2. Loop through each answer submitted by the user
            foreach (var answer in voteDto.Answers)
            {
                var question = poll.Questions.FirstOrDefault(q => q.Id == answer.QuestionId);

                if (question == null) continue;

                // Check validity
                bool isValid = (question.Type == QuestionType.OpenText)
                               ? !string.IsNullOrEmpty(answer.OpinionText)
                               : question.Options.Any(o => o.Id == answer.OptionId);

                if (isValid)
                {
                    var vote = new Vote
                    {
                        QuestionId = answer.QuestionId,
                        OptionId = answer.OptionId,
                        OpinionText = answer.OpinionText,

                        // FIX: Changed 'submitVoteDto' to 'voteDto' to match the method parameter!
                        VoterToken = voteDto.VoterName ?? string.Empty
                    };

                    // ONLY assign OptionId if it's not an OpenText question
                    if (question.Type != QuestionType.OpenText)
                    {
                        vote.OptionId = answer.OptionId;
                    }
                    else
                    {
                        // Explicitly set to null for OpenText, which is allowed by your 'Guid?' property
                        vote.OptionId = null;
                    }

                    _context.Set<Vote>().Add(vote);
                }
            }

            // 3. Commit all valid votes to the SQL database at once
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<PollResultDTO?> GetPollResultsAsync(string code)
        {
            var poll = await _context.Polls
                .Include(p => p.Questions)
                .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(p => p.Code == code);

            if (poll == null) return null;

            var questionIds = poll.Questions.Select(q => q.Id).ToList();

            // 1. Fetch choice-based votes
            var pollVotes = await _context.Set<Vote>()
                .Where(v => questionIds.Contains(v.QuestionId))
                .ToListAsync();

            // 2. We use the Vote table for OpenText answers as well
            var allVotes = pollVotes;

            var result = new PollResultDTO
            {
                Title = poll.Title,
                Description = poll.Description,
                TotalPollVotes = pollVotes.Select(v => v.VoterToken).Distinct().Count(),
                Questions = poll.Questions.OrderBy(q => q.Position).Select(q => new QuestionResultDTO
                {
                    Id = q.Id,
                    Text = q.Text,
                    Type = q.Type,

                    Options = q.Options.OrderBy(o => o.Position).Select(o => new OptionResultDTO
                    {
                        Id = o.Id,
                        Text = o.Text,
                        VoteCount = pollVotes.Count(v => v.OptionId == o.Id)
                    }).ToList(),

                    // FIX: Use OpinionText instead of Content
                    // We also filter out null/empty opinions so they don't show up in the UI
                    TextResponses = allVotes
                        .Where(v => v.QuestionId == q.Id && !string.IsNullOrEmpty(v.OpinionText))
                        .Select(v => v.OpinionText!)
                        .ToList() ?? new List<string>()
                }).ToList()
            };

            return result;
        }


        public async Task<List<PollResponseDTO>> GetPollsByCreatorAsync(string creatorId)
        {
            var polls = await _context.Polls
                .Where(p => p.CreatorId == creatorId)
                .Include(p => p.Questions)
                .ThenInclude(q => q.Options)
                .OrderByDescending(p => p.LaunchDate)
                .ToListAsync();

            return polls.Select(poll => new PollResponseDTO
            {
                Code = poll.Code,
                Title = poll.Title,
                Description = poll.Description,
                LaunchDate = poll.LaunchDate,
                Status = poll.Status.ToString(),
                Questions = poll.Questions.OrderBy(q => q.Position).Select(q => new QuestionResponseDTO
                {
                    Id = q.Id,
                    Text = q.Text,
                    Type = (int)q.Type,
                    Position = q.Position,
                    Options = q.Options.OrderBy(o => o.Position).Select(o => new OptionResponseDTO
                    {
                        Id = o.Id,
                        Text = o.Text,
                        Position = o.Position
                    }).ToList()
                }).ToList()
            }).ToList();
        }

        public async Task<bool> ClosePollAsync(string code, string creatorId)
        {
            var poll = await _context.Polls
                .FirstOrDefaultAsync(p => p.Code == code && p.CreatorId == creatorId);

            if (poll == null) return false;

            poll.IsActive = false;

            // FIX: Use your existing 'Ended' status here!
            poll.Status = PollStatus.Ended;

            await _context.SaveChangesAsync();

            return true;
        }

        // FIX: Changed 'Task' to 'Task'
        public async Task<bool> DeletePollAsync(string code, string creatorId)
        {
            var poll = await _context.Polls
                .FirstOrDefaultAsync(p => p.Code == code && p.CreatorId == creatorId);

            if (poll == null) return false;

            _context.Polls.Remove(poll);
            await _context.SaveChangesAsync();

            return true;
        }

        private string GenerateShortCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();

            return new string(Enumerable.Repeat(chars, 5)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public async Task<bool> HasUserVotedAsync(string pollCode, string voterToken)
        {
            var poll = await _context.Set<Poll>().FirstOrDefaultAsync(p => p.Code == pollCode);

            if (poll == null) return false;

            // Fixed: Changed v.VoterName to v.VoterToken to match your entity!
            return await _context.Set<Vote>()
                .AnyAsync(v => v.Question!.PollId == poll.Id && v.VoterToken == voterToken);
        }
    }
}