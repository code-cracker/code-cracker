Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Refactoring

    <ExportCodeFixProvider("StyleCopAllowMembersOrderingCodeFixProvider", LanguageNames.VisualBasic)>
    <[Shared]>
    Public Class StyleCopAllowMembersOrderingCodeFixProvider
        Inherits BaseAllowMembersOrderingCodeFixProvider

        Public Sub New()
            MyBase.New("Order {0}'s members following StyleCop patterns")
        End Sub

        Protected Overrides Function GetMemberDeclarationComparer(document As Document, cancellationToken As CancellationToken) As IComparer(Of DeclarationStatementSyntax)
            Return New StyleCopMembersComparer
        End Function

        Public Class StyleCopMembersComparer
            Implements IComparer(Of DeclarationStatementSyntax)

            ReadOnly typeRank As New Dictionary(Of Type, Integer) From
                {
                    {GetType(FieldDeclarationSyntax), 1},
                    {GetType(ConstructorBlockSyntax), 2},
                    {GetType(DelegateStatementSyntax), 4},
                    {GetType(EventStatementSyntax), 5},
                    {GetType(EventBlockSyntax), 6},
                    {GetType(EnumBlockSyntax), 7},
                    {GetType(InterfaceBlockSyntax), 8},
                    {GetType(PropertyBlockSyntax), 9},
                    {GetType(PropertyStatementSyntax), 9}, ' For VB Auto Properties
                    {GetType(OperatorBlockSyntax), 11},
                    {GetType(MethodBlockSyntax), 12},
                    {GetType(StructureBlockSyntax), 13},
                    {GetType(ClassBlockSyntax), 14}
                }

            ReadOnly specialModifierRank As New Dictionary(Of SyntaxKind, Integer) From
                {
                    {SyntaxKind.ConstKeyword, 1},
                    {SyntaxKind.SharedKeyword, 2}
                }

            ReadOnly accessLevelRank As New Dictionary(Of SyntaxKind, Integer) From
                {
                    {SyntaxKind.PublicKeyword, -4},
                    {SyntaxKind.FriendKeyword, -2},
                    {SyntaxKind.ProtectedKeyword, 1},
                    {SyntaxKind.PrivateKeyword, 2}
                }

            Public Function Compare(x As DeclarationStatementSyntax, y As DeclarationStatementSyntax) As Integer Implements IComparer(Of DeclarationStatementSyntax).Compare
                If x Is Nothing AndAlso y Is Nothing Then Return 0
                If x Is Nothing Then Return 1
                If y Is Nothing Then Return -1
                If x.Equals(y) Then Return 0

                Dim comparedPoints = GetRankPoints(x).CompareTo(GetRankPoints(y))
                    If comparedPoints <> 0 Then Return comparedPoints

                Dim xModifiers = GetModifiers(x)
                Dim yModifiers = GetModifiers(y)
                comparedPoints = GetAccessLevelPoints(xModifiers).CompareTo(GetAccessLevelPoints(yModifiers))
                If comparedPoints <> 0 Then Return comparedPoints

                comparedPoints = GetSpecialModifierPoints(xModifiers).CompareTo(GetSpecialModifierPoints(yModifiers))
                If comparedPoints <> 0 Then Return comparedPoints

                Return GetName(x).CompareTo(GetName(y))
            End Function

            Private Function GetAccessLevelPoints(tokens As SyntaxTokenList) As Integer
                Return sumrankPoints(tokens, accessLevelRank, accessLevelRank(SyntaxKind.PrivateKeyword))
            End Function

            Private Function GetModifiers(node As DeclarationStatementSyntax) As SyntaxTokenList
                If TypeOf node Is MethodBlockBaseSyntax Then
                    Return DirectCast(node, MethodBlockBaseSyntax).Begin.Modifiers
                End If
                If TypeOf node Is FieldDeclarationSyntax Then
                    Return DirectCast(node, FieldDeclarationSyntax).Modifiers
                End If
                If TypeOf node Is DelegateStatementSyntax Then
                    Return DirectCast(node, DelegateStatementSyntax).Modifiers
                End If
                If TypeOf node Is PropertyStatementSyntax Then
                    Return DirectCast(node, PropertyStatementSyntax).Modifiers
                End If
                If TypeOf node Is PropertyBlockSyntax Then
                    Return DirectCast(node, PropertyBlockSyntax).PropertyStatement.Modifiers
                End If
                Return New SyntaxTokenList()
            End Function

            Private Function GetRankPoints(node As DeclarationStatementSyntax) As Integer
                Dim points = 0
                If Not typeRank.TryGetValue(node.GetType(), points) Then
                    Return 0
                End If
                Return points
            End Function

            Private Function GetSpecialModifierPoints(tokens As SyntaxTokenList) As Integer
                Return SumRankPoints(tokens, specialModifierRank, 100)
            End Function

            Private Function SumRankPoints(tokens As SyntaxTokenList, rank As Dictionary(Of SyntaxKind, Integer), defaultSumValue As Integer) As Integer
                Dim points = tokens.Sum(Function(t) If(rank.ContainsKey(t.VBKind), rank(t.VBKind), 0))

                Return If(points = 0, defaultSumValue, points)
            End Function

            Private Function GetName(node As SyntaxNode) As String
                If TypeOf node Is FieldDeclarationSyntax Then
                    Return GetFieldName(DirectCast(node, FieldDeclarationSyntax).Declarators)
                End If
                If TypeOf node Is PropertyStatementSyntax Then
                    Return DirectCast(node, PropertyStatementSyntax).Identifier.Text
                End If
                If TypeOf node Is PropertyBlockSyntax Then
                    Return GetName(DirectCast(node, PropertyBlockSyntax).PropertyStatement)
                End If
                If TypeOf node Is MethodBlockSyntax Then
                    Return DirectCast(node, MethodBlockSyntax).Begin.Identifier.Text
                End If
                If TypeOf node Is SubNewStatementSyntax Then
                    Return "New"
                End If
                If TypeOf node Is EnumBlockSyntax Then
                    Return DirectCast(node, EnumBlockSyntax).EnumStatement.Identifier.Text
                End If
                If TypeOf node Is InterfaceBlockSyntax Then
                    Return DirectCast(node, InterfaceBlockSyntax).Begin.Identifier.Text
                End If
                If TypeOf node Is EventStatementSyntax Then
                    Return DirectCast(node, EventStatementSyntax).Identifier.Text
                End If
                If TypeOf node Is EventBlockSyntax Then
                    Return GetName(DirectCast(node, EventBlockSyntax).EventStatement)
                End If
                If TypeOf node Is OperatorBlockSyntax Then
                    Return DirectCast(node, OperatorBlockSyntax).Begin.OperatorToken.Text
                End If
                If TypeOf node Is DelegateStatementSyntax Then
                    Return DirectCast(node, DelegateStatementSyntax).Identifier.Text
                End If
                If TypeOf node Is ClassBlockSyntax Then
                    Return DirectCast(node, ClassBlockSyntax).Begin.Identifier.Text
                End If
                If TypeOf node Is StructureBlockSyntax Then
                    Return DirectCast(node, StructureBlockSyntax).Begin.Identifier.Text
                End If
                Return ""
            End Function
            Custom Event foo As Action
                AddHandler(value As Action)

                End AddHandler
                RemoveHandler(value As Action)

                End RemoveHandler
                RaiseEvent()

                End RaiseEvent
            End Event


            Private Function GetFieldName(declarations As SeparatedSyntaxList(Of VariableDeclaratorSyntax)) As String
                Dim names = From declaration In declarations
                            From name In declaration.Names
                            Select name.Identifier.Text
                Return String.Join("", names)
            End Function
        End Class
    End Class
End Namespace
