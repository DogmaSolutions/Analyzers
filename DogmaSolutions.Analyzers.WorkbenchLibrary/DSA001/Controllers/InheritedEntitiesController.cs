using System.Collections.Generic;
using System.Linq;
using DogmaSolutions.Analyzers.MockLibrary.DSA001.Entities;
using Microsoft.AspNetCore.Mvc;

namespace DogmaSolutions.Analyzers.MockLibrary.DSA001.Controllers
{
    public class InheritedEntitiesController : MyEntitiesController
    {

        public InheritedEntitiesController(MyDbContext dbContext) : base(dbContext)
        {
           
        }

        [HttpPost]
        public IEnumerable<long> GetAll2()
        {
            // this MUST NOT trigger an error
            var query = DbContext.MyEntities.Where(entities => entities.Id > 0).Select(entities=>entities.Id);
            return query.ToList(); 
        }

        [HttpPost]
        public IEnumerable<MyEntity> GetAll3()
        {
            // this MUST trigger an error
            var query = from entities in DbContext.MyEntities where entities.Id > 0 select entities;
            return query.ToList(); 
        }
    }
}