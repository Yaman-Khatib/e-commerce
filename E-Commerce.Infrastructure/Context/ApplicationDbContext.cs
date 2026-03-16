using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using E_Commerce.Application.Shared;
using Microsoft.EntityFrameworkCore.Infrastructure;
namespace E_Commerce.Infrastructure.Context
{
    public class ApplicationDbContext(DbContextOptions options) : DbContext,IApplicationDbContext
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(GetType().Assembly);
        }
    }
}
