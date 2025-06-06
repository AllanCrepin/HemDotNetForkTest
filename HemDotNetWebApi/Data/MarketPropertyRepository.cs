using AutoMapper;
using AutoMapper.QueryableExtensions;
using HemDotNetWebApi.DTO;
using HemDotNetWebApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace HemDotNetWebApi.Data
{
    public class MarketPropertyRepository : IMarketPropertyRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public MarketPropertyRepository(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // Allan
        public async Task<MarketProperty> UpdateMarketProperty(MarketProperty marketProperty)
        {
            var existingProperty = await _context.MarketProperties
                .Include(mp => mp.Municipality)
                .Include(mp => mp.RealEstateAgent)
                .FirstOrDefaultAsync(mp => mp.MarketPropertyId == marketProperty.MarketPropertyId);

            if (existingProperty == null)
            {
                return null;
            }

            // We don't allow changing id
            marketProperty.MarketPropertyId = existingProperty.MarketPropertyId;

            //We keep the original creation date intact.

            marketProperty.CreationDate = existingProperty.CreationDate;

            //Co-Author: Johan. Added explicit reassign of navigation property.

            _context.Entry(existingProperty).CurrentValues.SetValues(marketProperty);


            if (marketProperty.Municipality != null)
            {
                var newMunicipality = await _context.Municipalities
                    .FirstOrDefaultAsync(m => m.MunicipalityId == marketProperty.Municipality.MunicipalityId);

                existingProperty.Municipality = newMunicipality;
            }

            //Commencted out, as Detachment caused troubles updating models from the Client. /Johan

            //// we say to EF: "forget about this object"
            //_context.Entry(existingProperty).State = EntityState.Detached;

            //// this tells EF: here's a new object representing a row in the database. Treat all its properties as changed,
            //// and update them in the database. Generated SQL Update query for this row
            //_context.Entry(marketProperty).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            return marketProperty;
        }

        //Author: Johan Ek
        public async Task<IEnumerable<PartialMarketPropertyDTO>> GetAllMarketPropertiesPartial()
        {
            //Gets all MarketProperties, eagerly loads Municipality and Images, then projects to a DTO.
            return await _context.MarketProperties
                .Include(mp => mp.Municipality)
                .Include(mp => mp.Images)
                .Where(p => p.IsActive == true)
                .Where(p => p.IsSold == false)
                .ProjectTo<PartialMarketPropertyDTO>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        // Allan
        public async Task<IEnumerable<MarketProperty>> GetAllByMunicipality(string municipality)
        {
            return await _context.MarketProperties
                .Include(p => p.Municipality)
                .Include(p => p.Images)
                .Where(p => p.IsActive == true)
                .Where(p => p.IsSold == false)
                .Where(p => p.Municipality.MunicipalityName == municipality).ToListAsync();
        }

        // CHRIS
        public async Task<IEnumerable<MarketProperty>> GetAllActiveByAgent(string agentId)
        {
            return await _context.MarketProperties
                .Include(p => p.Municipality)
                .Include (p => p.RealEstateAgent)
                .ThenInclude(mp => mp.RealEstateAgentAgency)
                .Include(p => p.Images)
                .Where(p => p.RealEstateAgent.Id == agentId)
                .Where(p => p.IsActive == true)
                .Where(p => p.IsSold == false)
                .ToListAsync();
        }

        // Adam
        public async Task<bool> AgentDelete(int propertyId, string agentId)
        {
            var property = await _context.MarketProperties
                .Include(p => p.RealEstateAgent)
                .FirstOrDefaultAsync(p => p.MarketPropertyId == propertyId && p.RealEstateAgent.Id == agentId);
            if (property == null)
            {
                return false;
            }
            _context.MarketProperties.Remove(property);
            await _context.SaveChangesAsync();
            return true;
        }

        // Allan
        public async Task<bool> IsPropertyOwnedByAgentAsync(int marketPropertyId, string agentUserId)
        {
            return await _context.MarketProperties
                .AnyAsync(p => p.MarketPropertyId == marketPropertyId && p.RealEstateAgent.Id == agentUserId);

        }

        // Katarina
        public async Task<MarketProperty?> GetMarketPropertyById(int id)
        {
            return await _context.MarketProperties
                .Include(mp => mp.Municipality)
                .Include(mp => mp.RealEstateAgent)
                .Include(mp => mp.Images)
                .FirstOrDefaultAsync(mp => mp.MarketPropertyId == id);
        }
       // Katarina
        public async Task<MarketProperty?> CreateMarketPropertyAsync(MarketProperty marketProperty)
        {
            //Co-Author: Johan. Assigned local variables for municipalityId and agentId, and edited linq expressions to use them.
            //EF Core seems to have issues populating the keys otherwise, since MarketProperty lacks MunicipalityId and RealEstateAgentId.
            var municipalityId = marketProperty.Municipality.MunicipalityId;
            var agentId = marketProperty.RealEstateAgent.Id;

            var municipality = await _context.Municipalities
                .FirstOrDefaultAsync(m => m.MunicipalityId == municipalityId);

            var agent = await _context.RealEstateAgents
                .FirstOrDefaultAsync(a => a.Id == agentId);

            if (municipality == null || agent == null)
            {
                return null;
            }

           
            marketProperty.Municipality = municipality;
            marketProperty.RealEstateAgent = agent;

            _context.MarketProperties.Add(marketProperty);
            await _context.SaveChangesAsync();

            return marketProperty;
        }

        // Allan
        public async Task<List<MarketProperty>> SearchMarketPropertiesAsync(MarketPropertySearchDto searchDto)
        {
            var query = _context.MarketProperties
                .Include(mp => mp.Municipality)
                .Include(mp => mp.RealEstateAgent)
                .ThenInclude(mp => mp.RealEstateAgentAgency)
                .Include(mp => mp.Images)
                .AsQueryable();

            
            if (!string.IsNullOrEmpty(searchDto.Area))
            {
                query = query.Where(mp => mp.Municipality.MunicipalityName.Contains(searchDto.Area));
            }
            

            if (searchDto.SelectedTypes != null && searchDto.SelectedTypes.Any())
            {
                query = query.Where(mp => searchDto.SelectedTypes.Contains(mp.Category));
            }
            

            if (searchDto.MinRooms.HasValue)
            {
                query = query.Where(mp => mp.AmountOfRooms >= searchDto.MinRooms.Value);
            }

            if (searchDto.MinArea.HasValue)
            {
                query = query.Where(mp => mp.LivingArea >= searchDto.MinArea.Value);
            }

            if (searchDto.MaxPrice.HasValue)
            {
                query = query.Where(mp => mp.Price <= searchDto.MaxPrice.Value);
            }


            if (searchDto.NewProduction.HasValue)
            {
                if (searchDto.NewProduction.Value)
                {
                    query = query.Where(mp => mp.ConstructionYear >= DateTime.UtcNow.Year - 5);
                }
            }
           

            query = query.Where(mp => mp.IsActive && !mp.IsSold);

            return await query.ToListAsync();
        }

    }
}
