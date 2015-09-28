using System;
using System.Diagnostics;
using System.Collections.Generic;
using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.PatternMatching;
using ManualILSpy.Extention.Json;
using System.Linq;

namespace ManualILSpy.Extention
{
    public class AstCSharpToJsonVisitor : IAstVisitor
    {
        Stack<JsonValue> jsonValueStack = new Stack<JsonValue>();
        public JsonValue LastValue { get; private set; }
        //JsonWriter writer;
        public AstCSharpToJsonVisitor(ITextOutput output)
        {
            //writer = new JsonWriter(output);
            methodNum = 0;
        }

        #region Get JsonValue

        JsonValue GetModifiers(IEnumerable<CSharpModifierToken> modifierTokens)
        {
            List<CSharpModifierToken> modifierList = new List<CSharpModifierToken>();
            foreach (CSharpModifierToken modifier in modifierTokens)
            {
                modifierList.Add(modifier);
            }
            int count = modifierList.Count;
            int lastIndex = count - 1;
            if (count > 1)
            {
                JsonArray modifierArr = new JsonArray();
                modifierArr.Comment = "GetModifiers";
                for (int i = 0; i < count; i++)
                {
                    modifierList[i].AcceptVisitor(this);
                    modifierArr.AddJsonValue(jsonValueStack.Pop());
                }
                return modifierArr;
            }
            else if (count == 1)
            {
                modifierList[0].AcceptVisitor(this);
                return jsonValueStack.Pop();
            }
            else//count = 0;
            {
                JsonElement nullElement = new JsonElement();
                nullElement.SetValue(null);
                return nullElement;
            }
        }

        JsonValue GetMethodBody(BlockStatement body)
        {
            if (body.IsNull)
            {
                return null;
            }
            else
            {
                VisitBlockStatement(body);
                return jsonValueStack.Pop();
            }
        }

        JsonValue GetMethodTypeInfo(List<string> typeInfoList)
        {
            JsonArray typeArr = new JsonArray();
            typeArr.Comment = "GetMethodTypeInfo";
            foreach(string value in typeInfoList)
            {
                typeArr.AddJsonValue(new JsonElement(value));
            }
            if (typeArr.Count == 0)
            {
                typeArr = null;
            }
            return typeArr;
        }

        JsonValue GetIdentifier(Identifier identifier)
        {
            return new JsonElement(identifier.Name);
        }

        JsonValue GetCommaSeparatedList(IEnumerable<AstNode> list)
        {
            int count = list.Count();
            if (count > 0)
            {
                JsonArray nodeArr = new JsonArray();
                nodeArr.Comment = "GetCommaSeparatedList";
                foreach (AstNode node in list)
                {
                    node.AcceptVisitor(this);
                    if (count == 1)
                    {
                        return jsonValueStack.Pop();
                    }
                    else
                    {
                        nodeArr.AddJsonValue(jsonValueStack.Pop());
                    }
                }
                return nodeArr;
            }
            else//count == 0
            {
                return null;
            }
        }

        JsonValue GetEmbeddedStatement(Statement embeddedStatement)
        {
            JsonObject embedded = new JsonObject();
            embedded.Comment = "GetEmbeddedStatement";
            if (embeddedStatement.IsNull)
            {
                return null;
            }
            BlockStatement block = embeddedStatement as BlockStatement;
            if (block != null)
            {
                VisitBlockStatement(block);
                embedded.AddJsonValues("block-statement", jsonValueStack.Pop());
            }
            else
            {
                embeddedStatement.AcceptVisitor(this);
                embedded.AddJsonValues("statement", jsonValueStack.Pop());
            }
            return embedded;
        }

        #endregion

        #region StartNode/EndNode
        void StartNode(AstNode node)
        {
            //Debug.Assert(containerStack.Count == 0 || node.Parent == containerStack.Peek() || containerStack.Peek().NodeType == NodeType.Pattern);
            //containerStack.Push(node);
            //writer.StartNode(node);
        }

        void EndNode(AstNode node)
        {
            //Debug.Assert(node == containerStack.Peek());
            //containerStack.Pop();
            //writer.EndNode(node);
        }
        #endregion

        #region Expressions
        public void VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression)
        {
            throw new NotImplementedException();
        }

