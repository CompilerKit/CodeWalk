using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.PatternMatching;

namespace ManualILSpy.Extention
{
    public class JsonCSharpVisitor : IAstVisitor
    {
        readonly JsonTokenWriter writer;
        readonly Stack<AstNode> containerStack = new Stack<AstNode>();
        string testLog = "test";
        public JsonCSharpVisitor(JsonTokenWriter textWriter)
        {
            if (textWriter == null)
            {
                throw new ArgumentNullException("textWriter");
            }
            writer = textWriter;
            methodNum = 0;
        }

        #region Write tokens
        bool isAtStartOfLine = true;

        void WriteKeyword(string token, Role tokenRole = null)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("WriteKeyword", testLog));
            writer.WritePairValue("keyword", '"' + token + '"');
            //1. write modifier
            //writer.WriteKeyword(tokenRole, token);
            //isAtStartOfLine = false;
        }

        void WriteToken(TokenRole tokenRole)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("WriteToken", testLog));
            //WriteToken(tokenRole.Token, tokenRole); b
        }

        void WriteToken(string token, Role tokenRole)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("WriteToken with 2 agrm", testLog));
            //writer.WriteToken(tokenRole, token);
            //isAtStartOfLine = false;
        }

        void WriteModifiers(IEnumerable<CSharpModifierToken> modifierTokens)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("WriteModifier", testLog));
            List<CSharpModifierToken> modifierList = new List<CSharpModifierToken>();
            foreach (CSharpModifierToken modifier in modifierTokens)
            {
                modifierList.Add(modifier);
                //modifier.AcceptVisitor(this);
            }
            int count = modifierList.Count;
            int lastIndex = count - 1;
            writer.WriteKey("modifier");
            if (count > 1)
            {
                writer.OpenArrayBrace();
                for (int i = 0; i < count; i++)
                {
                    modifierList[i].AcceptVisitor(this);
                    if (i < lastIndex)
                    {
                        WriteComma();
                    }
                }
                writer.CloseArrayBrace();
            }
            else if(count == 1)
            {
                modifierList[0].AcceptVisitor(this);
            }
            else//count = 0;
            {
                writer.WriteValue("null");
            }
        }

        void WriteMethodBody(BlockStatement body)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("WriteMethodBody", testLog));
            if (body.IsNull)
            {
                writer.WritePairValue("body", "null");
            }
            else
            {
                writer.WriteKey("body");
                writer.OpenObjectBrace();
                VisitBlockStatement(body);
                writer.CloseObjectBrace();
            }
        }

        void WriteMethodTypeInfo(List<string> typeInfoList)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("WriteMethodTypeInfo", testLog));
            writer.WriteKey("typeInfoList");
            
            int count = typeInfoList.Count;
            bool isFirst = true;
            for(int i = 0; i < count; i++)
            {
                if (isFirst)
                {
                    writer.OpenArrayBrace();
                    isFirst = false;
                }
                else
                {
                    WriteComma();
                }
                writer.WriteValue('"' + typeInfoList[i] + '"');
            }
            if(!isFirst)
                writer.CloseArrayBrace();
        }

        void WriteIdentifier(Identifier identifier)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("WriteIdentifier", testLog));
            writer.WriteValue('"' + identifier.Name + '"');
        }

        void WriteCommaSeparatedList(IEnumerable<AstNode> list)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("WriteCommaSeparatedList", testLog));
            bool isFirst = true;
            foreach (AstNode node in list)
            {
                if (isFirst)
                {
                    WriteComma();
                    writer.WriteKey("arguments");
                    isFirst = false;
                }
                else
                {
                    WriteComma();
                }
                node.AcceptVisitor(this);
            }
        }

        void WriteEmbeddedStatement(Statement embeddedStatement)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("WriteEmbeddedStatement", testLog));
            if (embeddedStatement.IsNull)
            {
                //NewLine();
                return;
            }
            BlockStatement block = embeddedStatement as BlockStatement;
            if (block != null)
            {
                VisitBlockStatement(block);
            }
            else
            {
                //NewLine();
                //writer.Indent();
                embeddedStatement.AcceptVisitor(this);
                //writer.Unindent();
            }
        }
        #endregion

        #region Write Json Format

        void WriteJsonObject()
        {
            writer.OpenObjectBrace();

            writer.CloseObjectBrace();
        }

        void WriteJsonMember()
        {
        }

        void WriteJsonPair(string key, string value)
        {
            writer.WritePairValue(key, value);
        }

        void WriteJsonArray(string key, List<string> values)//List<JsonValue>
        {
            writer.WriteComment(MyDebugWriter.LogReturn("WriteJsonArray", testLog));
            int lastIndex = values.Count - 1;
            writer.OpenArrayBrace();
            for(int i = 0; i < values.Count; i++)
            {
                WriteJsonValue(values[i]);
                if (i < lastIndex)
                {
                    WriteComma();
                }
            }
            writer.CloseArrayBrace();
        }

        void WriteJsonElement()
        {
        }

        void WriteJsonValue(string value)
        {
            writer.WriteValue('"' + value + '"');
        }

        void WriteComma()
        {
            writer.Comma();
        }

        #endregion

        #region StartNode/EndNode
        void StartNode(AstNode node)
        {
            Debug.Assert(containerStack.Count == 0 || node.Parent == containerStack.Peek() || containerStack.Peek().NodeType == NodeType.Pattern);
            containerStack.Push(node);
            //writer.StartNode(node);
        }

        void EndNode(AstNode node)
        {
            Debug.Assert(node == containerStack.Peek());
            containerStack.Pop();
            //writer.EndNode(node);
        }
        #endregion

        #region Expressions
        public void VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitAnonymousMethodExpression", testLog));
            throw new NotImplementedException();
        }

        public void VisitUndocumentedExpression(UndocumentedExpression undocumentedExpression)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitUndocumentedExpression", testLog));
            throw new NotImplementedException();
        }

        public void VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitUndocumentedExpression", testLog));
            throw new NotImplementedException();
        }

        public void VisitArrayInitializerExpression(ArrayInitializerExpression arrayInitializerExpression)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitArrayInitializerExpression", testLog));
            throw new NotImplementedException();
        }

        public void VisitAsExpression(AsExpression asExpression)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitAsExpression", testLog));
            throw new NotImplementedException();
        }

        public void VisitAssignmentExpression(AssignmentExpression assignmentExpression)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitAssignmentExpression", testLog));
            //StartNode(assignmentExpression);
            assignmentExpression.Left.AcceptVisitor(this);
            //Space(policy.SpaceAroundAssignment);
            WriteToken(AssignmentExpression.GetOperatorRole(assignmentExpression.Operator));
            //Space(policy.SpaceAroundAssignment);
            assignmentExpression.Right.AcceptVisitor(this);
            //EndNode(assignmentExpression);
        }

        public void VisitBaseReferenceExpression(BaseReferenceExpression baseReferenceExpression)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitBaseReferenceExpression", testLog));
            throw new NotImplementedException();
        }

        public void VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitBinaryOperatorExpression", testLog));
            binaryOperatorExpression.Left.AcceptVisitor(this);
            WriteToken(BinaryOperatorExpression.GetOperatorRole(binaryOperatorExpression.Operator));
            binaryOperatorExpression.Right.AcceptVisitor(this);

            //StartNode(binaryOperatorExpression);
            //binaryOperatorExpression.Left.AcceptVisitor(this);
            //bool spacePolicy;
            //switch (binaryOperatorExpression.Operator)
            //{
            //    case BinaryOperatorType.BitwiseAnd:
            //    case BinaryOperatorType.BitwiseOr:
            //    case BinaryOperatorType.ExclusiveOr:
            //        spacePolicy = policy.SpaceAroundBitwiseOperator;
            //        break;
            //    case BinaryOperatorType.ConditionalAnd:
            //    case BinaryOperatorType.ConditionalOr:
            //        spacePolicy = policy.SpaceAroundLogicalOperator;
            //        break;
            //    case BinaryOperatorType.GreaterThan:
            //    case BinaryOperatorType.GreaterThanOrEqual:
            //    case BinaryOperatorType.LessThanOrEqual:
            //    case BinaryOperatorType.LessThan:
            //        spacePolicy = policy.SpaceAroundRelationalOperator;
            //        break;
            //    case BinaryOperatorType.Equality:
            //    case BinaryOperatorType.InEquality:
            //        spacePolicy = policy.SpaceAroundEqualityOperator;
            //        break;
            //    case BinaryOperatorType.Add:
            //    case BinaryOperatorType.Subtract:
            //        spacePolicy = policy.SpaceAroundAdditiveOperator;
            //        break;
            //    case BinaryOperatorType.Multiply:
            //    case BinaryOperatorType.Divide:
            //    case BinaryOperatorType.Modulus:
            //        spacePolicy = policy.SpaceAroundMultiplicativeOperator;
            //        break;
            //    case BinaryOperatorType.ShiftLeft:
            //    case BinaryOperatorType.ShiftRight:
            //        spacePolicy = policy.SpaceAroundShiftOperator;
            //        break;
            //    case BinaryOperatorType.NullCoalescing:
            //        spacePolicy = true;
            //        break;
            //    default:
            //        throw new NotSupportedException("Invalid value for BinaryOperatorType");
            //}
            //Space(spacePolicy);
            //WriteToken(BinaryOperatorExpression.GetOperatorRole(binaryOperatorExpression.Operator));
            //Space(spacePolicy);
            //binaryOperatorExpression.Right.AcceptVisitor(this);
            //EndNode(binaryOperatorExpression);
        }

        public void VisitCastExpression(CastExpression castExpression)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitCastExpression", testLog));
            throw new NotImplementedException();
        }

        public void VisitCheckedExpression(CheckedExpression checkedExpression)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitCheckedExpression", testLog));
            throw new NotImplementedException();
        }

        public void VisitConditionalExpression(ConditionalExpression conditionalExpression)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitConditionalExpression", testLog));
            throw new NotImplementedException();
        }

        public void VisitDefaultValueExpression(DefaultValueExpression defaultValueExpression)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitDefaultValueExpression", testLog));
            throw new NotImplementedException();
        }

        public void VisitDirectionExpression(DirectionExpression directionExpression)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitDirectionExpression", testLog));
            throw new NotImplementedException();
        }

        public void VisitIdentifierExpression(IdentifierExpression identifierExpression)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitIdentifierExpression", testLog));
            WriteIdentifier(identifierExpression.IdentifierToken);
            //WriteTypeArguments(identifierExpression.TypeArguments);
        }

        public void VisitIndexerExpression(IndexerExpression indexerExpression)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitIndexerExpression", testLog));
            throw new NotImplementedException();
        }

        public void VisitInvocationExpression(InvocationExpression invocationExpression)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitInvocationExpression", testLog));
            invocationExpression.Target.AcceptVisitor(this);
            //WriteComma();
            WriteCommaSeparatedList(invocationExpression.Arguments);
        }

        public void VisitIsExpression(IsExpression isExpression)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitIsExpression", testLog));
            throw new NotImplementedException();
        }

        public void VisitLambdaExpression(LambdaExpression lambdaExpression)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitLambdaExpression", testLog));
            throw new NotImplementedException();
        }

        public void VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitMemberReferenceExpression", testLog));
            memberReferenceExpression.Target.AcceptVisitor(this);
            WriteIdentifier(memberReferenceExpression.MemberNameToken);
        }

        public void VisitNamedArgumentExpression(NamedArgumentExpression namedArgumentExpression)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitNamedArgumentExpression", testLog));
            throw new NotImplementedException();
        }

        public void VisitNamedExpression(NamedExpression namedExpression)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitNamedExpression", testLog));
            throw new NotImplementedException();
        }

        public void VisitNullReferenceExpression(NullReferenceExpression nullReferenceExpression)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitNullReferenceExpression", testLog));
            writer.WriteValue("null");
            //writer.WritePrimitiveValue(null);
        }

        public void VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitObjectCreateExpression", testLog));
            WriteKeyword(ObjectCreateExpression.NewKeywordRole.Token);
            WriteComma();
            objectCreateExpression.Type.AcceptVisitor(this);
            bool useParenthesis = objectCreateExpression.Arguments.Any() || objectCreateExpression.Initializer.IsNull;
            if (!objectCreateExpression.LParToken.IsNull)
            {
                useParenthesis = true;
            }
            if (useParenthesis)
            {
                //Space(policy.SpaceBeforeMethodCallParentheses);
                //WriteComma();
                WriteCommaSeparatedList(objectCreateExpression.Arguments);
            }
            objectCreateExpression.Initializer.AcceptVisitor(this);
            //StartNode(objectCreateExpression);
            //WriteKeyword(ObjectCreateExpression.NewKeywordRole);
            //objectCreateExpression.Type.AcceptVisitor(this);
            //bool useParenthesis = objectCreateExpression.Arguments.Any() || objectCreateExpression.Initializer.IsNull;
            //// also use parenthesis if there is an '(' token
            //if (!objectCreateExpression.LParToken.IsNull)
            //{
            //    useParenthesis = true;
            //}
            //if (useParenthesis)
            //{
            //    Space(policy.SpaceBeforeMethodCallParentheses);
            //    WriteCommaSeparatedListInParenthesis(objectCreateExpression.Arguments, policy.SpaceWithinMethodCallParentheses);
            //}
            //objectCreateExpression.Initializer.AcceptVisitor(this);
            //EndNode(objectCreateExpression);
        }

        public void VisitAnonymousTypeCreateExpression(AnonymousTypeCreateExpression anonymousTypeCreateExpression)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitAnonymousTypeCreateExpression", testLog));
            throw new NotImplementedException();
        }

        public void VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitParenthesizedExpression", testLog));
            throw new NotImplementedException();
        }

        public void VisitPointerReferenceExpression(PointerReferenceExpression pointerReferenceExpression)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitPointerReferenceExpression", testLog));
            throw new NotImplementedException();
        }

        #region VisitPrimitiveExpression
        public void VisitPrimitiveExpression(PrimitiveExpression primitiveExpression)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitPrimitiveExpression", testLog));
            //throw new NotImplementedException();
            writer.WriteKey("primitiveExpression");
            writer.WriteValue(primitiveExpression.Value.ToString());
            //writer.WritePrimitiveValue(primitiveExpression.Value, primitiveExpression.UnsafeLiteralValue);
        }
        #endregion

        public void VisitSizeOfExpression(SizeOfExpression sizeOfExpression)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitSizeOfExpression", testLog));
            throw new NotImplementedException();
        }

        public void VisitStackAllocExpression(StackAllocExpression stackAllocExpression)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitStackAllocExpression", testLog));
            throw new NotImplementedException();
        }

        public void VisitThisReferenceExpression(ThisReferenceExpression thisReferenceExpression)
        {
            throw new NotImplementedException();
        }

        public void VisitTypeOfExpression(TypeOfExpression typeOfExpression)
        {
            throw new NotImplementedException();
        }

        public void VisitTypeReferenceExpression(TypeReferenceExpression typeReferenceExpression)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitTypeReferenceExpression", testLog));
            typeReferenceExpression.Type.AcceptVisitor(this);
        }

        public void VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression)
        {
            throw new NotImplementedException();
        }

        public void VisitUncheckedExpression(UncheckedExpression uncheckedExpression)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Query Expressions
        public void VisitQueryContinuationClause(QueryContinuationClause queryContinuationClause)
        {
            throw new NotImplementedException();
        }

        public void VisitQueryExpression(QueryExpression queryExpression)
        {
            throw new NotImplementedException();
        }

        public void VisitQueryFromClause(QueryFromClause queryFromClause)
        {
            throw new NotImplementedException();
        }

        public void VisitQueryGroupClause(QueryGroupClause queryGroupClause)
        {
            throw new NotImplementedException();
        }

        public void VisitQueryJoinClause(QueryJoinClause queryJoinClause)
        {
            throw new NotImplementedException();
        }

        public void VisitQueryLetClause(QueryLetClause queryLetClause)
        {
            throw new NotImplementedException();
        }

        public void VisitQueryOrderClause(QueryOrderClause queryOrderClause)
        {
            throw new NotImplementedException();
        }

        public void VisitQueryOrdering(QueryOrdering queryOrdering)
        {
            throw new NotImplementedException();
        }

        public void VisitQuerySelectClause(QuerySelectClause querySelectClause)
        {
            throw new NotImplementedException();
        }

        public void VisitQueryWhereClause(QueryWhereClause queryWhereClause)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region GeneralScope
        public void VisitAttribute(ICSharpCode.NRefactory.CSharp.Attribute attribute)
        {
            throw new NotImplementedException();
        }

        public void VisitAttributeSection(AttributeSection attributeSection)
        {
            throw new NotImplementedException();
        }

        public void VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration)
        {
            throw new NotImplementedException();
        }

        public void VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration)
        {
            throw new NotImplementedException();
        }

        public void VisitTypeDeclaration(TypeDeclaration typeDeclaration)
        {
            throw new NotImplementedException();
        }

        public void VisitUsingAliasDeclaration(UsingAliasDeclaration usingAliasDeclaration)
        {
            throw new NotImplementedException();
        }

        public void VisitUsingDeclaration(UsingDeclaration usingDeclaration)
        {
            throw new NotImplementedException();
        }

        public void VisitExternAliasDeclaration(ExternAliasDeclaration externAliasDeclaration)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Statements

        public void VisitBlockStatement(BlockStatement blockStatement)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitBlockStatement", testLog));
            writer.WriteKey("statementList");
            
            bool isFirst = true;
            foreach (var node in blockStatement.Statements)
            {
                if (isFirst)
                {
                    writer.OpenArrayBrace();
                    isFirst = false;
                }
                else
                {
                    WriteComma();
                }
                writer.OpenObjectBrace();
                node.AcceptVisitor(this);
                writer.CloseObjectBrace();
            }
            if (!isFirst)
                writer.CloseArrayBrace();
            else
                writer.WriteValue(null);
        }

        public void VisitBreakStatement(BreakStatement breakStatement)
        {
            throw new NotImplementedException();
        }

        public void VisitCheckedStatement(CheckedStatement checkedStatement)
        {
            throw new NotImplementedException();
        }

        public void VisitContinueStatement(ContinueStatement continueStatement)
        {
            throw new NotImplementedException();
        }

        public void VisitDoWhileStatement(DoWhileStatement doWhileStatement)
        {
            throw new NotImplementedException();
        }

        public void VisitEmptyStatement(EmptyStatement emptyStatement)
        {
            throw new NotImplementedException();
        }

        public void VisitExpressionStatement(ExpressionStatement expressionStatement)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitExpressionStatement", testLog));
            expressionStatement.Expression.AcceptVisitor(this);
        }

        public void VisitFixedStatement(FixedStatement fixedStatement)
        {
            throw new NotImplementedException();
        }

        public void VisitForeachStatement(ForeachStatement foreachStatement)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitForeachStatement", testLog));
            //writer.OpenObjectBrace();
            writer.WritePairValue("type", "\"ForeachStatement\"");
            WriteComma();
            WriteKeyword(ForeachStatement.ForeachKeywordRole.Token);
            WriteComma();
            writer.WriteKey("local-variable-type");
            foreachStatement.VariableType.AcceptVisitor(this);
            WriteComma();
            writer.WriteKey("identifiername");
            WriteIdentifier(foreachStatement.VariableNameToken);
            WriteKeyword(ForeachStatement.InKeywordRole.Token);
            WriteComma();
            writer.WriteKey("foreach-in-expression");
            foreachStatement.InExpression.AcceptVisitor(this);
            WriteComma();
            WriteEmbeddedStatement(foreachStatement.EmbeddedStatement);
            //writer.CloseObjectBrace();
        }

        public void VisitForStatement(ForStatement forStatement)
        {
            throw new NotImplementedException();
        }

        public void VisitGotoCaseStatement(GotoCaseStatement gotoCaseStatement)
        {
            throw new NotImplementedException();
        }

        public void VisitGotoDefaultStatement(GotoDefaultStatement gotoDefaultStatement)
        {
            throw new NotImplementedException();
        }

        public void VisitGotoStatement(GotoStatement gotoStatement)
        {
            throw new NotImplementedException();
        }

        public void VisitIfElseStatement(IfElseStatement ifElseStatement)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitIfElseStatement", testLog));
            WriteKeyword(IfElseStatement.IfKeywordRole.Token);
            ifElseStatement.Condition.AcceptVisitor(this);
            WriteEmbeddedStatement(ifElseStatement.TrueStatement);
            if (!ifElseStatement.FalseStatement.IsNull)
            {
                WriteKeyword(IfElseStatement.ElseKeywordRole.Token);
                if (ifElseStatement.FalseStatement is IfElseStatement)
                {
                    // don't put newline between 'else' and 'if'
                    ifElseStatement.FalseStatement.AcceptVisitor(this);
                }
                else
                {
                    WriteEmbeddedStatement(ifElseStatement.FalseStatement);
                }
            }
            //StartNode(ifElseStatement);
            //WriteKeyword(IfElseStatement.IfKeywordRole);
            //Space(policy.SpaceBeforeIfParentheses);
            //LPar();
            //Space(policy.SpacesWithinIfParentheses);
            //ifElseStatement.Condition.AcceptVisitor(this);
            //Space(policy.SpacesWithinIfParentheses);
            //RPar();
            //WriteEmbeddedStatement(ifElseStatement.TrueStatement);
            //if (!ifElseStatement.FalseStatement.IsNull)
            //{
            //    WriteKeyword(IfElseStatement.ElseKeywordRole);
            //    if (ifElseStatement.FalseStatement is IfElseStatement)
            //    {
            //        // don't put newline between 'else' and 'if'
            //        ifElseStatement.FalseStatement.AcceptVisitor(this);
            //    }
            //    else
            //    {
            //        WriteEmbeddedStatement(ifElseStatement.FalseStatement);
            //    }
            //}
            //EndNode(ifElseStatement);
        }

        public void VisitLabelStatement(LabelStatement labelStatement)
        {
            throw new NotImplementedException();
        }

        public void VisitLockStatement(LockStatement lockStatement)
        {
            throw new NotImplementedException();
        }

        public void VisitReturnStatement(ReturnStatement returnStatement)
        {
            throw new NotImplementedException();
        }

        public void VisitSwitchSection(SwitchSection switchSection)
        {
            throw new NotImplementedException();
        }

        public void VisitSwitchStatement(SwitchStatement switchStatement)
        {
            throw new NotImplementedException();
        }

        public void VisitCaseLabel(CaseLabel caseLabel)
        {
            throw new NotImplementedException();
        }

        public void VisitThrowStatement(ThrowStatement throwStatement)
        {
            throw new NotImplementedException();
        }

        public void VisitTryCatchStatement(TryCatchStatement tryCatchStatement)
        {
            throw new NotImplementedException();
        }

        public void VisitCatchClause(CatchClause catchClause)
        {
            throw new NotImplementedException();
        }

        public void VisitUncheckedStatement(UncheckedStatement uncheckedStatement)
        {
            throw new NotImplementedException();
        }

        public void VisitUnsafeStatement(UnsafeStatement unsafeStatement)
        {
            throw new NotImplementedException();
        }

        public void VisitUsingStatement(UsingStatement usingStatement)
        {
            throw new NotImplementedException();
        }
        
        public void VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitVariableDeclarationStatement", testLog));
            writer.WriteKey("statementType");
            writer.WriteValue("\"VariableDeclarationStatement\"");
            WriteComma();
            WriteModifiers(variableDeclarationStatement.GetChildrenByRole(VariableDeclarationStatement.ModifierRole));
            WriteComma();
            variableDeclarationStatement.Type.AcceptVisitor(this);
            //WriteComma();
            WriteCommaSeparatedList(variableDeclarationStatement.Variables);
        }

        public void VisitWhileStatement(WhileStatement whileStatement)
        {
            throw new NotImplementedException();
        }

        public void VisitYieldBreakStatement(YieldBreakStatement yieldBreakStatement)
        {
            throw new NotImplementedException();
        }

        public void VisitYieldReturnStatement(YieldReturnStatement yieldReturnStatement)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region TypeMembers

        public void VisitAccessor(Accessor accessor)
        {
            throw new NotImplementedException();
        }

        public void VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration)
        {
            if (methodNum > 0)
            {
                typeInfo.Clear();
                typeInfoIndex.Clear();
                WriteComma();
                writer.WriteLine();
            }
            writer.OpenObjectBrace();
            writer.WriteComment(MyDebugWriter.LogReturn("VisitConstructorDeclaration", testLog));
            //WriteAttributes(constructorDeclaration.Attributes);
            WriteModifiers(constructorDeclaration.ModifierTokens);
            TypeDeclaration type = constructorDeclaration.Parent as TypeDeclaration;
            if (type != null && type.Name != constructorDeclaration.Name)
                WriteIdentifier((Identifier)type.NameToken.Clone());
            else
                WriteIdentifier(constructorDeclaration.NameToken);
            //Space(policy.SpaceBeforeConstructorDeclarationParentheses);
            //WriteComma();
            WriteCommaSeparatedList(constructorDeclaration.Parameters);
            if (!constructorDeclaration.Initializer.IsNull)
            {
                //Space();
                constructorDeclaration.Initializer.AcceptVisitor(this);
            }
            WriteMethodBody(constructorDeclaration.Body);
            writer.CloseObjectBrace();
        }

        public void VisitConstructorInitializer(ConstructorInitializer constructorInitializer)
        {
            throw new NotImplementedException();
        }

        public void VisitDestructorDeclaration(DestructorDeclaration destructorDeclaration)
        {
            throw new NotImplementedException();
        }

        public void VisitEnumMemberDeclaration(EnumMemberDeclaration enumMemberDeclaration)
        {
            throw new NotImplementedException();
        }

        public void VisitEventDeclaration(EventDeclaration eventDeclaration)
        {
            throw new NotImplementedException();
        }

        public void VisitCustomEventDeclaration(CustomEventDeclaration customEventDeclaration)
        {
            throw new NotImplementedException();
        }

        public void VisitFieldDeclaration(FieldDeclaration fieldDeclaration)
        {
            throw new NotImplementedException();
        }

        public void VisitFixedFieldDeclaration(FixedFieldDeclaration fixedFieldDeclaration)
        {
            throw new NotImplementedException();
        }

        public void VisitFixedVariableInitializer(FixedVariableInitializer fixedVariableInitializer)
        {
            throw new NotImplementedException();
        }

        public void VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration)
        {
            throw new NotImplementedException();
        }
        int methodNum;
        List<string> typeInfo;
        Dictionary<string, int> typeInfoIndex = new Dictionary<string, int>();
        public void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
        {
            if (methodNum > 0)
            {
                typeInfo.Clear();
                typeInfoIndex.Clear();
                WriteComma();
                writer.WriteLine();
            }
            writer.WriteComment(MyDebugWriter.LogReturn("VisitMethodDeclaration", testLog));
            methodNum++;
            writer.OpenObjectBrace();
            writer.WriteKey("method");
            writer.OpenObjectBrace();
            WriteJsonPair("name", '"' + methodDeclaration.Name + '"');
            WriteComma();
            WriteModifiers(methodDeclaration.ModifierTokens);
            WriteComma();
            writer.WriteKey("returnType");
            methodDeclaration.ReturnType.AcceptVisitor(this);
            //WriteComma();
            WriteCommaSeparatedList(methodDeclaration.Parameters);
            WriteComma();
            WriteMethodBody(methodDeclaration.Body);
            writer.CloseObjectBrace();
            typeInfo = new List<string>(typeInfoIndex.Keys);
            WriteComma();
            WriteMethodTypeInfo(typeInfo);
            writer.CloseObjectBrace();
        }

        public void VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration)
        {
            throw new NotImplementedException();
        }

        public void VisitParameterDeclaration(ParameterDeclaration parameterDeclaration)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitParameterDeclaration", testLog));
            writer.OpenObjectBrace();
            //parameterDeclaration.Type.AcceptVisitor(this);
            int temp;
            string type = parameterDeclaration.Type.ToString();
            if (!typeInfoIndex.TryGetValue(type, out temp))
            {
                temp = typeInfoIndex.Count;
                typeInfoIndex.Add(type, temp);
                
            }
            writer.WritePairValue("typeInfo", temp.ToString());
            WriteComma();
            writer.WritePairValue("name", '"' + parameterDeclaration.Name + '"');
            //writer.WriteValue(parameterDeclaration.Name);
            writer.CloseObjectBrace();
        }

        public void VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Other Node

        public void VisitVariableInitializer(VariableInitializer variableInitializer)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitVariableInitializer", testLog));
            writer.OpenObjectBrace();
            writer.WriteKey("variable-name");
            WriteIdentifier(variableInitializer.NameToken);
            if (!variableInitializer.Initializer.IsNull)
            {
                //Space(policy.SpaceAroundAssignment);
                //WriteToken(Roles.Assign);
                //Space(policy.SpaceAroundAssignment);
                WriteComma();
                variableInitializer.Initializer.AcceptVisitor(this);
            }
            writer.CloseObjectBrace();
        }

        public void VisitSimpleType(SimpleType simpleType)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitSimpleType", testLog));
            int temp;
            string name = simpleType.ToString();
            if (!typeInfoIndex.TryGetValue(name, out temp))
            {
                temp = typeInfoIndex.Count;
                typeInfoIndex.Add(name, typeInfoIndex.Count);
            }
            writer.WritePairValue("typeInfo", temp.ToString());
            //StartNode(simpleType);
            //WriteIdentifier(simpleType.IdentifierToken);
            //WriteTypeArguments(simpleType.TypeArguments);
            //EndNode(simpleType);
        }

        public void VisitSyntaxTree(SyntaxTree syntaxTree)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitSyntaxTree", testLog));
            foreach (AstNode node in syntaxTree.Children)
            {
                node.AcceptVisitor(this);
            }
        }

        public void VisitMemberType(MemberType memberType)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitMemberType", testLog));
            memberType.Target.AcceptVisitor(this);
            //if (memberType.IsDoubleColon)
            //{
            //    WriteToken(Roles.DoubleColon);
            //}
            //else
            //{
            //    WriteToken(Roles.Dot);
            //}
            WriteIdentifier(memberType.MemberNameToken);
        }

        public void VisitComposedType(ComposedType composedType)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitComposedType", testLog));
            composedType.BaseType.AcceptVisitor(this);
        }

        public void VisitArraySpecifier(ArraySpecifier arraySpecifier)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitArraySpecifier", testLog));
            WriteToken(Roles.LBracket);
            foreach (var comma in arraySpecifier.GetChildrenByRole(Roles.Comma))
            {
                WriteComma();
                //writer.WriteToken(Roles.Comma, ",");
            }
            WriteToken(Roles.RBracket);
        }

        public void VisitPrimitiveType(PrimitiveType primitiveType)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitPrimitiveType", testLog));
            //write return type
            writer.WriteValue('"' + primitiveType.Keyword + '"');
        }

        public void VisitComment(Comment comment)
        {
            throw new NotImplementedException();
        }

        public void VisitNewLine(NewLineNode newLineNode)
        {
            throw new NotImplementedException();
        }

        public void VisitWhitespace(WhitespaceNode whitespaceNode)
        {
            throw new NotImplementedException();
        }

        public void VisitPreProcessorDirective(PreProcessorDirective preProcessorDirective)
        {
            throw new NotImplementedException();
        }

        public void VisitText(TextNode textNode)
        {
            throw new NotImplementedException();
        }

        public void VisitTypeParameterDeclaration(TypeParameterDeclaration typeParameterDeclaration)
        {
            throw new NotImplementedException();
        }

        public void VisitConstraint(Constraint constraint)
        {
            throw new NotImplementedException();
        }

        public void VisitCSharpTokenNode(CSharpTokenNode cSharpTokenNode)
        {
            writer.WriteComment(MyDebugWriter.LogReturn("VisitCSharpTokenNode", testLog));
            CSharpModifierToken mod = cSharpTokenNode as CSharpModifierToken;
            if (mod != null)
            {
                // ITokenWriter assumes that each node processed between a
                // StartNode(parentNode)-EndNode(parentNode)-pair is a child of parentNode.
                WriteJsonValue(CSharpModifierToken.GetModifierName(mod.Modifier));
                //WriteKeyword(CSharpModifierToken.GetModifierName(mod.Modifier), cSharpTokenNode.Role);
            }
            else
            {
                throw new NotSupportedException("Should never visit individual tokens");
            }
        }

        public void VisitIdentifier(Identifier identifier)
        {
            throw new NotImplementedException();
        }

        void IAstVisitor.VisitErrorNode(AstNode errorNode)
        {
            throw new NotImplementedException();
        }

        void IAstVisitor.VisitNullNode(AstNode nullNode)
        {
            //throw new NotImplementedException();
        }

        #endregion

        #region Pattern Nodes

        public void VisitPatternPlaceholder(AstNode placeholder, Pattern pattern)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Documentation Reference
        public void VisitDocumentationReference(DocumentationReference documentationReference)
        {
            throw new NotImplementedException();
        }
        #endregion

    }
}