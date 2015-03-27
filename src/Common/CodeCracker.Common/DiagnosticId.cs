﻿namespace CodeCracker
{
    public enum DiagnosticId
    {
        None = 0,
        AlwaysUseVar = 1,
        ArgumentException = 2,
        CatchEmpty = 3,
        EmptyCatchBlock = 4,
        EmptyObjectInitializer = 5,
        ForInArray = 6,
        IfReturnTrue = 7,
        ObjectInitializer_LocalDeclaration = 8,
        ObjectInitializer_Assignment = 9,
        Regex = 10,
        RemoveWhereWhenItIsPossible = 11,
        RethrowException = 12,
        TernaryOperator_Return = 13,
        TernaryOperator_Assignment = 14,
        UnnecessaryParenthesis = 15,
        CopyEventToVariableBeforeFire = 16,
        ExistenceOperator = 18,
        ConvertToSwitch = 19,
        ConvertLambdaExpressionToMethodGroup = 20,
        NameOf = 21,
        DisposableVariableNotDisposed = 22,
        SealedAttribute = 23,
        StaticConstructorException = 24,
        EmptyFinalizer = 25,
        CallExtensionMethodAsExtension = 26,
        DisposablesShouldCallSuppressFinalize = 29,
        MakeLocalVariableConstWhenItIsPossible = 30,
        UseInvokeMethodToFireEvent = 31,
        DisposableFieldNotDisposed_Returned = 32,
        DisposableFieldNotDisposed_Created = 33,
        AllowMembersOrdering = 35,
        RemoveCommentedCode = 37,
        ConvertToExpressionBodiedMember = 38,
        StringBuilderInLoop = 39,
        InvertFor = 42,
        ParameterRefactory = 44,
        StringRepresentation_RegularString = 45,
        StringRepresentation_VerbatimString = 46,
        PropertyPrivateSet = 47,
        StringFormat = 48,
        SimplifyRedundantBooleanComparisons = 49,
        ReadonlyField = 52,
        JsonNet = 54,
        UnusedParameters = 57,
        AbstractClassShouldNotHavePublicCtors = 60,
        TaskNameAsync = 61,
        InterfaceName = 62,
        Uri = 63,
        IPAddress = 64,
        RemoveTrailingWhitespace = 65,
        VirtualMethodOnConstructor = 67,
        RemovePrivateMethodNeverUsed = 68,
        UseConfigureAwaitFalse = 70,
        IntroduceFieldFromConstructor = 71,
        RemoveAsyncFromMethod = 72,
        AddBracesToSwitchSections = 73,
        NoPrivateReadonlyField = 74,
        MergeNestedIf = 75,
        NumericLiteral = 79,
        TernaryOperator_Iif = 80
    }
}