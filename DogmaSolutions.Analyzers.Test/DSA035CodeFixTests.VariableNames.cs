using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>
/// DSA035 code fix: the name of the variable the reflection call is hoisted into is built from the call and its
/// receiver (<c>hoisted_receiver_Method</c>), whatever the shape of the receiver (<c>DSA035CodeFixProvider.GenerateVariableName</c>).
/// </summary>
public partial class DSA035CodeFixTests
{
    // The line ending of this file: the verbatim sources below use it, and so must the lines added to them.
    private static readonly string NewLine = @"
".Contains("\r\n") ? "\r\n" : "\n";

    private static string LoopWith(string hoisted, string loopExpression) => @"
            using System;
            using System.Reflection;
            namespace TestApp
            {
                public class Holder { public Type Kind; }
                public class MyClass
                {
                    private Type _type;
                    public void Test(object obj, Type type, Holder holder, int[] arr)
                    {
" + (hoisted.Length == 0 ? string.Empty : hoisted + NewLine) + @"                        for (int i = 0; i < arr.Length; i++)
                        {
                            var result = " + loopExpression + @";
                        }
                    }
                }
            }";

    private static async Task VerifyHoistedAsync(string expression, string hoistedName)
    {
        var source = LoopWith(string.Empty, "{|#0:" + expression + "|}");
        var fixedSource = LoopWith(
            "                        var " + hoistedName + " = " + expression + ";",
            hoistedName);

        var test = new CSharpCodeFixVerifier<DSA035Analyzer, DSA035CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA035Analyzer, DSA035CodeFixProvider>
                .Diagnostic(DSA035Analyzer.DiagnosticId).WithLocation(0).WithArguments(expression));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task HoistedVariableName_UsesTheReceiverAndTheMethod()
    {
        await VerifyHoistedAsync("type.GetMethod(\"Execute\")", "hoisted_type_GetMethod").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task HoistedVariableName_ForATypeofReceiver_UsesOnlyTheMethod()
    {
        await VerifyHoistedAsync("typeof(MyClass).GetMethod(\"Test\")", "hoisted_GetMethod").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task HoistedVariableName_ForAMemberAccessReceiver_UsesOnlyTheMethod()
    {
        await VerifyHoistedAsync("holder.Kind.GetProperties()", "hoisted_GetProperties").ConfigureAwait(false);
    }

    // ---- GenerateVariableName on its own: the shapes the analyzer does not hand over in a predictable order ---

    private static string GenerateVariableName(string invocationText)
    {
        // The first invocation in document order is the outermost one: the one the analyzer reports.
        var invocation = SyntaxFactory.ParseExpression(invocationText)
            .DescendantNodesAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .First();
        return DSA035CodeFixProvider.GenerateVariableName(invocation);
    }

    [TestMethod]
    [DataRow("type.GetMethod(\"X\")", "hoisted_type_GetMethod")]
    [DataRow("obj.GetType().GetProperties()", "hoisted_GetType_GetProperties")]
    [DataRow("typeof(MyClass).GetMethod(\"X\")", "hoisted_GetMethod")]
    [DataRow("holder.Kind.GetProperties()", "hoisted_GetProperties")]
    [DataRow("type?.GetProperties()", "hoisted_type_GetProperties")]
    [DataRow("holder.Kind?.GetProperties()", "hoisted_GetProperties")]
    [DataRow("GetProperties()", "hoisted")]
    [DataRow("handlers[0]()", "hoisted")]
    public void GenerateVariableName_builds_the_name_from_the_receiver_and_the_method(string invocationText, string expectedName)
    {
        Assert.AreEqual(expectedName, GenerateVariableName(invocationText));
    }
}
