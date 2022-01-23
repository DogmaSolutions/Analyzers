using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = DogmaSolutions.Analyzers.Test.CSharpAnalyzerVerifier<DogmaSolutions.Analyzers.DSA002Analyzer>;

namespace DogmaSolutions.Analyzers.Test
{
    [TestClass]
    public class DSA002Tests
    {
        [TestMethod]
        public async Task Empty()
        {
            var test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task QueryExpressionSyntax_Ok()
        {
            var sourceCode = @"
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication1.Entities
{
    using Microsoft.EntityFrameworkCore;

    public class MyEntity
    {
        public long Id { get; set; }
    }
	
    public class MyDbContext : DbContext
    {
        public virtual DbSet<MyEntity> MyEntities { get; set; }
    }
}

namespace WebApplication1.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using WebApplication1.Entities;

    public class MyEntitiesController : Microsoft.AspNetCore.Mvc.ControllerBase
    {
        protected readonly WebApplication1.Entities.MyDbContext _dbContext;

        public MyEntitiesController(WebApplication1.Entities.MyDbContext dbContext)
        {
            this._dbContext = dbContext;
        }

        [HttpGet]
        public IEnumerable<MyEntity> GetAll0()
        {
            // this MUST NOT trigger an error
            var query = {|#0:from entities in _dbContext.MyEntities where entities.Id > 0 select entities|};
            return query.ToList(); 
        }

        [HttpPost]
        public IEnumerable<long> GetAll1()
        {
            // this MUST trigger an error
            var query = {|#1:_dbContext.MyEntities.Where(entities => entities.Id > 0).Select|}(entities=>entities.Id);
            return query.ToList(); 
        }
    }

    public class InheritedEntitiesController : MyEntitiesController
    {

        public InheritedEntitiesController(WebApplication1.Entities.MyDbContext dbContext) : base(dbContext)
        {
           
        }

        [HttpPost]
        public IEnumerable<long> GetAll2()
        {
            // this MUST trigger an error
            var query = {|#2:_dbContext.MyEntities.Where(entities => entities.Id > 0).Select|}(entities=>entities.Id);
            return query.ToList(); 
        }

        [HttpPost]
        public IEnumerable<MyEntity> GetAll3()
        {
            // this MUST NOT trigger an error
            var query = {|#3:from entities in _dbContext.MyEntities where entities.Id > 0 select entities|};
            return query.ToList(); 
        }
    }

    public class BusinessLogicClass 
    {
        private readonly WebApplication1.Entities.MyDbContext _dbContext;

        public BusinessLogicClass(WebApplication1.Entities.MyDbContext dbContext)
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
";
            var test = new VerifyCS.Test();
            test.TestCode = sourceCode;
            test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(ImmutableArray.Create(new PackageIdentity[]
            {
                new PackageIdentity("Microsoft.AspNetCore.Mvc", "2.2.0"),
                new PackageIdentity("Microsoft.EntityFrameworkCore", "3.1.22")
            }));


            test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(DSA002Analyzer.DiagnosticId)
                .WithMessage(
                    @"The WebApi method 'MyEntitiesController.GetAll1' is invoking the method 'Select' of the DbSet 'MyEntities' to directly manipulate data through a LINQ fluent query.
WebApi controllers should not contain data-manipulation business logics.
Move the data-manipulation business logics into a more appropriate class, or even better, an injected service.")
                .WithLocation(1));

            test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(DSA002Analyzer.DiagnosticId)
                .WithMessage(
                    @"The WebApi method 'MyEntitiesController.GetAll1' is invoking the method 'Where' of the DbSet 'MyEntities' to directly manipulate data through a LINQ fluent query.
WebApi controllers should not contain data-manipulation business logics.
Move the data-manipulation business logics into a more appropriate class, or even better, an injected service.")
                .WithLocation(1));

            test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(DSA002Analyzer.DiagnosticId)
                .WithMessage(
                    @"The WebApi method 'InheritedEntitiesController.GetAll2' is invoking the method 'Select' of the DbSet 'MyEntities' to directly manipulate data through a LINQ fluent query.
WebApi controllers should not contain data-manipulation business logics.
Move the data-manipulation business logics into a more appropriate class, or even better, an injected service.")
                .WithLocation(2));


            test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(DSA002Analyzer.DiagnosticId)
                .WithMessage(
                    @"The WebApi method 'InheritedEntitiesController.GetAll2' is invoking the method 'Where' of the DbSet 'MyEntities' to directly manipulate data through a LINQ fluent query.
WebApi controllers should not contain data-manipulation business logics.
Move the data-manipulation business logics into a more appropriate class, or even better, an injected service.")
                .WithLocation(2));

            await test.RunAsync();
        }
    }
}