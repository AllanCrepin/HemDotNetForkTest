﻿using HemDotNetWebApi.Constants;
using HemDotNetWebApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace HemDotNetWebApi.Data
{
    public class ApplicationDbContext : IdentityDbContext<RealEstateAgent>
    {
        // Author: All
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<MarketProperty> MarketProperties { get; set; }
        public DbSet<Municipality> Municipalities { get; set; }
        public DbSet<PropertyImage> PropertyImages { get; set; }
        public DbSet<RealEstateAgency> RealEstateAgencies { get; set; }
        public DbSet<RealEstateAgent> RealEstateAgents { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<RealEstateAgent>()
                .HasOne(agent => agent.RealEstateAgentAgency)
                .WithMany(agency => agency.RealEstateAgencyAgents)
                .HasForeignKey(agent => agent.RealEstateAgentAgencyId)
                .OnDelete(DeleteBehavior.Restrict);

        }

    }
}
