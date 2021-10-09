// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Bicep.Core.Diagnostics;
using Bicep.Core.Emit;
using Bicep.Core.Parsing;
using Bicep.Core.Semantics;
using Bicep.Core.Syntax;
using Bicep.Core.TypeSystem;
using Bicep.Core.Visitors;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Bicep.Core.Analyzers.Linter.Rules
{
    public sealed class NoUnnecessaryDependsOnRule : LinterRuleBase
    {
        public new const string Code = "no-unnecessary-dependson";

        public NoUnnecessaryDependsOnRule() : base(
            code: Code,
            description: CoreResources.NoUnnecessaryDependsOnRuleDescription,
            docUri: new Uri($"https://aka.ms/bicep/linter/{Code}")
        )
        {
        }

        public override string FormatMessage(params object[] values)
            => string.Format(CoreResources.NoUnnecessaryDependsOnRuleMessaage, values.First());

        public override IEnumerable<IDiagnostic> AnalyzeInternal(SemanticModel model)
        {
            ImmutableDictionary<DeclaredSymbol, ImmutableHashSet<ResourceDependency>> inferredDependenciesMap =
                ResourceDependencyVisitor.GetResourceDependencies(model, new ResourceDependencyVisitor.Options { IgnoreDependsOnProperties = true });
            var visitor = new ResourceVisitor(this, inferredDependenciesMap, model);
            visitor.Visit(model.SourceFile.ProgramSyntax);
            return visitor.diagnostics;
        }

        private static ObjectPropertySyntax? TryGetDependsOnProperty(ObjectSyntax? body) => body?.SafeGetPropertyByName("dependsOn"); //asdff?

        private class ResourceVisitor : SyntaxVisitor
        {
            public List<IDiagnostic> diagnostics = new List<IDiagnostic>();

            private readonly NoUnnecessaryDependsOnRule parent;
            IImmutableDictionary<DeclaredSymbol, ImmutableHashSet<ResourceDependency>> inferredDependenciesMap;
            private readonly SemanticModel model;

            public ResourceVisitor(NoUnnecessaryDependsOnRule parent, IImmutableDictionary<DeclaredSymbol, ImmutableHashSet<ResourceDependency>> inferredDependenciesMap, SemanticModel model)
            {
                this.parent = parent;
                this.inferredDependenciesMap = inferredDependenciesMap;
                this.model = model;
            }

            public override void VisitResourceDeclarationSyntax(ResourceDeclarationSyntax syntax)
            {
                if (syntax.TryGetBody() is ObjectSyntax body)
                {
                    var dependsOnProperty = body.SafeGetPropertyByName(LanguageConstants.ResourceDependsOnPropertyName);
                    if (dependsOnProperty?.Value is ArraySyntax declaredDependencies)
                    {
                        if (model.GetSymbolInfo(syntax) is DeclaredSymbol thisResource)
                        {
                            if (inferredDependenciesMap.TryGetValue(thisResource, out ImmutableHashSet<ResourceDependency> inferredDependencies))
                            {
                                var inferredDependencies2 = ResourceDependencyFinderVisitor.GetResourceDependencies(model, body); //asdff 

                                foreach (ArrayItemSyntax declaredDependency in declaredDependencies.Items)
                                {
                                    if (declaredDependency.Value is VariableAccessSyntax variableAccess) //asdff what else could this be?  what about modules?
                                    {
                                        if (model.GetSymbolInfo(variableAccess) is ResourceSymbol referencedResouce)
                                        {
                                            if (inferredDependencies.Any(d => d.Resource == referencedResouce))
                                            {
                                                this.diagnostics.Add(
                                                    parent.CreateDiagnosticForSpan(
                                                        declaredDependency.Span,
                                                        variableAccess.Name.IdentifierName)); //asdff fixable
                                            }
                                        }
                                        //inferredDependencies.Any(d => d.Resource == variableAccess.)
                                        //if (variableAccess.ReferencesResource(declaredDependency.Value))


                                        // if (inferredDependencies.Any(d => d.DeclaringSyntax is ResourceDeclarationSyntax resource
                                        //      && dependency.ReferencesResource(resource)))
                                        // {
                                        //     this.diagnostics.Add(dependency.Span);
                                        // }
                                    }

                                    var a = 1;
                                    a = a + 1;
                                }
                            }
                        }
                    }
                }

                base.VisitResourceDeclarationSyntax(syntax);
            }
        }

        private class PropertiesVisitor : SyntaxVisitor
        {
            private List<IDiagnostic> diagnostics;

            private const string adminUsernamePropertyName = "adminusername";
            private readonly NoUnnecessaryDependsOnRule parent;
            private readonly SemanticModel model;

            public PropertiesVisitor(NoUnnecessaryDependsOnRule parent, List<IDiagnostic> diagnostics, SemanticModel model)
            {
                this.parent = parent;
                this.model = model;
                this.diagnostics = diagnostics;
            }

            public override void VisitObjectPropertySyntax(ObjectPropertySyntax syntax)
            {
                string? propertyName = syntax.TryGetKeyText();
                // Check all properties with the name 'adminUsername', regardless of casing
                if (propertyName != null && StringComparer.OrdinalIgnoreCase.Equals(propertyName, adminUsernamePropertyName))
                {
                    var type = model.GetTypeInfo(syntax.Value);
                    if (type is StringLiteralType stringType)
                    {
                        diagnostics.Add(parent.CreateDiagnosticForSpan(syntax.Value.Span, stringType.RawStringValue));
                    }

                    // No need to traverse deeper
                }
                else
                {
                    base.VisitObjectPropertySyntax(syntax);
                }
            }
        }
    }
}

//asdff ResourceDependency
/*asdff

            this.ResourceDependencies = ResourceDependencyVisitor.GetResourceDependencies(semanticModel);

*/





/* asdff


            public override void VisitObjectPropertySyntax(ObjectPropertySyntax syntax)
            {
                if (syntax.NameEquals("dependsOn"))
                {
                    if (syntax.Value is ArraySyntax dependencies)
                    {
                        var inferredDependencies = ResourceDependencyFinderVisitor.GetResourceDependencies(this.semanticModel, syntax);
                        foreach (var dependency in dependencies.Items)
                        {
                            if (dependency.Value is VariableAccessSyntax item
                                && inferredDependencies.Any(d => d.DeclaringSyntax is ResourceDeclarationSyntax resource
                                   && item.ReferencesResource(resource)))
                            {
                                this.diagnostics.Add(dependency.Span);
                            }
                        }
                    }
                }
                base.VisitObjectPropertySyntax(syntax);
            }

            */