using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NewAgeUI.Models
{
  public class NewAgeDbContext : IdentityDbContext
  {
    public NewAgeDbContext(DbContextOptions<NewAgeDbContext> options) : base(options)
    {

    }

    public DbSet<Employee> Employees { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
      base.OnModelCreating(builder);

      var foreignKeys = builder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys());

      foreach (var foreignKey in foreignKeys)
      {
        foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
      }
    }
  }
}
