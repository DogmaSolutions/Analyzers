using Microsoft.EntityFrameworkCore;

namespace DogmaSolutions.Analyzers.MockLibrary.DSA002.Controllers
{
    public class MyDbContext : DbContext
    {
        public virtual DbSet<MyEntity> MyEntities { get; set; }
    }
}