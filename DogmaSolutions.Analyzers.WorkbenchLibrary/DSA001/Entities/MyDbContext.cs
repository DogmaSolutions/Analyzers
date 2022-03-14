using Microsoft.EntityFrameworkCore;

namespace DogmaSolutions.Analyzers.MockLibrary.DSA001.Entities
{
    public class MyDbContext : DbContext
    {
        public virtual DbSet<MyEntity> MyEntities { get; set; }
    }
}