        public void VisitUndocumentedExpression(UndocumentedExpression undocumentedExpression)
        {
            throw new NotImplementedException();
        }

        public void VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitArrayCreateExpression";
            expression.AddJsonValues("expression-type", new JsonElement("array-create-expression"));
            expression.AddJsonValues("keyword", new JsonElement(ArrayCreateExpression.NewKeywordRole.Token));
            arrayCreateExpression.Type.AcceptVisitor(this);
            expression.AddJsonValues("array-type", jsonValueStack.Pop());
            if (arrayCreateExpression.Arguments.Count > 0)
            {
                expression.AddJsonValues("arguments", GetCommaSeparatedList(arrayCreateExpression.Arguments));
            }
            JsonArray specifierArr = new JsonArray();
            foreach(var specifier in arrayCreateExpression.AdditionalArraySpecifiers)
            {
                specifier.AcceptVisitor(this);
                var pop = jsonValueStack.Pop();
                if (pop != null)
                {
                    specifierArr.AddJsonValue(jsonValueStack.Pop());
                }
            }
            if (specifierArr.Count == 0)
            {
                specifierArr = null;
            }
            expression.AddJsonValues("specifier", specifierArr);
            arrayCreateExpression.Initializer.AcceptVisitor(this);
            expression.AddJsonValues("initializer", jsonValueStack.Pop());
            jsonValueStack.Push(expression);
        }

        public void VisitArrayInitializerExpression(ArrayInitializerExpression arrayInitializerExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitArrayInitializerExpression";
            expression.AddJsonValues("expression-type", new JsonElement("array-initializer-expression"));
            bool bracesAreOptional = arrayInitializerExpression.Elements.Count == 1
                && IsObjectOrCollectionInitializer(arrayInitializerExpression.Parent)
                && !CanBeConfusedWithObjectInitializer(arrayInitializerExpression.Elements.Single());
            if (bracesAreOptional && arrayInitializerExpression.LBraceToken.IsNull)
            {
                arrayInitializerExpression.Elements.Single().AcceptVisitor(this);
                expression.AddJsonValues("elements", jsonValueStack.Pop());
            }
            else
            {
                var json = GetInitializerElements(arrayInitializerExpression.Elements);
                expression.AddJsonValues("elements", json);
            }
            jsonValueStack.Push(expression);
        }

        bool CanBeConfusedWithObjectInitializer(Expression expr)
        {
            // "int a; new List<int> { a = 1 };" is an object initalizers and invalid, but
            // "int a; new List<int> { { a = 1 } };" is a valid collection initializer.
            AssignmentExpression ae = expr as AssignmentExpression;
            return ae != null && ae.Operator == AssignmentOperatorType.Assign;
        }

        bool IsObjectOrCollectionInitializer(AstNode node)
        {
            if (!(node is ArrayInitializerExpression))
            {
                return false;
            }
            if (node.Parent is ObjectCreateExpression)
            {
                return node.Role == ObjectCreateExpression.InitializerRole;
            }
            if (node.Parent is NamedExpression)
            {
                return node.Role == Roles.Expression;
            }
            return false;
        }

        JsonValue GetInitializerElements(AstNodeCollection<Expression> elements)
        {
            JsonArray initArr = new JsonArray();
            foreach (AstNode node in elements)
            {
                node.AcceptVisitor(this);
                initArr.AddJsonValue(jsonValueStack.Pop());
            }
            if (initArr.Count == 0)
            {
                initArr = null;
            }
            return initArr;
        }

        public void VisitAsExpression(AsExpression asExpression)
        {
            throw new NotImplementedException();
        }

        public void VisitAssignmentExpression(AssignmentExpression assignmentExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitAssignmentExpression";
            expression.AddJsonValues("expression-type", new JsonElement("assignment-expression"));
            assignmentExpression.Left.AcceptVisitor(this);
            expression.AddJsonValues("left-operand", jsonValueStack.Pop());
            TokenRole operatorRole = AssignmentExpression.GetOperatorRole(assignmentExpression.Operator);
            expression.AddJsonValues("operator", new JsonElement(operatorRole.Token));
            assignmentExpression.Right.AcceptVisitor(this);
            expression.AddJsonValues("right-operand", jsonValueStack.Pop());
            jsonValueStack.Push(expression);
        }

        public void VisitBaseReferenceExpression(BaseReferenceExpression baseReferenceExpression)
        {
            throw new NotImplementedException();
        }

        public void VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitBinaryOperatorExpression";
            expression.AddJsonValues("expression-type", new JsonElement("binary-operator-expression"));
            binaryOperatorExpression.Left.AcceptVisitor(this);
            expression.AddJsonValues("left-operand", jsonValueStack.Pop());
            string opt = BinaryOperatorExpression.GetOperatorRole(binaryOperatorExpression.Operator).Token;
            expression.AddJsonValues("operator", new JsonElement(opt));
            binaryOperatorExpression.Right.AcceptVisitor(this);
            expression.AddJsonValues("right-operand", jsonValueStack.Pop());
            jsonValueStack.Push(expression);
        }

        public void VisitCastExpression(CastExpression castExpression)
        {
            throw new NotImplementedException();
        }

        public void VisitCheckedExpression(CheckedExpression checkedExpression)
        {
            throw new NotImplementedException();
        }

        public void VisitConditionalExpression(ConditionalExpression conditionalExpression)
        {
            throw new NotImplementedException();
        }

        public void VisitDefaultValueExpression(DefaultValueExpression defaultValueExpression)
        {
            throw new NotImplementedException();
        }

        public void VisitDirectionExpression(DirectionExpression directionExpression)
        {
            throw new NotImplementedException();
        }

        public void VisitIdentifierExpression(IdentifierExpression identifierExpression)
        {
            jsonValueStack.Push(GetIdentifier(identifierExpression.IdentifierToken));
        }

        public void VisitIndexerExpression(IndexerExpression indexerExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitIndexerExpression";
            expression.AddJsonValues("expression-type", new JsonElement("indexer-expression"));
            indexerExpression.Target.AcceptVisitor(this);
            expression.AddJsonValues("target", jsonValueStack.Pop());
            expression.AddJsonValues("arguments", GetCommaSeparatedList(indexerExpression.Arguments));
            jsonValueStack.Push(expression);
        }

        public void VisitInvocationExpression(InvocationExpression invocationExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitInvocationExpression";
            expression.AddJsonValues("expression-type", new JsonElement("invocation"));
            invocationExpression.Target.AcceptVisitor(this);
            expression.AddJsonValues("target", jsonValueStack.Pop());
            expression.AddJsonValues("arguments", GetCommaSeparatedList(invocationExpression.Arguments));
            jsonValueStack.Push(expression);
        }

        public void VisitIsExpression(IsExpression isExpression)
        {
            throw new NotImplementedException();
        }

        public void VisitLambdaExpression(LambdaExpression lambdaExpression)
        {
            throw new NotImplementedException();
        }

        public void VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitMemberReferenceExpression";
            expression.AddJsonValues("expression-type", new JsonElement("member-reference"));
            expression.AddJsonValues("identifier-name", GetIdentifier(memberReferenceExpression.MemberNameToken));
            memberReferenceExpression.Target.AcceptVisitor(this);
            expression.AddJsonValues("type-info", jsonValueStack.Pop());
            jsonValueStack.Push(expression);
        }

        public void VisitNamedArgumentExpression(NamedArgumentExpression namedArgumentExpression)
        {
            throw new NotImplementedException();
        }

        public void VisitNamedExpression(NamedExpression namedExpression)
        {
            throw new NotImplementedException();
        }

        public void VisitNullReferenceExpression(NullReferenceExpression nullReferenceExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitNullReferenceExpression";
            expression.AddJsonValues("expression-type", new JsonElement("NullReference"));
            expression.AddJsonValues("keyword", new JsonElement("null"));
            jsonValueStack.Push(expression);
        }

        public void VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitObjectCreateExpression";
            expression.AddJsonValues("expression-type", new JsonElement("ObjectCreate"));
            expression.AddJsonValues("keyword", new JsonElement(ObjectCreateExpression.NewKeywordRole.Token));
            objectCreateExpression.Type.AcceptVisitor(this);
            expression.AddJsonValues("type-info", jsonValueStack.Pop());
            bool useParenthesis = objectCreateExpression.Arguments.Any() || objectCreateExpression.Initializer.IsNull;
            if (!objectCreateExpression.LParToken.IsNull)
            {
                useParenthesis = true;
            }
            if (useParenthesis)
            {
                expression.AddJsonValues("arguments", GetCommaSeparatedList(objectCreateExpression.Arguments));
            }
            objectCreateExpression.Initializer.AcceptVisitor(this);
            expression.AddJsonValues("initializer", jsonValueStack.Pop());
            jsonValueStack.Push(expression);
        }

        public void VisitAnonymousTypeCreateExpression(AnonymousTypeCreateExpression anonymousTypeCreateExpression)
        {
            throw new NotImplementedException();
        }

        public void VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression)
        {
            throw new NotImplementedException();
        }

        public void VisitPointerReferenceExpression(PointerReferenceExpression pointerReferenceExpression)
        {
            throw new NotImplementedException();
        }

        #region VisitPrimitiveExpression
        public void VisitPrimitiveExpression(PrimitiveExpression primitiveExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitPrimitiveExpression";
            expression.AddJsonValues("expression-type", new JsonElement("primitive-expression"));
            expression.AddJsonValues("value", new JsonElement(primitiveExpression.Value.ToString()));
            jsonValueStack.Push(expression);
        }
        #endregion

        public void VisitSizeOfExpression(SizeOfExpression sizeOfExpression)
        {
            throw new NotImplementedException();
        }

        public void VisitStackAllocExpression(StackAllocExpression stackAllocExpression)
        {
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
            typeReferenceExpression.Type.AcceptVisitor(this);
        }

        public void VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitUnaryOperatorExpression";
            expression.AddJsonValues("expression-type", new JsonElement("unary-operator-expression"));
            UnaryOperatorType opType = unaryOperatorExpression.Operator;
            var opSymbol = UnaryOperatorExpression.GetOperatorRole(opType);
            if (opType == UnaryOperatorType.Await)
            {
                expression.AddJsonValues("symbol", new JsonElement(opSymbol.Token));
            }
            else if (!(opType == UnaryOperatorType.PostIncrement || opType == UnaryOperatorType.PostDecrement))
            {
                expression.AddJsonValues("symbol", new JsonElement(opSymbol.Token));
            }
            unaryOperatorExpression.Expression.AcceptVisitor(this);
            expression.AddJsonValues("expression", jsonValueStack.Pop());
            if (opType == UnaryOperatorType.PostIncrement || opType == UnaryOperatorType.PostDecrement)
            {
                expression.AddJsonValues("symbol", new JsonElement(opSymbol.Token));
            }
            jsonValueStack.Push(expression);
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
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitBlockStatement";
            statement.AddJsonValues("statement-type", new JsonElement("block-statement"));
            int count = blockStatement.Statements.Count;
            if (count == 0)
            {
                jsonValueStack.Push(null);
                return;
            }
            JsonArray stmtList = new JsonArray();
            foreach(var node in blockStatement.Statements)
            {
                node.AcceptVisitor(this);
                stmtList.AddJsonValue(jsonValueStack.Pop());
            }
            statement.AddJsonValues("statement-list", stmtList);
            jsonValueStack.Push(statement);
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
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitDoWhileStatement";
            statement.AddJsonValues("statement-type", new JsonElement("do-while-statement"));
            doWhileStatement.Condition.AcceptVisitor(this);
            statement.AddJsonValues("condition", jsonValueStack.Pop());
            statement.AddJsonValues("statement-list", GetEmbeddedStatement(doWhileStatement.EmbeddedStatement));
            jsonValueStack.Push(statement);
        }

        public void VisitEmptyStatement(EmptyStatement emptyStatement)
        {
            throw new NotImplementedException();
        }

        public void VisitExpressionStatement(ExpressionStatement expressionStatement)
        {
            expressionStatement.Expression.AcceptVisitor(this);
        }

        public void VisitFixedStatement(FixedStatement fixedStatement)
        {
            throw new NotImplementedException();
        }

        public void VisitForeachStatement(ForeachStatement foreachStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitForeachStatement";
            statement.AddJsonValues("statement-type", new JsonElement("ForEach"));
            statement.AddJsonValues("keyword", new JsonElement(ForeachStatement.ForeachKeywordRole.Token));
            foreachStatement.VariableType.AcceptVisitor(this);
            statement.AddJsonValues("local-variable-type", jsonValueStack.Pop());
            statement.AddJsonValues("local-variable-name", GetIdentifier(foreachStatement.VariableNameToken));
            foreachStatement.InExpression.AcceptVisitor(this);
            statement.AddJsonValues("in-expression", jsonValueStack.Pop());
            statement.AddJsonValues("embedded-statement", GetEmbeddedStatement(foreachStatement.EmbeddedStatement));
            jsonValueStack.Push(statement);
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
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitIfElseStatement";
            ifElseStatement.Condition.AcceptVisitor(this);
            statement.AddJsonValues("condition", jsonValueStack.Pop());

            ifElseStatement.TrueStatement.AcceptVisitor(this);
            statement.AddJsonValues("true-statement", jsonValueStack.Pop());
            ifElseStatement.FalseStatement.AcceptVisitor(this);
            statement.AddJsonValues("false-statement", jsonValueStack.Pop());
            jsonValueStack.Push(statement);
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
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitVariableDeclarationStatement";
            statement.AddJsonValues("statement-type", new JsonElement("variable-declaration"));
            JsonValue modifier = GetModifiers(variableDeclarationStatement.GetChildrenByRole(VariableDeclarationStatement.ModifierRole));
            statement.AddJsonValues("modifier", modifier);
            variableDeclarationStatement.Type.AcceptVisitor(this);
            statement.AddJsonValues("declaration-type-info", jsonValueStack.Pop());
            statement.AddJsonValues("variables-list", GetCommaSeparatedList(variableDeclarationStatement.Variables));
            jsonValueStack.Push(statement);
        }

        public void VisitWhileStatement(WhileStatement whileStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitWhileStatement";
            statement.AddJsonValues("statement-type", new JsonElement("while-statement"));
            whileStatement.Condition.AcceptVisitor(this);
            statement.AddJsonValues("condition", jsonValueStack.Pop());
            statement.AddJsonValues("statement-list", GetEmbeddedStatement(whileStatement.EmbeddedStatement));
            jsonValueStack.Push(statement);
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

        int constructorNum = 0;
        public void VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration)
        {
            if (constructorNum > 0)
            {
                typeInfoIndex.Clear();
            }
            JsonObject construct = new JsonObject();
            construct.Comment = "VisitConstructorDeclaration";
            TypeDeclaration type = constructorDeclaration.Parent as TypeDeclaration;
            if (type != null && type.Name != constructorDeclaration.Name)
            {
                construct.AddJsonValues("type", GetIdentifier((Identifier)type.NameToken.Clone()));
            }
            else
            {
                construct.AddJsonValues("name", new JsonElement(constructorDeclaration.Name));
            }
            construct.AddJsonValues("modifier", GetModifiers(constructorDeclaration.ModifierTokens));
            construct.AddJsonValues("parameters", GetCommaSeparatedList(constructorDeclaration.Parameters));
            if (!constructorDeclaration.Initializer.IsNull)
            {
                constructorDeclaration.Initializer.AcceptVisitor(this);
                construct.AddJsonValues("initializer", jsonValueStack.Pop());
            }
            construct.AddJsonValues("body", GetMethodBody(constructorDeclaration.Body));
            jsonValueStack.Push(construct);
            constructorNum++;
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
        JsonArray methodList;
        Dictionary<string, int> typeInfoIndex = new Dictionary<string, int>();
        public void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
        {
            JsonObject method = new JsonObject();
            method.Comment = "VisitMethodDeclaration";
            if (methodNum == 0)
            {
                methodList = new JsonArray();
            }
            else
            {
                typeInfoIndex.Clear();
            }
            method.AddJsonValues("name", new JsonElement(methodDeclaration.Name));
            //write modifier
            method.AddJsonValues("modifier", GetModifiers(methodDeclaration.ModifierTokens));
            //write return type
            methodDeclaration.ReturnType.AcceptVisitor(this);
            method.AddJsonValues("return-type", jsonValueStack.Pop());
            //write parameters
            method.AddJsonValues("parameters", GetCommaSeparatedList(methodDeclaration.Parameters));
            //write body
            method.AddJsonValues("body", GetMethodBody(methodDeclaration.Body));
            //write method type info
            method.AddJsonValues("type-info-list", GetMethodTypeInfo(new List<string>(typeInfoIndex.Keys)));
            methodList.AddJsonValue(method);

            jsonValueStack.Push(method);
            methodNum++;
        }

        public void VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration)
        {
            throw new NotImplementedException();
        }

        public void VisitParameterDeclaration(ParameterDeclaration parameterDeclaration)
        {
            JsonObject parameter = new JsonObject();
            parameter.Comment = "VisitParameterDeclaration";
            int index;
            string type = parameterDeclaration.Type.ToString();
            if (!typeInfoIndex.TryGetValue(type, out index))
            {
                index = typeInfoIndex.Count;
                typeInfoIndex.Add(type, index);
            }
            parameter.AddJsonValues("type-info", new JsonElement(index));
            parameter.AddJsonValues("name", new JsonElement(parameterDeclaration.Name));
            jsonValueStack.Push(parameter);
        }

        public void VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Other Node

        public void VisitVariableInitializer(VariableInitializer variableInitializer)
        {
            JsonObject variable = new JsonObject();
            variable.Comment = "VisitVariableInitializer";
            variable.AddJsonValues("variable-name", GetIdentifier(variableInitializer.NameToken));
            if (!variableInitializer.Initializer.IsNull)
            {
                variableInitializer.Initializer.AcceptVisitor(this);
                variable.AddJsonValues("initializer", jsonValueStack.Pop());
            }
            else
            {
                variable.AddJsonValues("initializer", null);
            }
            jsonValueStack.Push(variable);
        }

        public void VisitSimpleType(SimpleType simpleType)
        {
            jsonValueStack.Push(GetTypeInfo(simpleType.IdentifierToken.Name));
        }

        JsonElement GetTypeInfo(string typeKeyword)
        {
            int index = 0;
            if (!typeInfoIndex.TryGetValue(typeKeyword, out index))
            {
                index = typeInfoIndex.Count;
                typeInfoIndex.Add(typeKeyword, index);
            }
            return new JsonElement(index);
        }

        public void VisitSyntaxTree(SyntaxTree syntaxTree)
        {
            JsonArray arr = new JsonArray();
            arr.Comment = "VisitSyntaxTree";
            int counter = 0;
            foreach (AstNode node in syntaxTree.Children)
            {
                node.AcceptVisitor(this);
                arr.AddJsonValue(jsonValueStack.Pop());
                counter++;
            }
            if (counter == 1)
            {
                LastValue = arr.ValueList[0];
            }
            else
            {
                LastValue = arr;
            }
        }

        public void VisitMemberType(MemberType memberType)
        {
            JsonObject memtype = new JsonObject();
            memtype.Comment = "VisitMemberType";
            memberType.Target.AcceptVisitor(this);
            memtype.AddJsonValues("type-info", jsonValueStack.Pop());
            memtype.AddJsonValues("member-name", GetIdentifier(memberType.MemberNameToken));
            jsonValueStack.Push(memtype);
        }

        public void VisitComposedType(ComposedType composedType)
        {
            composedType.BaseType.AcceptVisitor(this);
        }

        public void VisitArraySpecifier(ArraySpecifier arraySpecifier)
        {
            JsonArray arrSpec = new JsonArray();
            arrSpec.Comment = "VisitArraySpecifier";
            foreach (var spec in arraySpecifier.GetChildrenByRole(Roles.Comma))
            {
                spec.AcceptVisitor(this);
                arrSpec.AddJsonValue(jsonValueStack.Pop());
            }
            if (arrSpec.Count == 0)
            {
                arrSpec = null;
            }
            jsonValueStack.Push(arrSpec);
        }

        public void VisitPrimitiveType(PrimitiveType primitiveType)
        {
            jsonValueStack.Push(GetTypeInfo(primitiveType.Keyword));
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
            CSharpModifierToken mod = cSharpTokenNode as CSharpModifierToken;
            if (mod != null)
            {
                JsonElement element = new JsonElement();
                element.SetValue(CSharpModifierToken.GetModifierName(mod.Modifier));
                jsonValueStack.Push(element);
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
            jsonValueStack.Push(null);
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