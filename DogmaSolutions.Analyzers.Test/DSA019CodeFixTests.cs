using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public class DSA019CodeFixTests
{
    [TestMethod]
    public async Task ExtractsRepeatedChainWithDifferentIndexer()
    {
        var source = @"
namespace TestApp
{
    public class Light { public bool IsOn() => true; }
    public class Room { public Light[] Lights; }
    public class Rooms { public Room Bathroom; }
    public class Home { public Rooms Rooms; }
    public class MyService
    {
        public void Process(Home home)
        {
            var status = new
            {
                Primary = {|#0:home.Rooms.Bathroom.Lights|}[0].IsOn(),
                Secondary = {|#1:home.Rooms.Bathroom.Lights|}[1].IsOn(),
            };
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class Light { public bool IsOn() => true; }
    public class Room { public Light[] Lights; }
    public class Rooms { public Room Bathroom; }
    public class Home { public Rooms Rooms; }
    public class MyService
    {
        public void Process(Home home)
        {
            var lights = home.Rooms.Bathroom.Lights;
            var status = new
            {
                Primary = lights[0].IsOn(),
                Secondary = lights[1].IsOn(),
            };
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("home.Rooms.Bathroom.Lights", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("home.Rooms.Bathroom.Lights", 2));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtractsRepeatedChainWithDifferentTerminalProperty()
    {
        var source = @"
namespace TestApp
{
    public class DbSettings { public string ConnectionString; public int Timeout; }
    public class Infra { public DbSettings Database; }
    public class Settings { public Infra Infrastructure; }
    public class AppConfig { public Settings Settings; }
    public class MyService
    {
        public void Process(AppConfig config)
        {
            var connStr = {|#0:config.Settings.Infrastructure.Database|}.ConnectionString;
            var timeout = {|#1:config.Settings.Infrastructure.Database|}.Timeout;
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class DbSettings { public string ConnectionString; public int Timeout; }
    public class Infra { public DbSettings Database; }
    public class Settings { public Infra Infrastructure; }
    public class AppConfig { public Settings Settings; }
    public class MyService
    {
        public void Process(AppConfig config)
        {
            var database = config.Settings.Infrastructure.Database;
            var connStr = database.ConnectionString;
            var timeout = database.Timeout;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("config.Settings.Infrastructure.Database", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("config.Settings.Infrastructure.Database", 2));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtractsThreeOccurrences()
    {
        var source = @"
namespace TestApp
{
    public class Address { public string Street; public string City; public string Zip; }
    public class Profile { public Address Address; }
    public class Contact { public Profile Profile; }
    public class Customer { public Contact Contact; }
    public class MyService
    {
        public void Process(Customer customer)
        {
            var street = {|#0:customer.Contact.Profile.Address|}.Street;
            var city = {|#1:customer.Contact.Profile.Address|}.City;
            var zip = {|#2:customer.Contact.Profile.Address|}.Zip;
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class Address { public string Street; public string City; public string Zip; }
    public class Profile { public Address Address; }
    public class Contact { public Profile Profile; }
    public class Customer { public Contact Contact; }
    public class MyService
    {
        public void Process(Customer customer)
        {
            var address = customer.Contact.Profile.Address;
            var street = address.Street;
            var city = address.City;
            var zip = address.Zip;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("customer.Contact.Profile.Address", 3));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("customer.Contact.Profile.Address", 3));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(2).WithArguments("customer.Contact.Profile.Address", 3));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ResolvesNameConflictWithExistingVariable()
    {
        var source = @"
namespace TestApp
{
    public class Deep { public int X; public int Y; }
    public class Inner { public Deep Deep; }
    public class Middle { public Inner Inner; }
    public class Outer { public Middle Middle; }
    public class MyService
    {
        public void Process(Outer outer)
        {
            var deep = ""already taken"";
            var x = {|#0:outer.Middle.Inner.Deep|}.X;
            var y = {|#1:outer.Middle.Inner.Deep|}.Y;
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class Deep { public int X; public int Y; }
    public class Inner { public Deep Deep; }
    public class Middle { public Inner Inner; }
    public class Outer { public Middle Middle; }
    public class MyService
    {
        public void Process(Outer outer)
        {
            var deep = ""already taken"";
            var deep1 = outer.Middle.Inner.Deep;
            var x = deep1.X;
            var y = deep1.Y;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("outer.Middle.Inner.Deep", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("outer.Middle.Inner.Deep", 2));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtractsInExpressionBodyLambda()
    {
        var source = @"
using System.Linq;
namespace TestApp
{
    public class ItemDetail { public string Name; public decimal Price; }
    public class ItemSpec { public ItemDetail Detail; }
    public class ItemCategory { public ItemSpec Spec; }
    public class CatalogEntry { public ItemCategory Category; }
    public class MyService
    {
        public void Process(CatalogEntry[] entries)
        {
            var result = entries.Select(e => new
            {
                Name = {|#0:e.Category.Spec.Detail|}.Name,
                Price = {|#1:e.Category.Spec.Detail|}.Price,
            });
        }
    }
}";

        var fixedSource = @"
using System.Linq;
namespace TestApp
{
    public class ItemDetail { public string Name; public decimal Price; }
    public class ItemSpec { public ItemDetail Detail; }
    public class ItemCategory { public ItemSpec Spec; }
    public class CatalogEntry { public ItemCategory Category; }
    public class MyService
    {
        public void Process(CatalogEntry[] entries)
        {
            var result = entries.Select(e => {
    var detail = e.Category.Spec.Detail;
    return new
    {
        Name = detail.Name,
        Price = detail.Price,
    };
});
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("e.Category.Spec.Detail", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("e.Category.Spec.Detail", 2));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtractsChainWithThisPrefix()
    {
        var source = @"
namespace TestApp
{
    public class Deep { public int X; public int Y; }
    public class Inner { public Deep Deep; }
    public class Middle { public Inner Inner; }
    public class MyService
    {
        private Middle _field;
        public void Process()
        {
            var x = {|#0:this._field.Inner.Deep|}.X;
            var y = {|#1:this._field.Inner.Deep|}.Y;
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class Deep { public int X; public int Y; }
    public class Inner { public Deep Deep; }
    public class Middle { public Inner Inner; }
    public class MyService
    {
        private Middle _field;
        public void Process()
        {
            var deep = this._field.Inner.Deep;
            var x = deep.X;
            var y = deep.Y;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("this._field.Inner.Deep", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("this._field.Inner.Deep", 2));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task InsertsBeforeEarliestUsageNotFirstStatement()
    {
        var source = @"
namespace TestApp
{
    public class Deep { public int X; public int Y; }
    public class Inner { public Deep Deep; }
    public class Middle { public Inner Inner; }
    public class Outer { public Middle Middle; }
    public class MyService
    {
        public void Process(Outer outer)
        {
            var unrelated = 42;
            System.Console.WriteLine(unrelated);
            var x = {|#0:outer.Middle.Inner.Deep|}.X;
            var y = {|#1:outer.Middle.Inner.Deep|}.Y;
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class Deep { public int X; public int Y; }
    public class Inner { public Deep Deep; }
    public class Middle { public Inner Inner; }
    public class Outer { public Middle Middle; }
    public class MyService
    {
        public void Process(Outer outer)
        {
            var unrelated = 42;
            System.Console.WriteLine(unrelated);
            var deep = outer.Middle.Inner.Deep;
            var x = deep.X;
            var y = deep.Y;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("outer.Middle.Inner.Deep", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("outer.Middle.Inner.Deep", 2));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtractsFullInvocationWhenMemberAccessIsMethodCall()
    {
        var source = @"
using System.Data;
namespace TestApp
{
    public class MyService
    {
        public void Process(DataTable dt)
        {
            var a = {|#2:{|#0:dt.Rows[0][""COL_A""]|}.ToString|}();
            var b = {|#3:{|#1:dt.Rows[0][""COL_A""]|}.ToString|}();
        }
    }
}";

        // The iterative codefix extracts the ElementAccess first (shallower),
        // which resolves both the ElementAccess and MemberAccess diagnostics.
        var fixedSource = @"
using System.Data;
namespace TestApp
{
    public class MyService
    {
        public void Process(DataTable dt)
        {
            var rows = dt.Rows[0][""COL_A""];
            var a = rows.ToString();
            var b = rows.ToString();
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        // Diagnostics sorted by span: ElementAccess before MemberAccess on each line
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments(@"dt.Rows[0][""COL_A""]", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(2).WithArguments(@"dt.Rows[0][""COL_A""].ToString", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments(@"dt.Rows[0][""COL_A""]", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(3).WithArguments(@"dt.Rows[0][""COL_A""].ToString", 2));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtractsMethodCallWithArguments()
    {
        var source = @"
namespace TestApp
{
    public class Formatter { public string Format(string fmt) => null; }
    public class Options { public Formatter Formatter; }
    public class Config { public Options Options; }
    public class MyService
    {
        public void Process(Config config)
        {
            var a = {|#0:config.Options.Formatter.Format|}(""A"");
            var b = {|#1:config.Options.Formatter.Format|}(""A"");
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class Formatter { public string Format(string fmt) => null; }
    public class Options { public Formatter Formatter; }
    public class Config { public Options Options; }
    public class MyService
    {
        public void Process(Config config)
        {
            var format = config.Options.Formatter.Format(""A"");
            var a = format;
            var b = format;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("config.Options.Formatter.Format", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("config.Options.Formatter.Format", 2));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtractsChainFromAssignmentTarget()
    {
        var source = @"
namespace TestApp
{
    public class Deep { public int X; public int Y; }
    public class Inner { public Deep Deep; }
    public class Middle { public Inner Inner; }
    public class Outer { public Middle Middle; }
    public class MyService
    {
        public void Process(Outer outer)
        {
            {|#0:outer.Middle.Inner.Deep|}.X = 1;
            {|#1:outer.Middle.Inner.Deep|}.Y = 2;
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class Deep { public int X; public int Y; }
    public class Inner { public Deep Deep; }
    public class Middle { public Inner Inner; }
    public class Outer { public Middle Middle; }
    public class MyService
    {
        public void Process(Outer outer)
        {
            var deep = outer.Middle.Inner.Deep;
            deep.X = 1;
            deep.Y = 2;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("outer.Middle.Inner.Deep", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("outer.Middle.Inner.Deep", 2));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtractsChainFromMethodArguments()
    {
        var source = @"
namespace TestApp
{
    public class Deep { public int X; public int Y; }
    public class Inner { public Deep Deep; }
    public class Middle { public Inner Inner; }
    public class Outer { public Middle Middle; }
    public class MyService
    {
        private void Use(int a, int b) { }
        public void Process(Outer outer)
        {
            Use({|#0:outer.Middle.Inner.Deep|}.X, {|#1:outer.Middle.Inner.Deep|}.Y);
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class Deep { public int X; public int Y; }
    public class Inner { public Deep Deep; }
    public class Middle { public Inner Inner; }
    public class Outer { public Middle Middle; }
    public class MyService
    {
        private void Use(int a, int b) { }
        public void Process(Outer outer)
        {
            var deep = outer.Middle.Inner.Deep;
            Use(deep.X, deep.Y);
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("outer.Middle.Inner.Deep", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("outer.Middle.Inner.Deep", 2));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtractsChainFromObjectInitializer()
    {
        var source = @"
namespace TestApp
{
    public class Deep { public int X; public int Y; }
    public class Inner { public Deep Deep; }
    public class Middle { public Inner Inner; }
    public class Outer { public Middle Middle; }
    public class Dto { public int A; public int B; }
    public class MyService
    {
        public void Process(Outer outer)
        {
            var dto = new Dto
            {
                A = {|#0:outer.Middle.Inner.Deep|}.X,
                B = {|#1:outer.Middle.Inner.Deep|}.Y,
            };
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class Deep { public int X; public int Y; }
    public class Inner { public Deep Deep; }
    public class Middle { public Inner Inner; }
    public class Outer { public Middle Middle; }
    public class Dto { public int A; public int B; }
    public class MyService
    {
        public void Process(Outer outer)
        {
            var deep = outer.Middle.Inner.Deep;
            var dto = new Dto
            {
                A = deep.X,
                B = deep.Y,
            };
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("outer.Middle.Inner.Deep", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("outer.Middle.Inner.Deep", 2));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtractsReceiverWhenMethodCallsHaveDifferentArguments()
    {
        var source = @"
namespace TestApp
{
    public class Cell { public string ToValue(string fallback) => null; }
    public class Row { public Cell this[string key] => null; }
    public class RowCollection { public Row this[int index] => null; }
    public class DataTable { public RowCollection Rows; }
    public class MyService
    {
        public void Process(DataTable dt)
        {
            var a = {|#2:{|#0:dt.Rows[0][""TheKey""]|}.ToValue|}(""fallback1"");
            var b = {|#3:{|#1:dt.Rows[0][""TheKey""]|}.ToValue|}(""fallback2"");
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class Cell { public string ToValue(string fallback) => null; }
    public class Row { public Cell this[string key] => null; }
    public class RowCollection { public Row this[int index] => null; }
    public class DataTable { public RowCollection Rows; }
    public class MyService
    {
        public void Process(DataTable dt)
        {
            var rows = dt.Rows[0][""TheKey""];
            var a = rows.ToValue(""fallback1"");
            var b = rows.ToValue(""fallback2"");
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        // Diagnostics sorted by span: ElementAccess before MemberAccess on each line
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments(@"dt.Rows[0][""TheKey""]", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(2).WithArguments(@"dt.Rows[0][""TheKey""].ToValue", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments(@"dt.Rows[0][""TheKey""]", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(3).WithArguments(@"dt.Rows[0][""TheKey""].ToValue", 2));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtractsCommonElementAccessPrefixWithDifferentProperties()
    {
        var source = @"
namespace TestApp
{
    public class Cell { public string Value1; public string Value2; }
    public class Row { public Cell this[string key] => null; }
    public class RowCollection { public Row this[int index] => null; }
    public class DataTable { public RowCollection Rows; }
    public class MyService
    {
        public void Process(DataTable dt)
        {
            var a = {|#0:dt.Rows[0][""TheKey""]|}.Value1;
            var b = {|#1:dt.Rows[0][""TheKey""]|}.Value2;
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class Cell { public string Value1; public string Value2; }
    public class Row { public Cell this[string key] => null; }
    public class RowCollection { public Row this[int index] => null; }
    public class DataTable { public RowCollection Rows; }
    public class MyService
    {
        public void Process(DataTable dt)
        {
            var rows = dt.Rows[0][""TheKey""];
            var a = rows.Value1;
            var b = rows.Value2;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments(@"dt.Rows[0][""TheKey""]", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments(@"dt.Rows[0][""TheKey""]", 2));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task DifferentMethodArguments_VariableNamedAfterReceiver()
    {
        var source = @"
namespace TestApp
{
    public class Folder { public void SetReadOnly(string a, string b, bool c) {} }
    public class MyHandler
    {
        public static MyHandler Instance;
        public Folder CommonFolder;
    }
    public class MyService
    {
        public void Process(string fileName)
        {
            {|#0:MyHandler.Instance.CommonFolder.SetReadOnly|}(string.Empty, fileName, false);
            {|#1:MyHandler.Instance.CommonFolder.SetReadOnly|}(string.Empty, fileName, true);
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class Folder { public void SetReadOnly(string a, string b, bool c) {} }
    public class MyHandler
    {
        public static MyHandler Instance;
        public Folder CommonFolder;
    }
    public class MyService
    {
        public void Process(string fileName)
        {
            var commonFolder = MyHandler.Instance.CommonFolder;
            commonFolder.SetReadOnly(string.Empty, fileName, false);
            commonFolder.SetReadOnly(string.Empty, fileName, true);
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("MyHandler.Instance.CommonFolder.SetReadOnly", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("MyHandler.Instance.CommonFolder.SetReadOnly", 2));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtractsInsideForeachWhenExpressionReferencesIterationVariable()
    {
        var source = @"
namespace TestApp
{
    public class Attr { public string Value; }
    public class AttrMap
    {
        public Attr this[string name] => null;
    }
    public class Node { public AttrMap Attributes; }
    public class MyService
    {
        public void Process(Node[] nodes)
        {
            foreach (var nod in nodes)
            {
                var name = {|#0:nod.Attributes[""name""].Value|};
                var type = {|#1:nod.Attributes[""name""].Value|};
            }
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class Attr { public string Value; }
    public class AttrMap
    {
        public Attr this[string name] => null;
    }
    public class Node { public AttrMap Attributes; }
    public class MyService
    {
        public void Process(Node[] nodes)
        {
            foreach (var nod in nodes)
            {
                var value = nod.Attributes[""name""].Value;
                var name = value;
                var type = value;
            }
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments(@"nod.Attributes[""name""].Value", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments(@"nod.Attributes[""name""].Value", 2));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtractsInsideForLoopWhenExpressionReferencesLoopVariable()
    {
        var source = @"
namespace TestApp
{
    public class Deep { public string Name; }
    public class Container { public Deep[] Items; }
    public class MyService
    {
        public void Process(Container container)
        {
            for (int i = 0; i < 10; i++)
            {
                var a = {|#0:container.Items[i].Name|};
                var b = {|#1:container.Items[i].Name|};
            }
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class Deep { public string Name; }
    public class Container { public Deep[] Items; }
    public class MyService
    {
        public void Process(Container container)
        {
            for (int i = 0; i < 10; i++)
            {
                var name = container.Items[i].Name;
                var a = name;
                var b = name;
            }
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("container.Items[i].Name", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("container.Items[i].Name", 2));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtractsCommonRootWhenSiblingChainsSharePrefix()
    {
        var source = @"
namespace TestApp
{
    public class Machine { public string Name; public int Id; }
    public class MachineVersion { public Machine Machine; }
    public class Connection { public MachineVersion MachineVersion; }
    public class MyService
    {
        public void Process(Connection conn)
        {
            var name1 = {|#0:conn.MachineVersion.Machine.Name|};
            var id1 = {|#1:conn.MachineVersion.Machine.Id|};
            var name2 = {|#2:conn.MachineVersion.Machine.Name|};
            var id2 = {|#3:conn.MachineVersion.Machine.Id|};
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class Machine { public string Name; public int Id; }
    public class MachineVersion { public Machine Machine; }
    public class Connection { public MachineVersion MachineVersion; }
    public class MyService
    {
        public void Process(Connection conn)
        {
            var machine = conn.MachineVersion.Machine;
            var name1 = machine.Name;
            var id1 = machine.Id;
            var name2 = machine.Name;
            var id2 = machine.Id;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("conn.MachineVersion.Machine.Name", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("conn.MachineVersion.Machine.Id", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(2).WithArguments("conn.MachineVersion.Machine.Name", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(3).WithArguments("conn.MachineVersion.Machine.Id", 2));

        test.CodeActionEquivalenceKey = DSA019Analyzer.DiagnosticId + "_root";

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtractsCommonRootAcrossTryCatch()
    {
        var source = @"
namespace TestApp
{
    public class Machine { public string Name; public int Id; }
    public class MachineVersion { public Machine Machine; }
    public class Connection { public MachineVersion MachineVersion; }
    public class MyService
    {
        public void Process(Connection conn)
        {
            try
            {
                var name = {|#0:conn.MachineVersion.Machine.Name|};
                var id = {|#1:conn.MachineVersion.Machine.Id|};
            }
            catch
            {
                var name = {|#2:conn.MachineVersion.Machine.Name|};
                var id = {|#3:conn.MachineVersion.Machine.Id|};
            }
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class Machine { public string Name; public int Id; }
    public class MachineVersion { public Machine Machine; }
    public class Connection { public MachineVersion MachineVersion; }
    public class MyService
    {
        public void Process(Connection conn)
        {
            var machine = conn.MachineVersion.Machine;
            try
            {
                var name = machine.Name;
                var id = machine.Id;
            }
            catch
            {
                var name = machine.Name;
                var id = machine.Id;
            }
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("conn.MachineVersion.Machine.Name", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("conn.MachineVersion.Machine.Id", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(2).WithArguments("conn.MachineVersion.Machine.Name", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(3).WithArguments("conn.MachineVersion.Machine.Id", 2));

        test.CodeActionEquivalenceKey = DSA019Analyzer.DiagnosticId + "_root";

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtractsCommonRootWithThreeTerminals()
    {
        var source = @"
namespace TestApp
{
    public class Address { public string Street; public string City; public string Zip; }
    public class Profile { public Address Home; public Address Work; }
    public class Contact { public Profile Profile; }
    public class Customer { public Contact Contact; }
    public class MyService
    {
        public void Process(Customer customer)
        {
            var homeStreet = {|#0:customer.Contact.Profile.Home|}.Street;
            var homeCity = {|#1:customer.Contact.Profile.Home|}.City;
            var workStreet = {|#2:customer.Contact.Profile.Work|}.Street;
            var workCity = {|#3:customer.Contact.Profile.Work|}.City;
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class Address { public string Street; public string City; public string Zip; }
    public class Profile { public Address Home; public Address Work; }
    public class Contact { public Profile Profile; }
    public class Customer { public Contact Contact; }
    public class MyService
    {
        public void Process(Customer customer)
        {
            var profile = customer.Contact.Profile;
            var homeStreet = profile.Home.Street;
            var homeCity = profile.Home.City;
            var workStreet = profile.Work.Street;
            var workCity = profile.Work.City;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("customer.Contact.Profile.Home", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("customer.Contact.Profile.Home", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(2).WithArguments("customer.Contact.Profile.Work", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(3).WithArguments("customer.Contact.Profile.Work", 2));

        test.CodeActionEquivalenceKey = DSA019Analyzer.DiagnosticId + "_root";

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExactFixStillWorksWhenRootFixIsAvailable()
    {
        var source = @"
namespace TestApp
{
    public class Machine { public string Name; public int Id; }
    public class MachineVersion { public Machine Machine; }
    public class Connection { public MachineVersion MachineVersion; }
    public class MyService
    {
        public void Process(Connection conn)
        {
            var name1 = {|#0:conn.MachineVersion.Machine.Name|};
            var id1 = {|#1:conn.MachineVersion.Machine.Id|};
            var name2 = {|#2:conn.MachineVersion.Machine.Name|};
            var id2 = {|#3:conn.MachineVersion.Machine.Id|};
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class Machine { public string Name; public int Id; }
    public class MachineVersion { public Machine Machine; }
    public class Connection { public MachineVersion MachineVersion; }
    public class MyService
    {
        public void Process(Connection conn)
        {
            var name = conn.MachineVersion.Machine.Name;
            var name1 = name;
            var id = conn.MachineVersion.Machine.Id;
            var id1 = id;
            var name2 = name;
            var id2 = id;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("conn.MachineVersion.Machine.Name", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("conn.MachineVersion.Machine.Id", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(2).WithArguments("conn.MachineVersion.Machine.Name", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(3).WithArguments("conn.MachineVersion.Machine.Id", 2));

        test.CodeActionEquivalenceKey = DSA019Analyzer.DiagnosticId;

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task DoesNotDuplicateRegionDirective()
    {
        var source = @"
namespace TestApp
{
    public class Data { public int Value; }
    public class Settings { public Data Data; }
    public class Config { public Settings Settings; }
    public class MyClass
    {
        public void MyMethod(Config config)
        {
#region Processing
            var x = {|#0:config.Settings.Data.Value|};
            var y = {|#1:config.Settings.Data.Value|};
#endregion
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class Data { public int Value; }
    public class Settings { public Data Data; }
    public class Config { public Settings Settings; }
    public class MyClass
    {
        public void MyMethod(Config config)
        {
            var value = config.Settings.Data.Value;
#region Processing
            var x = value;
            var y = value;
#endregion
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("config.Settings.Data.Value", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("config.Settings.Data.Value", 2));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtractsMemberAccessUsedAsElementAccessPrefix()
    {
        var source = @"
namespace TestApp
{
    public class StepInfo { public string Code; public string Name; public string Tag; }
    public class GroupInfo { public string Id; public StepInfo[] Steps; public string Extra; }
    public class Container { public string Label; public GroupInfo[] Groups; }
    public static class Check
    {
        public static void AreEqual(object a, object b) { }
        public static void One(object a) { }
    }
    public class MyTests
    {
        public void Verify()
        {
            var container = new Container();
            var code = ""C1"";
            var name = ""N1"";
            var tag = ""T1"";
            var extra = ""E1"";

            Check.One({|#0:container.Groups[0].Steps|});
            Check.AreEqual(code, {|#4:{|#1:container.Groups[0].Steps|}[0]|}.Code);
            Check.AreEqual(name, {|#5:{|#2:container.Groups[0].Steps|}[0]|}.Name);
            Check.AreEqual(tag, {|#6:{|#3:container.Groups[0].Steps|}[0]|}.Tag);
            Check.AreEqual(extra, container.Groups[0].Extra);
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class StepInfo { public string Code; public string Name; public string Tag; }
    public class GroupInfo { public string Id; public StepInfo[] Steps; public string Extra; }
    public class Container { public string Label; public GroupInfo[] Groups; }
    public static class Check
    {
        public static void AreEqual(object a, object b) { }
        public static void One(object a) { }
    }
    public class MyTests
    {
        public void Verify()
        {
            var container = new Container();
            var code = ""C1"";
            var name = ""N1"";
            var tag = ""T1"";
            var extra = ""E1"";

            var steps = container.Groups[0].Steps;

            Check.One(steps);
            Check.AreEqual(code, steps[0].Code);
            Check.AreEqual(name, steps[0].Name);
            Check.AreEqual(tag, steps[0].Tag);
            Check.AreEqual(extra, container.Groups[0].Extra);
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("container.Groups[0].Steps", 4));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("container.Groups[0].Steps", 4));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(4).WithArguments("container.Groups[0].Steps[0]", 3));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(2).WithArguments("container.Groups[0].Steps", 4));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(5).WithArguments("container.Groups[0].Steps[0]", 3));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(3).WithArguments("container.Groups[0].Steps", 4));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(6).WithArguments("container.Groups[0].Steps[0]", 3));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtractsMemberAccessOnlyAsElementAccessPrefixWithDifferentIndices()
    {
        var source = @"
namespace TestApp
{
    public class StepInfo { public string Code; public string Name; }
    public class GroupInfo { public StepInfo[] Steps; }
    public class Container { public GroupInfo[] Groups; }
    public class MyTests
    {
        public void Verify(Container container)
        {
            var a = {|#3:{|#0:container.Groups[0].Steps|}[0]|}.Code;
            var b = {|#1:container.Groups[0].Steps|}[1].Code;
            var c = {|#4:{|#2:container.Groups[0].Steps|}[0]|}.Name;
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class StepInfo { public string Code; public string Name; }
    public class GroupInfo { public StepInfo[] Steps; }
    public class Container { public GroupInfo[] Groups; }
    public class MyTests
    {
        public void Verify(Container container)
        {
            var steps = container.Groups[0].Steps;
            var a = steps[0].Code;
            var b = steps[1].Code;
            var c = steps[0].Name;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("container.Groups[0].Steps", 3));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(3).WithArguments("container.Groups[0].Steps[0]", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("container.Groups[0].Steps", 3));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(2).WithArguments("container.Groups[0].Steps", 3));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(4).WithArguments("container.Groups[0].Steps[0]", 2));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtractsMemberAccessWithIntermediateIndexerInAssignments()
    {
        var source = @"
namespace TestApp
{
    public class StepInfo { public string Code; public string Name; }
    public class GroupInfo { public StepInfo[] Steps; }
    public class Container { public GroupInfo[] Groups; }
    public class MyTests
    {
        public void Update(Container container)
        {
            {|#2:{|#0:container.Groups[0].Steps|}[0]|}.Code = ""A"";
            {|#3:{|#1:container.Groups[0].Steps|}[0]|}.Name = ""B"";
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class StepInfo { public string Code; public string Name; }
    public class GroupInfo { public StepInfo[] Steps; }
    public class Container { public GroupInfo[] Groups; }
    public class MyTests
    {
        public void Update(Container container)
        {
            var steps = container.Groups[0].Steps;
            steps[0].Code = ""A"";
            steps[0].Name = ""B"";
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("container.Groups[0].Steps", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(2).WithArguments("container.Groups[0].Steps[0]", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("container.Groups[0].Steps", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(3).WithArguments("container.Groups[0].Steps[0]", 2));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtractsNestedDoubleElementAccessChain()
    {
        var source = @"
namespace TestApp
{
    public class Cell { public string Value; public string Label; }
    public class Sheet { public Cell[][] Rows; }
    public class Workbook { public Sheet[] Sheets; }
    public class MyTests
    {
        public void Verify(Workbook data)
        {
            var v = {|#4:{|#2:{|#0:data.Sheets[0].Rows|}[1]|}.Length|};
            var l = {|#5:{|#3:{|#1:data.Sheets[0].Rows|}[1]|}.Length|};
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class Cell { public string Value; public string Label; }
    public class Sheet { public Cell[][] Rows; }
    public class Workbook { public Sheet[] Sheets; }
    public class MyTests
    {
        public void Verify(Workbook data)
        {
            var rows = data.Sheets[0].Rows;
            var v = rows[1].Length;
            var l = rows[1].Length;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("data.Sheets[0].Rows", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(2).WithArguments("data.Sheets[0].Rows[1]", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(4).WithArguments("data.Sheets[0].Rows[1].Length", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("data.Sheets[0].Rows", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(3).WithArguments("data.Sheets[0].Rows[1]", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(5).WithArguments("data.Sheets[0].Rows[1].Length", 2));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtractsMemberAccessAsElementAccessPrefixInMixedContexts()
    {
        var source = @"
namespace TestApp
{
    public class StepInfo { public string Code; public string Name; }
    public class GroupInfo { public StepInfo[] Steps; }
    public class Container { public GroupInfo[] Groups; }
    public static class Util { public static string Format(string s) => s; }
    public class MyTests
    {
        public void Process(Container container)
        {
            var code = Util.Format({|#3:{|#0:container.Groups[0].Steps|}[0]|}.Code);
            {|#4:{|#1:container.Groups[0].Steps|}[0]|}.Name = ""updated"";
            var items = {|#2:container.Groups[0].Steps|};
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class StepInfo { public string Code; public string Name; }
    public class GroupInfo { public StepInfo[] Steps; }
    public class Container { public GroupInfo[] Groups; }
    public static class Util { public static string Format(string s) => s; }
    public class MyTests
    {
        public void Process(Container container)
        {
            var steps = container.Groups[0].Steps;
            var code = Util.Format(steps[0].Code);
            steps[0].Name = ""updated"";
            var items = steps;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("container.Groups[0].Steps", 3));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(3).WithArguments("container.Groups[0].Steps[0]", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("container.Groups[0].Steps", 3));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(4).WithArguments("container.Groups[0].Steps[0]", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(2).WithArguments("container.Groups[0].Steps", 3));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task CodeFixTitle_ContainsTargetExpression()
    {
        var source = @"
namespace TestApp
{
    public class Deep { public int X; public int Y; }
    public class Inner { public Deep Deep; }
    public class Middle { public Inner Inner; }
    public class Outer { public Middle Middle; }
    public class MyService
    {
        public void Process(Outer outer)
        {
            var x = outer.Middle.Inner.Deep.X;
            var y = outer.Middle.Inner.Deep.Y;
        }
    }
}";

        var runtimeDir = RuntimeEnvironment.GetRuntimeDirectory();
        var references = new MetadataReference[]
        {
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Private.CoreLib.dll")),
        };

        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "Test",
            new[] { tree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new DSA019Analyzer());
        var compilationWithAnalyzers = compilation.WithAnalyzers(analyzers);
        var diagnostics = (await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false))
            .Where(d => d.Id == DSA019Analyzer.DiagnosticId)
            .OrderBy(d => d.Location.SourceSpan.Start)
            .ToArray();

        Assert.IsTrue(diagnostics.Length > 0, "Expected DSA019 diagnostics");

        using var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var solution = workspace.CurrentSolution
            .AddProject(projectId, "Test", "Test", LanguageNames.CSharp)
            .AddMetadataReferences(projectId, references);
        var docId = DocumentId.CreateNewId(projectId);
        solution = solution.AddDocument(docId, "Test.cs", source);
        workspace.TryApplyChanges(solution);

        var document = workspace.CurrentSolution.GetDocument(docId);
        var docCompilation = await document.Project.GetCompilationAsync().ConfigureAwait(false);
        var docDiags = (await docCompilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync().ConfigureAwait(false))
            .Where(d => d.Id == DSA019Analyzer.DiagnosticId)
            .ToArray();

        var provider = new DSA019CodeFixProvider();
        var actions = new List<CodeAction>();
        var fixContext = new CodeFixContext(
            document,
            docDiags[0],
            (action, _) => actions.Add(action),
            CancellationToken.None);

        await provider.RegisterCodeFixesAsync(fixContext).ConfigureAwait(false);

        Assert.IsTrue(actions.Count > 0, "Expected code fix actions");
        var mainAction = actions.First(a => a.EquivalenceKey == DSA019Analyzer.DiagnosticId);
        StringAssert.StartsWith(mainAction.Title, "Extract '");
        StringAssert.Contains(mainAction.Title, "outer.Middle.Inner.Deep");
        StringAssert.EndsWith(mainAction.Title, "' to short variable");
    }

    [TestMethod]
    public async Task CodeFixTitle_TruncatesLongExpression()
    {
        var source = @"
namespace TestApp
{
    public class Z { public int V; }
    public class Y { public Z VeryLongPropertyNameForTestingTruncation; }
    public class X { public Y AnotherVeryLongPropertyNameHere; }
    public class W { public X YetAnotherLongPropertyNameForGoodMeasure; }
    public class MyService
    {
        public void Process(W w)
        {
            var a = w.YetAnotherLongPropertyNameForGoodMeasure.AnotherVeryLongPropertyNameHere.VeryLongPropertyNameForTestingTruncation.V;
            var b = w.YetAnotherLongPropertyNameForGoodMeasure.AnotherVeryLongPropertyNameHere.VeryLongPropertyNameForTestingTruncation.V;
        }
    }
}";

        var runtimeDir = RuntimeEnvironment.GetRuntimeDirectory();
        var references = new MetadataReference[]
        {
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Private.CoreLib.dll")),
        };

        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "Test",
            new[] { tree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new DSA019Analyzer());
        var docDiags = (await compilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync().ConfigureAwait(false))
            .Where(d => d.Id == DSA019Analyzer.DiagnosticId)
            .ToArray();

        Assert.IsTrue(docDiags.Length > 0, "Expected DSA019 diagnostics");

        using var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var solution = workspace.CurrentSolution
            .AddProject(projectId, "Test", "Test", LanguageNames.CSharp)
            .AddMetadataReferences(projectId, references);
        var docId = DocumentId.CreateNewId(projectId);
        solution = solution.AddDocument(docId, "Test.cs", source);
        workspace.TryApplyChanges(solution);

        var document = workspace.CurrentSolution.GetDocument(docId);
        var docCompilation = await document.Project.GetCompilationAsync().ConfigureAwait(false);
        var docDiags2 = (await docCompilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync().ConfigureAwait(false))
            .Where(d => d.Id == DSA019Analyzer.DiagnosticId)
            .ToArray();

        var provider = new DSA019CodeFixProvider();
        var actions = new List<CodeAction>();
        var fixContext = new CodeFixContext(
            document,
            docDiags2[0],
            (action, _) => actions.Add(action),
            CancellationToken.None);

        await provider.RegisterCodeFixesAsync(fixContext).ConfigureAwait(false);

        Assert.IsTrue(actions.Count > 0, "Expected code fix actions");
        var mainAction = actions.First(a => a.EquivalenceKey == DSA019Analyzer.DiagnosticId);
        StringAssert.StartsWith(mainAction.Title, "Extract '");
        StringAssert.EndsWith(mainAction.Title, "' to short variable");
        Assert.IsTrue(mainAction.Title.Contains("..."), "Long expressions should be truncated with '...'");
        Assert.IsFalse(mainAction.Title.Contains("VeryLongPropertyNameForTestingTruncation"),
            "Full long member name should not appear — it should be truncated");
    }

    #region Long variable name strategy

    [TestMethod]
    public async Task LongName_ExtractsFullChainAsCamelCase()
    {
        var source = @"
namespace TestApp
{
    public class VersionInfo { public int Id; }
    public class DataItemInfo { public VersionInfo DataItem; }
    public class DescriptorInfo { public DataItemInfo MyEnvironmentDescriptor; }
    public class ScenarioData { public DescriptorInfo Details; }
    public class MyService
    {
        public void Process(ScenarioData scenario)
        {
            var x = {|#0:scenario.Details.MyEnvironmentDescriptor.DataItem|}.Id;
            var y = {|#1:scenario.Details.MyEnvironmentDescriptor.DataItem|}.Id;
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class VersionInfo { public int Id; }
    public class DataItemInfo { public VersionInfo DataItem; }
    public class DescriptorInfo { public DataItemInfo MyEnvironmentDescriptor; }
    public class ScenarioData { public DescriptorInfo Details; }
    public class MyService
    {
        public void Process(ScenarioData scenario)
        {
            var scenarioDetailsMyEnvironmentDescriptorDataItem = scenario.Details.MyEnvironmentDescriptor.DataItem;
            var x = scenarioDetailsMyEnvironmentDescriptorDataItem.Id;
            var y = scenarioDetailsMyEnvironmentDescriptorDataItem.Id;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA019Analyzer.DiagnosticId + "_Long";
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("scenario.Details.MyEnvironmentDescriptor.DataItem", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithSpan(12, 21, 12, 107).WithArguments("scenario.Details.MyEnvironmentDescriptor.DataItem.Id", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("scenario.Details.MyEnvironmentDescriptor.DataItem", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithSpan(13, 21, 13, 107).WithArguments("scenario.Details.MyEnvironmentDescriptor.DataItem.Id", 2));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task LongName_SimpleThreeLevelChain()
    {
        var source = @"
namespace TestApp
{
    public class DbSettings { public string ConnectionString; public int Timeout; }
    public class Infra { public DbSettings Database; }
    public class Settings { public Infra Infrastructure; }
    public class AppConfig { public Settings Settings; }
    public class MyService
    {
        public void Process(AppConfig config)
        {
            var connStr = {|#0:config.Settings.Infrastructure.Database|}.ConnectionString;
            var timeout = {|#1:config.Settings.Infrastructure.Database|}.Timeout;
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class DbSettings { public string ConnectionString; public int Timeout; }
    public class Infra { public DbSettings Database; }
    public class Settings { public Infra Infrastructure; }
    public class AppConfig { public Settings Settings; }
    public class MyService
    {
        public void Process(AppConfig config)
        {
            var configSettingsInfrastructureDatabase = config.Settings.Infrastructure.Database;
            var connStr = configSettingsInfrastructureDatabase.ConnectionString;
            var timeout = configSettingsInfrastructureDatabase.Timeout;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA019Analyzer.DiagnosticId + "_Long";
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("config.Settings.Infrastructure.Database", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("config.Settings.Infrastructure.Database", 2));

        await test.RunAsync().ConfigureAwait(false);
    }

    #endregion

    #region Compact variable name strategy

    [TestMethod]
    public async Task CompactName_ExtractsFirstWordOfEachMember()
    {
        var source = @"
namespace TestApp
{
    public class VersionInfo { public int Id; }
    public class DataItemInfo { public VersionInfo DataItem; }
    public class DescriptorInfo { public DataItemInfo MyEnvironmentDescriptor; }
    public class ScenarioData { public DescriptorInfo Details; }
    public class MyService
    {
        public void Process(ScenarioData scenario)
        {
            var x = {|#0:scenario.Details.MyEnvironmentDescriptor.DataItem|}.Id;
            var y = {|#1:scenario.Details.MyEnvironmentDescriptor.DataItem|}.Id;
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class VersionInfo { public int Id; }
    public class DataItemInfo { public VersionInfo DataItem; }
    public class DescriptorInfo { public DataItemInfo MyEnvironmentDescriptor; }
    public class ScenarioData { public DescriptorInfo Details; }
    public class MyService
    {
        public void Process(ScenarioData scenario)
        {
            var scenarioDetailsRecordUser = scenario.Details.MyEnvironmentDescriptor.DataItem;
            var x = scenarioDetailsRecordUser.Id;
            var y = scenarioDetailsRecordUser.Id;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA019Analyzer.DiagnosticId + "_Compact";
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("scenario.Details.MyEnvironmentDescriptor.DataItem", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithSpan(12, 21, 12, 107).WithArguments("scenario.Details.MyEnvironmentDescriptor.DataItem.Id", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("scenario.Details.MyEnvironmentDescriptor.DataItem", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithSpan(13, 21, 13, 107).WithArguments("scenario.Details.MyEnvironmentDescriptor.DataItem.Id", 2));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task CompactName_SimpleThreeLevelChain()
    {
        // For single-word members, compact == long (first word of "Database" is "Database")
        var source = @"
namespace TestApp
{
    public class DbSettings { public string ConnectionString; public int Timeout; }
    public class Infra { public DbSettings Database; }
    public class Settings { public Infra Infrastructure; }
    public class AppConfig { public Settings Settings; }
    public class MyService
    {
        public void Process(AppConfig config)
        {
            var connStr = {|#0:config.Settings.Infrastructure.Database|}.ConnectionString;
            var timeout = {|#1:config.Settings.Infrastructure.Database|}.Timeout;
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class DbSettings { public string ConnectionString; public int Timeout; }
    public class Infra { public DbSettings Database; }
    public class Settings { public Infra Infrastructure; }
    public class AppConfig { public Settings Settings; }
    public class MyService
    {
        public void Process(AppConfig config)
        {
            var configSettingsInfrastructureDatabase = config.Settings.Infrastructure.Database;
            var connStr = configSettingsInfrastructureDatabase.ConnectionString;
            var timeout = configSettingsInfrastructureDatabase.Timeout;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA019Analyzer.DiagnosticId + "_Compact";
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("config.Settings.Infrastructure.Database", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("config.Settings.Infrastructure.Database", 2));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task CompactName_WithIndexer()
    {
        var source = @"
namespace TestApp
{
    public class Light { public bool IsOn() => true; }
    public class Room { public Light[] Lights; }
    public class Rooms { public Room Bathroom; }
    public class Home { public Rooms Rooms; }
    public class MyService
    {
        public void Process(Home home)
        {
            var status = new
            {
                Primary = {|#0:home.Rooms.Bathroom.Lights|}[0].IsOn(),
                Secondary = {|#1:home.Rooms.Bathroom.Lights|}[1].IsOn(),
            };
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class Light { public bool IsOn() => true; }
    public class Room { public Light[] Lights; }
    public class Rooms { public Room Bathroom; }
    public class Home { public Rooms Rooms; }
    public class MyService
    {
        public void Process(Home home)
        {
            var homeRoomsBathroomLights = home.Rooms.Bathroom.Lights;
            var status = new
            {
                Primary = homeRoomsBathroomLights[0].IsOn(),
                Secondary = homeRoomsBathroomLights[1].IsOn(),
            };
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA019Analyzer.DiagnosticId + "_Compact";
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("home.Rooms.Bathroom.Lights", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("home.Rooms.Bathroom.Lights", 2));

        await test.RunAsync().ConfigureAwait(false);
    }

    #endregion

    #region Three code actions registered

    [TestMethod]
    public async Task ThreeCodeActions_AreRegisteredWithCorrectTitles()
    {
        var source = @"
namespace TestApp
{
    public class DbSettings { public string ConnectionString; public int Timeout; }
    public class Infra { public DbSettings Database; }
    public class Settings { public Infra Infrastructure; }
    public class AppConfig { public Settings Settings; }
    public class MyService
    {
        public void Process(AppConfig config)
        {
            var connStr = config.Settings.Infrastructure.Database.ConnectionString;
            var timeout = config.Settings.Infrastructure.Database.Timeout;
        }
    }
}";

        var runtimeDir = RuntimeEnvironment.GetRuntimeDirectory();
        var references = new MetadataReference[]
        {
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Private.CoreLib.dll")),
        };

        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "Test",
            new[] { tree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new DSA019Analyzer());
        var compilationWithAnalyzers = compilation.WithAnalyzers(analyzers);
        var diagnostics = (await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false))
            .Where(d => d.Id == DSA019Analyzer.DiagnosticId)
            .OrderBy(d => d.Location.SourceSpan.Start)
            .ToArray();

        Assert.IsTrue(diagnostics.Length > 0, "Expected DSA019 diagnostics");

        using var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var solution = workspace.CurrentSolution
            .AddProject(projectId, "Test", "Test", LanguageNames.CSharp)
            .AddMetadataReferences(projectId, references);
        var docId = DocumentId.CreateNewId(projectId);
        solution = solution.AddDocument(docId, "Test.cs", source);
        workspace.TryApplyChanges(solution);

        var document = workspace.CurrentSolution.GetDocument(docId);
        var docCompilation = await document.Project.GetCompilationAsync().ConfigureAwait(false);
        var docDiags = (await docCompilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync().ConfigureAwait(false))
            .Where(d => d.Id == DSA019Analyzer.DiagnosticId)
            .ToArray();

        var provider = new DSA019CodeFixProvider();
        var actions = new List<CodeAction>();
        var fixContext = new CodeFixContext(
            document,
            docDiags[0],
            (action, _) => actions.Add(action),
            CancellationToken.None);

        await provider.RegisterCodeFixesAsync(fixContext).ConfigureAwait(false);

        var shortAction = actions.FirstOrDefault(a => a.EquivalenceKey == DSA019Analyzer.DiagnosticId);
        var longAction = actions.FirstOrDefault(a => a.EquivalenceKey == DSA019Analyzer.DiagnosticId + "_Long");
        var compactAction = actions.FirstOrDefault(a => a.EquivalenceKey == DSA019Analyzer.DiagnosticId + "_Compact");

        Assert.IsNotNull(shortAction, "Short variable action should be registered");
        Assert.IsNotNull(longAction, "Long variable action should be registered");
        Assert.IsNotNull(compactAction, "Compact variable action should be registered");

        StringAssert.EndsWith(shortAction.Title, "' to short variable");
        StringAssert.EndsWith(longAction.Title, "' to long variable");
        StringAssert.EndsWith(compactAction.Title, "' to compact variable");
    }

    #endregion

    #region Additional naming strategy scenarios

    [TestMethod]
    public async Task LongName_WithMethodCallInChain()
    {
        var source = @"
namespace TestApp
{
    public class Summary { public int Count; public string Label; }
    public class Report { public Summary Summary; }
    public class ServiceProvider { public Report GetReport() => null; }
    public class Root { public ServiceProvider Service; }
    public class MyService
    {
        public void Process(Root provider)
        {
            var count = {|#0:provider.Service.GetReport().Summary|}.Count;
            var label = {|#1:provider.Service.GetReport().Summary|}.Label;
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class Summary { public int Count; public string Label; }
    public class Report { public Summary Summary; }
    public class ServiceProvider { public Report GetReport() => null; }
    public class Root { public ServiceProvider Service; }
    public class MyService
    {
        public void Process(Root provider)
        {
            var providerServiceGetReportSummary = provider.Service.GetReport().Summary;
            var count = providerServiceGetReportSummary.Count;
            var label = providerServiceGetReportSummary.Label;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA019Analyzer.DiagnosticId + "_Long";
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("provider.Service.GetReport().Summary", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("provider.Service.GetReport().Summary", 2));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task CompactName_WithMethodCallInChain()
    {
        var source = @"
namespace TestApp
{
    public class Summary { public int Count; public string Label; }
    public class Report { public Summary Summary; }
    public class ServiceProvider { public Report GetReport() => null; }
    public class Root { public ServiceProvider Service; }
    public class MyService
    {
        public void Process(Root provider)
        {
            var count = {|#0:provider.Service.GetReport().Summary|}.Count;
            var label = {|#1:provider.Service.GetReport().Summary|}.Label;
        }
    }
}";

        // GetReport → first word is "Get", Summary → "Summary"
        // provider, Service, Get, Summary → providerServiceGetSummary
        var fixedSource = @"
namespace TestApp
{
    public class Summary { public int Count; public string Label; }
    public class Report { public Summary Summary; }
    public class ServiceProvider { public Report GetReport() => null; }
    public class Root { public ServiceProvider Service; }
    public class MyService
    {
        public void Process(Root provider)
        {
            var providerServiceGetSummary = provider.Service.GetReport().Summary;
            var count = providerServiceGetSummary.Count;
            var label = providerServiceGetSummary.Label;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA019Analyzer.DiagnosticId + "_Compact";
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("provider.Service.GetReport().Summary", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("provider.Service.GetReport().Summary", 2));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task LongName_WithThisPrefix()
    {
        var source = @"
namespace TestApp
{
    public class Deep { public int X; public int Y; }
    public class Inner { public Deep DeepChild; }
    public class Middle { public Inner InnerChild; }
    public class MyService
    {
        public Middle MiddleField;

        public void Process()
        {
            var x = {|#0:this.MiddleField.InnerChild.DeepChild|}.X;
            var y = {|#1:this.MiddleField.InnerChild.DeepChild|}.Y;
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class Deep { public int X; public int Y; }
    public class Inner { public Deep DeepChild; }
    public class Middle { public Inner InnerChild; }
    public class MyService
    {
        public Middle MiddleField;

        public void Process()
        {
            var middleFieldInnerChildDeepChild = this.MiddleField.InnerChild.DeepChild;
            var x = middleFieldInnerChildDeepChild.X;
            var y = middleFieldInnerChildDeepChild.Y;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA019Analyzer.DiagnosticId + "_Long";
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("this.MiddleField.InnerChild.DeepChild", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("this.MiddleField.InnerChild.DeepChild", 2));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task LongName_ThreeOccurrences()
    {
        var source = @"
namespace TestApp
{
    public class Deep { public int X; public int Y; public int Z; }
    public class Inner { public Deep DeepLevel; }
    public class Middle { public Inner InnerLevel; }
    public class Outer { public Middle MiddleLevel; }
    public class MyService
    {
        public void Process(Outer outer)
        {
            var x = {|#0:outer.MiddleLevel.InnerLevel.DeepLevel|}.X;
            var y = {|#1:outer.MiddleLevel.InnerLevel.DeepLevel|}.Y;
            var z = {|#2:outer.MiddleLevel.InnerLevel.DeepLevel|}.Z;
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class Deep { public int X; public int Y; public int Z; }
    public class Inner { public Deep DeepLevel; }
    public class Middle { public Inner InnerLevel; }
    public class Outer { public Middle MiddleLevel; }
    public class MyService
    {
        public void Process(Outer outer)
        {
            var outerMiddleLevelInnerLevelDeepLevel = outer.MiddleLevel.InnerLevel.DeepLevel;
            var x = outerMiddleLevelInnerLevelDeepLevel.X;
            var y = outerMiddleLevelInnerLevelDeepLevel.Y;
            var z = outerMiddleLevelInnerLevelDeepLevel.Z;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA019Analyzer.DiagnosticId + "_Long";
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("outer.MiddleLevel.InnerLevel.DeepLevel", 3));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("outer.MiddleLevel.InnerLevel.DeepLevel", 3));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(2).WithArguments("outer.MiddleLevel.InnerLevel.DeepLevel", 3));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task CompactName_WithThisPrefix()
    {
        var source = @"
namespace TestApp
{
    public class Deep { public int X; public int Y; }
    public class Inner { public Deep DeepChild; }
    public class Middle { public Inner InnerChild; }
    public class MyService
    {
        public Middle MiddleField;

        public void Process()
        {
            var x = {|#0:this.MiddleField.InnerChild.DeepChild|}.X;
            var y = {|#1:this.MiddleField.InnerChild.DeepChild|}.Y;
        }
    }
}";

        // this is excluded from chain, so: MiddleField → Middle, InnerChild → Inner, DeepChild → Deep
        // → middleInnerDeep
        var fixedSource = @"
namespace TestApp
{
    public class Deep { public int X; public int Y; }
    public class Inner { public Deep DeepChild; }
    public class Middle { public Inner InnerChild; }
    public class MyService
    {
        public Middle MiddleField;

        public void Process()
        {
            var middleInnerDeep = this.MiddleField.InnerChild.DeepChild;
            var x = middleInnerDeep.X;
            var y = middleInnerDeep.Y;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA019Analyzer.DiagnosticId + "_Compact";
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("this.MiddleField.InnerChild.DeepChild", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("this.MiddleField.InnerChild.DeepChild", 2));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ShortName_RepeatedChainAcrossSeparateObjectInitializersInArray()
    {
        var source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace TestApp
{
    public class Tenant { public int Id; }
    public class UserInfo { public Tenant Tenant; }
    public class TestCtx { public UserInfo CurrentUser; }
    public class WsDto { public int TenantId; public string Name; }
    public class MyService
    {
        public static Task<IReadOnlyCollection<WsDto>> GetWorkingEnvs(TestCtx testContext)
        {
            return Task.FromResult<IReadOnlyCollection<WsDto>>(
                new[]
                {
                    new WsDto
                    {
                        TenantId = {|#0:testContext.CurrentUser.Tenant.Id|},
                        Name = ""Test 1""
                    },
                    new WsDto
                    {
                        TenantId = {|#1:testContext.CurrentUser.Tenant.Id|},
                        Name = ""Test 2""
                    }
                });
        }
    }
}";

        var fixedSource = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace TestApp
{
    public class Tenant { public int Id; }
    public class UserInfo { public Tenant Tenant; }
    public class TestCtx { public UserInfo CurrentUser; }
    public class WsDto { public int TenantId; public string Name; }
    public class MyService
    {
        public static Task<IReadOnlyCollection<WsDto>> GetWorkingEnvs(TestCtx testContext)
        {
            var id = testContext.CurrentUser.Tenant.Id;
            return Task.FromResult<IReadOnlyCollection<WsDto>>(
                new[]
                {
                    new WsDto
                    {
                        TenantId = id,
                        Name = ""Test 1""
                    },
                    new WsDto
                    {
                        TenantId = id,
                        Name = ""Test 2""
                    }
                });
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("testContext.CurrentUser.Tenant.Id", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("testContext.CurrentUser.Tenant.Id", 2));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task LongName_WithElementAccessInMiddle()
    {
        var source = @"
namespace TestApp
{
    public class Light { public bool IsOn() => true; }
    public class Room { public Light[] Lights; }
    public class Rooms { public Room Bathroom; }
    public class Home { public Rooms Rooms; }
    public class MyService
    {
        public void Process(Home home)
        {
            var status = new
            {
                Primary = {|#0:home.Rooms.Bathroom.Lights|}[0].IsOn(),
                Secondary = {|#1:home.Rooms.Bathroom.Lights|}[1].IsOn(),
            };
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class Light { public bool IsOn() => true; }
    public class Room { public Light[] Lights; }
    public class Rooms { public Room Bathroom; }
    public class Home { public Rooms Rooms; }
    public class MyService
    {
        public void Process(Home home)
        {
            var homeRoomsBathroomLights = home.Rooms.Bathroom.Lights;
            var status = new
            {
                Primary = homeRoomsBathroomLights[0].IsOn(),
                Secondary = homeRoomsBathroomLights[1].IsOn(),
            };
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA019Analyzer.DiagnosticId + "_Long";
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("home.Rooms.Bathroom.Lights", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("home.Rooms.Bathroom.Lights", 2));

        await test.RunAsync().ConfigureAwait(false);
    }

    #endregion

}