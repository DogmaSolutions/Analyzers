using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers.Test;

/// <summary>
/// An <see cref="AnalysisContext"/> that runs nothing: it only records the host-level configuration an analyzer asks for in
/// <see cref="DiagnosticAnalyzer.Initialize"/>, so that tests can verify it without compiling any code.
/// </summary>
internal sealed class RecordingAnalysisContext : AnalysisContext
{
    public GeneratedCodeAnalysisFlags? GeneratedCodeFlags { get; private set; }

    public bool ConcurrentExecutionEnabled { get; private set; }

    public int RegistrationCount { get; private set; }

    public override void ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags analysisMode) => GeneratedCodeFlags = analysisMode;

    public override void EnableConcurrentExecution() => ConcurrentExecutionEnabled = true;

    public override void RegisterCompilationStartAction(Action<CompilationStartAnalysisContext> action) => RegistrationCount++;

    public override void RegisterCompilationAction(Action<CompilationAnalysisContext> action) => RegistrationCount++;

    public override void RegisterSemanticModelAction(Action<SemanticModelAnalysisContext> action) => RegistrationCount++;

    public override void RegisterSymbolAction(Action<SymbolAnalysisContext> action, ImmutableArray<SymbolKind> symbolKinds) => RegistrationCount++;

    public override void RegisterSymbolStartAction(Action<SymbolStartAnalysisContext> action, SymbolKind symbolKind) => RegistrationCount++;

    public override void RegisterCodeBlockStartAction<TLanguageKindEnum>(Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> action) => RegistrationCount++;

    public override void RegisterCodeBlockAction(Action<CodeBlockAnalysisContext> action) => RegistrationCount++;

    public override void RegisterSyntaxTreeAction(Action<SyntaxTreeAnalysisContext> action) => RegistrationCount++;

    public override void RegisterAdditionalFileAction(Action<AdditionalFileAnalysisContext> action) => RegistrationCount++;

    public override void RegisterSyntaxNodeAction<TLanguageKindEnum>(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds) => RegistrationCount++;

    public override void RegisterOperationAction(Action<OperationAnalysisContext> action, ImmutableArray<OperationKind> operationKinds) => RegistrationCount++;

    public override void RegisterOperationBlockStartAction(Action<OperationBlockStartAnalysisContext> action) => RegistrationCount++;

    public override void RegisterOperationBlockAction(Action<OperationBlockAnalysisContext> action) => RegistrationCount++;
}
