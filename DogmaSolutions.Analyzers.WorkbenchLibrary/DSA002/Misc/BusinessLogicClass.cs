using System.Collections.Generic;
using System.Linq;
using DogmaSolutions.Analyzers.MockLibrary.DSA002.Controllers;

namespace DogmaSolutions.Analyzers.MockLibrary.DSA002.Misc
{
    public class BusinessLogicClass 
    {
        private readonly MyDbContext _dbContext;

        public BusinessLogicClass(MyDbContext dbContext)
        {
            this._dbContext = dbContext;
        }

        public IEnumerable<MyEntity> GetAll0()
        {
            // this MUST NOT trigger an error
            var query = from entities in _dbContext.MyEntities where entities.Id > 0 select entities;
            return query.ToList(); 
        }

        public IEnumerable<long> GetAll1()
        {
            // this MUST NOT trigger an error
            var query = _dbContext.MyEntities.Where(entities => entities.Id > 0).Select(entities=>entities.Id);
            return query.ToList(); 
        }
    }
}