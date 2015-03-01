using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;

namespace CodeCracker.CSharp.Refactoring
{
    [ExportCodeFixProvider("StyleCopAllowMembersOrderingCodeFixProvider", LanguageNames.CSharp), Shared]
    public class StyleCopAllowMembersOrderingCodeFixProvider : BaseAllowMembersOrderingCodeFixProvider
    {
        public StyleCopAllowMembersOrderingCodeFixProvider() :
            base("Order {0}'s members following StyleCop patterns") { }

        protected override IComparer<MemberDeclarationSyntax> GetMemberDeclarationComparer(Document d, CancellationToken c) =>
            new StyleCopMembersComparer();

        public class StyleCopMembersComparer : IComparer<MemberDeclarationSyntax>
        {
            readonly Dictionary<Type, int> typeRank = new Dictionary<Type, int>
            {
                { typeof(FieldDeclarationSyntax),       1 },
                { typeof(ConstructorDeclarationSyntax), 2 },
                { typeof(DestructorDeclarationSyntax),  3 },
                { typeof(DelegateDeclarationSyntax),    4 },
                { typeof(EventFieldDeclarationSyntax),  5 },
                { typeof(EventDeclarationSyntax),       6 },
                { typeof(EnumDeclarationSyntax),        7 },
                { typeof(InterfaceDeclarationSyntax),   8 },
                { typeof(PropertyDeclarationSyntax),    9 },
                { typeof(IndexerDeclarationSyntax),     10 },
                { typeof(OperatorDeclarationSyntax),    11 },
                { typeof(MethodDeclarationSyntax),      12 },
                { typeof(StructDeclarationSyntax),      13 },
                { typeof(ClassDeclarationSyntax),       14 },
            };

            private readonly Dictionary<SyntaxKind, int> specialModifierRank = new Dictionary<SyntaxKind, int>
            {
                { SyntaxKind.ConstKeyword,   1 },
                { SyntaxKind.StaticKeyword,  2 },
            };

            private readonly Dictionary<SyntaxKind, int> accessLevelRank = new Dictionary<SyntaxKind, int>
            {
                { SyntaxKind.PublicKeyword,     -4 },
                { SyntaxKind.InternalKeyword,   -2 },
                { SyntaxKind.ProtectedKeyword,   1 },
                { SyntaxKind.PrivateKeyword,     2 },
            };

            public int Compare(MemberDeclarationSyntax x, MemberDeclarationSyntax y)
            {
                if (x == null && y == null) return 0;
                if (x == null) return 1;
                if (y == null) return -1;

                var comparedPoints = GetRankPoints(x).CompareTo(GetRankPoints(y));
                if (comparedPoints != 0)
                    return comparedPoints;

                var xModifiers = GetModifiers(x);
                var yModifiers = GetModifiers(y);

                comparedPoints = GetAccessLevelPoints(xModifiers).CompareTo(GetAccessLevelPoints(yModifiers));
                if (comparedPoints != 0)
                    return comparedPoints;

                comparedPoints = GetSpecialModifierPoints(xModifiers).CompareTo(GetSpecialModifierPoints(yModifiers));
                if (comparedPoints != 0)
                    return comparedPoints;

                return comparedPoints != 0 ? comparedPoints : GetName(x).CompareTo(GetName(y));
            }

            private int GetRankPoints(MemberDeclarationSyntax node)
            {
                int points;
                if (!typeRank.TryGetValue(node.GetType(), out points))
                    return 0;
                return points;
            }

            private SyntaxTokenList GetModifiers(MemberDeclarationSyntax node)
            {
                var nodeType = node.GetType();

                if (node is BaseMethodDeclarationSyntax)
                    return (node as BaseMethodDeclarationSyntax).Modifiers;

                if (node is BaseFieldDeclarationSyntax)
                    return (node as BaseFieldDeclarationSyntax).Modifiers;

                if (node is DelegateDeclarationSyntax)
                    return (node as DelegateDeclarationSyntax).Modifiers;

                if (node is BasePropertyDeclarationSyntax)
                    return (node as BasePropertyDeclarationSyntax).Modifiers;

                return default(SyntaxTokenList);
            }

            private int GetSpecialModifierPoints(SyntaxTokenList tokenList) => SumRankPoints(tokenList, specialModifierRank, 100);

            private int GetAccessLevelPoints(SyntaxTokenList tokenList) => SumRankPoints(tokenList, accessLevelRank, accessLevelRank[SyntaxKind.PrivateKeyword]);

            private int SumRankPoints(SyntaxTokenList tokenList, Dictionary<SyntaxKind, int> rank, int defaultSumValue)
            {
                var points = tokenList
                        .Select(s => s.Kind())
                        .Sum(tokenKind => rank.ContainsKey(tokenKind) ? rank[tokenKind] : 0);
                return points == 0 ? defaultSumValue : points;
            }

            private string GetName(SyntaxNode node)
            {
                if (node is FieldDeclarationSyntax)
                    return GetName((node as FieldDeclarationSyntax).Declaration);

                if (node is EventFieldDeclarationSyntax)
                    return GetName((node as EventFieldDeclarationSyntax).Declaration);

                if (node is OperatorDeclarationSyntax)
                    return (node as OperatorDeclarationSyntax).OperatorToken.Text;

                if (node is IndexerDeclarationSyntax)
                    return "this";

                if (node is PropertyDeclarationSyntax)
                    return (node as PropertyDeclarationSyntax).Identifier.Text;

                if (node is MethodDeclarationSyntax)
                    return (node as MethodDeclarationSyntax).Identifier.Text;

                if (node is ConstructorDeclarationSyntax)
                    return (node as ConstructorDeclarationSyntax).Identifier.Text;

                if (node is DestructorDeclarationSyntax)
                    return (node as DestructorDeclarationSyntax).Identifier.Text;

                if (node is DelegateDeclarationSyntax)
                    return (node as DelegateDeclarationSyntax).Identifier.Text;

                if (node is EventDeclarationSyntax)
                    return (node as EventDeclarationSyntax).Identifier.Text;

                if (node is BaseTypeDeclarationSyntax)
                    return (node as BaseTypeDeclarationSyntax).Identifier.Text;

                return "";
            }

            private string GetName(VariableDeclarationSyntax declaration)
            {
                var str = new StringBuilder();
                declaration.Variables.Aggregate(str, (accumulate, seed) => accumulate.Append(seed.Identifier.Text));
                return str.ToString();
            }
        }
    }
}