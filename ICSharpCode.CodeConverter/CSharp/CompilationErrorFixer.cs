﻿using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.CodeConverter.Util;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ICSharpCode.CodeConverter.CSharp
{
    internal class CompilationErrorFixer
    {
        private const string UnresolvedTypeOrNamespaceDiagnosticId = "CS0246";
        private const string UnusedUsingDiagnosticId = "CS8019";
        private readonly CSharpSyntaxTree _syntaxTree;
        private readonly SemanticModel _semanticModel;

        public CompilationErrorFixer(CSharpCompilation compilation, CSharpSyntaxTree syntaxTree)
        {
            this._syntaxTree = syntaxTree;
            this._semanticModel = compilation.GetSemanticModel(syntaxTree, true);
        }

        public CSharpSyntaxNode Fix()
        {
            var syntaxNode = _syntaxTree.GetRoot();
            return TidyUsings(syntaxNode);
        }

        /// <remarks>These are tidied up so we can add as many GlobalImports as we want when building compilations</remarks>
        private CSharpSyntaxNode TidyUsings(CSharpSyntaxNode root)
        {
            var diagnostics = _semanticModel.GetDiagnostics().ToList();
            var unusedUsings = diagnostics
                .Where(d => d.Id == UnusedUsingDiagnosticId)
                .Select(d => root.FindNode(d.Location.SourceSpan))
                .OfType<UsingDirectiveSyntax>()
                .ToList();

            var nodesWithUnresolvedTypes = diagnostics
                .Where(d => d.Id == UnresolvedTypeOrNamespaceDiagnosticId && d.Location.IsInSource)
                .Select(d => root.FindNode(d.Location.SourceSpan))
                .ToLookup(d => d.GetAncestor<UsingDirectiveSyntax>());
            unusedUsings = unusedUsings.Except(nodesWithUnresolvedTypes.Select(g => g.Key)).ToList();

            if (nodesWithUnresolvedTypes[null].Any() || !unusedUsings.Any()) {
                return root;
            }

            root = root.RemoveNodes(unusedUsings, SyntaxRemoveOptions.KeepNoTrivia);
            var firstChild = root.ChildNodes().FirstOrDefault();
            if (firstChild != null) {
                root = root.ReplaceNode(firstChild,
                    firstChild.WithLeadingTrivia(firstChild.GetLeadingTrivia()
                        .SkipWhile(t => t.IsWhitespaceTrivia())));
            }

            return root;
        }
    }
}