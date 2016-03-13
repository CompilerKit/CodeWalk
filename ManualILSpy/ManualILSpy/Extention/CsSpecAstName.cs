using System;
using System.Diagnostics;
using System.Collections.Generic;
using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.PatternMatching;
using ICSharpCode.NRefactory.TypeSystem;
using ManualILSpy.Extention.Json;
using System.Linq;

namespace ManualILSpy.Extention
{
    static class CsSpecAstName
    {

        static Dictionary<Type, string> csSpecAstNames = new Dictionary<Type, string>();
        static CsSpecAstName()
        {

            //expressions 
            AddName<ArrayCreateExpression>("array-create-expression");
            AddName<AnonymousMethodExpression>("anonymous-method-expression");
            AddName<UndocumentedExpression>("undocumented-expression");
            AddName<ArrayInitializerExpression>("array-initializer-expression");
            AddName<AsExpression>("as-expression");
            AddName<AssignmentExpression>("assignment-expression");
            AddName<BaseReferenceExpression>("base-reference-expression");
            AddName<BinaryOperatorExpression>("binary-operator-expression");
            AddName<CastExpression>("cast-expression");
            AddName<CheckedExpression>("checked-expression");
            AddName<ConditionalExpression>("conditional-expression");
            AddName<DefaultValueExpression>("default-value-expression");
            AddName<DirectionExpression>("direction-expression");
            AddName<IdentifierExpression>("identifier-expression");
            AddName<IndexerExpression>("indexer-expression");
            AddName<InvocationExpression>("invocation");
            AddName<IsExpression>("is-expression");
            AddName<LambdaExpression>("lambda-expression");
            AddName<MemberReferenceExpression>("member-reference");
            AddName<NamedArgumentExpression>("named-argument-expression");
            AddName<NamedExpression>("named-expression");
            AddName<NullReferenceExpression>("null-reference");
            AddName<ObjectCreateExpression>("object-creation-expression");
            AddName<AnonymousTypeCreateExpression>("anonymous-type-create-expression");
            AddName<ParenthesizedExpression>("parenthesized-expression");
            AddName<PointerReferenceExpression>("pointer-reference-expression");
            AddName<PrimitiveExpression>("primitive-expression");
            AddName<SizeOfExpression>("sizeof-expression");
            AddName<StackAllocExpression>("stack-alloc-expression");
            AddName<ThisReferenceExpression>("this-reference-expression");
            AddName<TypeOfExpression>("typeof-expression");
            AddName<UnaryOperatorExpression>("unary-operator-expression");
            AddName<UncheckedExpression>("unchecked-expression");
            //------------------------------------------------------------------------

            //statements
            AddName<CheckedStatement>("checked-statement");
            AddName<ContinueStatement>("continue-statement");
            AddName<BreakStatement>("break-statement");
            AddName<BlockStatement>("block-statement");
            AddName<DoWhileStatement>("do-while-statement");
            AddName<EmptyStatement>("empty-statement");
            AddName<FixedStatement>("fixed-statement");
            AddName<ForeachStatement>("foreach-statement");
            AddName<ForStatement>("for-statement");
            AddName<GotoCaseStatement>("goto-case-statement");
            AddName<GotoDefaultStatement>("goto-default-statement");
            AddName<GotoStatement>("goto-statement");
            AddName<IfElseStatement>("if-else-statement");
            AddName<LabelStatement>("label-statement");
            AddName<LockStatement>("lock-statement");
            AddName<ReturnStatement>("lock-statement");
            AddName<SwitchStatement>("switch-statement");
            AddName<ThrowStatement>("throw-statement");
            AddName<TryCatchStatement>("try-catch-statement");
            AddName<UncheckedStatement>("unchecked-statement");
            AddName<UnsafeStatement>("unsafe-statement");
            AddName<UsingStatement>("using-statement");
            AddName<VariableDeclarationStatement>("variable-declaration");
            AddName<WhileStatement>("while-statement");
            AddName<YieldBreakStatement>("yield-break-statement");
            AddName<YieldReturnStatement>("yield-return-statement");

        }
        static void AddName<T>(string csSpecName)
        {
            Type t = typeof(T);
            csSpecAstNames.Add(t, csSpecName);
        }
        public static string GetCsSpecName<T>()
        {
            Type t = typeof(T);
            string found;
            if (csSpecAstNames.TryGetValue(t, out found))
            {
                return found;
            }
            return "";
        }
    }
}
