using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using System;
using System.Linq;

namespace CodeCracker
{
    public static partial class AnalyzerExtensions
    {
        public static bool HasAttributeOnAncestorOrSelf(this VisualBasicSyntaxNode node, string attributeName)
        {
            var parentMethod = (MethodBlockBaseSyntax)node.FirstAncestorOrSelfOfType(typeof(MethodBlockSyntax), typeof(ConstructorBlockSyntax));
            if (parentMethod?.BlockStatement.AttributeLists.HasAttribute(attributeName) ?? false)
                return true;
            var type = (TypeBlockSyntax)node.FirstAncestorOrSelfOfType(typeof(ClassBlockSyntax), typeof(StructureBlockSyntax));
            while (type != null)
            {
                if (type.BlockStatement.AttributeLists.HasAttribute(attributeName))
                    return true;
                type = (TypeBlockSyntax)type.FirstAncestorOfType(typeof(ClassBlockSyntax), typeof(StructureBlockSyntax));
            }
            var propertyBlock = node.FirstAncestorOrSelfOfType<PropertyBlockSyntax>();
            if (propertyBlock?.PropertyStatement.AttributeLists.HasAttribute(attributeName) ?? false)
                return true;
            var accessor = node.FirstAncestorOrSelfOfType<AccessorBlockSyntax>();
            if (accessor?.AccessorStatement.AttributeLists.HasAttribute(attributeName) ?? false)
                return true;
            var anInterface = node.FirstAncestorOrSelfOfType<InterfaceBlockSyntax>();
            if (anInterface?.InterfaceStatement.AttributeLists.HasAttribute(attributeName) ?? false)
                return true;
            var anEnum = node.FirstAncestorOrSelfOfType<EnumBlockSyntax>();
            if (anEnum?.EnumStatement.AttributeLists.HasAttribute(attributeName) ?? false)
                return true;
            var module = node.FirstAncestorOrSelfOfType<ModuleBlockSyntax>();
            if (module?.ModuleStatement.AttributeLists.HasAttribute(attributeName) ?? false)
                return true;
            var eventBlock = node.FirstAncestorOrSelfOfType<EventBlockSyntax>();
            if (eventBlock?.EventStatement.AttributeLists.HasAttribute(attributeName) ?? false)
                return true;
            var theEvent = node as EventStatementSyntax;
            if (theEvent?.AttributeLists.HasAttribute(attributeName) ?? false)
                return true;
            var property = node as PropertyStatementSyntax;
            if (property?.AttributeLists.HasAttribute(attributeName) ?? false)
                return true;
            var field = node as FieldDeclarationSyntax;
            if (field?.AttributeLists.HasAttribute(attributeName) ?? false)
                return true;
            var parameter = node as ParameterSyntax;
            if (parameter?.AttributeLists.HasAttribute(attributeName) ?? false)
                return true;
            var aDelegate = node as DelegateStatementSyntax;
            if (aDelegate?.AttributeLists.HasAttribute(attributeName) ?? false)
                return true;
            return false;
        }

        public static bool HasAttribute(this SyntaxList<AttributeListSyntax> attributeLists, string attributeName) =>
            attributeLists.SelectMany(a => a.Attributes).Any(a => a.Name.ToString().EndsWith(attributeName, StringComparison.OrdinalIgnoreCase));

    }
}