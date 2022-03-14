using System.Collections.Generic;
using System.Linq;
using DogmaSolutions.Analyzers.MockLibrary.DSA001.Entities;
using Microsoft.AspNetCore.Mvc;

namespace DogmaSolutions.Analyzers.MockLibrary.DSA001.Controllers
{
    public class MyEntitiesController : ControllerBase
    {
        protected MyDbContext DbContext { get; }

        public MyEntitiesController(MyDbContext dbContext)
        {
            DbContext = dbContext;
        }

        [HttpGet]
        public IEnumerable<MyEntity> GetAll0()
        {
            // this MUST trigger an error
            var query = from entities in DbContext.MyEntities where entities.Id > 0 select entities;
            return query.ToList(); 
        }

        [HttpPost]
        public IEnumerable<long> GetAll1()
        {
            // this MUST NOT trigger an error
            var query = DbContext.MyEntities.Where(entities => entities.Id > 0).Select(entities=>entities.Id);
            return query.ToList(); 
        }
    }
}