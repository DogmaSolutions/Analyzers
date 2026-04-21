using System.Threading.Tasks;
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
                .WithLocation(0).WithArguments("home.Rooms.Bathroom.Lights"));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("home.Rooms.Bathroom.Lights"));

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
                .WithLocation(0).WithArguments("config.Settings.Infrastructure.Database"));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("config.Settings.Infrastructure.Database"));

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
                .WithLocation(0).WithArguments("customer.Contact.Profile.Address"));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("customer.Contact.Profile.Address"));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(2).WithArguments("customer.Contact.Profile.Address"));

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
                .WithLocation(0).WithArguments("outer.Middle.Inner.Deep"));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("outer.Middle.Inner.Deep"));

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
                .WithLocation(0).WithArguments("e.Category.Spec.Detail"));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("e.Category.Spec.Detail"));

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
                .WithLocation(0).WithArguments("this._field.Inner.Deep"));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("this._field.Inner.Deep"));

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
                .WithLocation(0).WithArguments("outer.Middle.Inner.Deep"));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("outer.Middle.Inner.Deep"));

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
                .WithLocation(0).WithArguments(@"dt.Rows[0][""COL_A""]"));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(2).WithArguments(@"dt.Rows[0][""COL_A""].ToString"));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments(@"dt.Rows[0][""COL_A""]"));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(3).WithArguments(@"dt.Rows[0][""COL_A""].ToString"));

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
                .WithLocation(0).WithArguments("config.Options.Formatter.Format"));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("config.Options.Formatter.Format"));

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
                .WithLocation(0).WithArguments("outer.Middle.Inner.Deep"));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("outer.Middle.Inner.Deep"));

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
                .WithLocation(0).WithArguments("outer.Middle.Inner.Deep"));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("outer.Middle.Inner.Deep"));

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
                .WithLocation(0).WithArguments("outer.Middle.Inner.Deep"));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("outer.Middle.Inner.Deep"));

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
                .WithLocation(0).WithArguments(@"dt.Rows[0][""TheKey""]"));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(2).WithArguments(@"dt.Rows[0][""TheKey""].ToValue"));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments(@"dt.Rows[0][""TheKey""]"));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(3).WithArguments(@"dt.Rows[0][""TheKey""].ToValue"));

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
                .WithLocation(0).WithArguments(@"dt.Rows[0][""TheKey""]"));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments(@"dt.Rows[0][""TheKey""]"));

        await test.RunAsync().ConfigureAwait(false);
    }
}
