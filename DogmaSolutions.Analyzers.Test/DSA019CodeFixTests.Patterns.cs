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

public partial class DSA019CodeFixTests
{

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
            var x = {|#0:config.Settings.Data.Value|};
            var y = {|#1:config.Settings.Data.Value|};
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
            var x = value;
            var y = value;
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


}
