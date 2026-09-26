using System.Collections.Generic;
using System.Threading.Tasks;
using SDP1.Models;

namespace SDP1.Services
{
    public interface IAIService
    {
        Task<AIRecommendationResultViewModel> GetRecommendationsAsync(
            string userQuery,
            DisabilityUser? userProfile,
            List<CandidateOpportunity> candidates);
    }
}
