﻿using HemDotNetWebApi.DTO;
using HemDotNetWebApi.Models;

namespace HemDotNetWebApi.Data
{
    public interface IRealEstateAgentRepository
    {
        // CHRIS
        Task<RealEstateAgent> UpdateAsync(RealEstateAgent agent);
        
        // CHRIS
        Task<RealEstateAgent> GetAsync(string agentId);

        // CHRIS
        Task<IEnumerable<RealEstateAgent>> GetAllAsync();

        // Allan
        Task<RealEstateAgent> UpdateAgentAgencyAsync(string agentId, int newAgencyId);
        Task DeleteAsync(string agentId);
        Task<string> UploadAgentProfilePictureAsync(string agentId, IFormFile file);

    }
}
