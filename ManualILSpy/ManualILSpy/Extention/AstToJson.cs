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
        
        public AstCSharpToJsonVisitor(ITextOutput output)
        {
            methodNum = 0;
            constructorNum = 0;
        }

        #region Stack
        private void Push(JsonValue value)
        {
            jsonValueStack.Push(value);
        }

        private JsonValue Pop()
        {
            return jsonValueStack.Pop();
        }
        #endregion

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
                    modifierArr.AddJsonValue(Pop());
                }
                return modifierArr;
            }
            else if (count == 1)
            {
                modifierList[0].AcceptVisitor(this);
                return Pop();
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
                return Pop();
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
                        return Pop();
                    }
                    else
                    {
                        nodeArr.AddJsonValue(Pop());
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
                embedded.AddJsonValues("block-statement", Pop());
            }
            else
            {
                embeddedStatement.AcceptVisitor(this);
                embedded.AddJsonValues("statement", Pop());
            }
            return embedded;
        }

        JsonValue GetAttributes(IEnumerable<AttributeSection> attributes)
        {
            JsonArray attrArray = new JsonArray();
            foreach(AttributeSection attr in attributes)
            {
                attr.AcceptVisitor(this);
                attrArray.AddJsonValue(Pop());
            }
            if (attrArray.Count == 0)
            {
                attrArray = null;
            }
            return attrArray;
        }

        JsonValue GetTypeArguments(IEnumerable<AstType> typeAgruments)
        {
            if (typeAgruments.Any())
            {
                return GetCommaSeparatedList(typeAgruments);
            }
            return null;
        }

        JsonValue GetTypeParameters(IEnumerable<TypeParameterDeclaration> typeParameters)
        {
            if (typeParameters.Any())
            {
                return GetCommaSeparatedList(typeParameters);
            }
            return null;
        }

        JsonValue GetPrivateImplementationType(AstType privateImplementationType)
        {
            if (!privateImplementationType.IsNull)
            {
                privateImplementationType.AcceptVisitor(this);
                return Pop();
            }
            return null;
        }

        #endregion

        #region Expressions
        public void VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitAnonymousMethodExpression";
            expression.AddJsonValues("expression-type", new JsonElement("anonymous-method-expression"));
            expression.AddJsonValues("keyword", new JsonElement(AnonymousMethodExpression.AsyncModifierRole.Token));
            expression.AddJsonValues("delegate-keyword", new JsonElement(AnonymousMethodExpression.DelegateKeywordRole.Token));
            if (anonymousMethodExpression.HasParameterList)
            {
                expression.AddJsonValues("arguments", GetCommaSeparatedList(anonymousMethodExpression.Parameters));
            }
            anonymousMethodExpression.Body.AcceptVisitor(this);
            expression.AddJsonValues("body", Pop());
            Push(expression);
        }

        public void VisitUndocumentedExpression(UndocumentedExpression undocumentedExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitUndocumentedExpression";
            expression.AddJsonValues("expression-type", new JsonElement("undocumented-expression"));
            switch (undocumentedExpression.UndocumentedExpressionType)
            {
                case UndocumentedExpressionType.ArgList:
                case UndocumentedExpressionType.ArgListAccess:
                    expression.AddJsonValues("type-info", new JsonElement(UndocumentedExpression.ArglistKeywordRole.Token));
                    break;
                case UndocumentedExpressionType.MakeRef:
                    expression.AddJsonValues("type-info", new JsonElement(UndocumentedExpression.MakerefKeywordRole.Token));
                    break;
                case UndocumentedExpressionType.RefType:
                    expression.AddJsonValues("type-info", new JsonElement(UndocumentedExpression.ReftypeKeywordRole.Token));
                    break;
                case UndocumentedExpressionType.RefValue:
                    expression.AddJsonValues("type-info", new JsonElement(UndocumentedExpression.RefvalueKeywordRole.Token));
                    break;
                default:
                    throw new Exception("unknowed type");
            }
            if (undocumentedExpression.Arguments.Count > 0)
            {
                expression.AddJsonValues("arguments", GetCommaSeparatedList(undocumentedExpression.Arguments));
            }
            else
            {
                expression.AddJsonValues("arguments", null);
            }
            Push(expression);
            throw new Exception("first time testing");//implement already, but not tested
        }

        public void VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitArrayCreateExpression";
            expression.AddJsonValues("expression-type", new JsonElement("array-create-expression"));
            expression.AddJsonValues("keyword", new JsonElement(ArrayCreateExpression.NewKeywordRole.Token));
            arrayCreateExpression.Type.AcceptVisitor(this);
            expression.AddJsonValues("array-type", Pop());
            if (arrayCreateExpression.Arguments.Count > 0)
            {
                expression.AddJsonValues("arguments", GetCommaSeparatedList(arrayCreateExpression.Arguments));
            }
            JsonArray specifierArr = new JsonArray();
            foreach(var specifier in arrayCreateExpression.AdditionalArraySpecifiers)
            {
                specifier.AcceptVisitor(this);
                var pop = Pop();
                if (pop != null)
                {
                    specifierArr.AddJsonValue(Pop());
                }
            }
            if (specifierArr.Count == 0)
            {
                specifierArr = null;
            }
            expression.AddJsonValues("specifier", specifierArr);
            arrayCreateExpression.Initializer.AcceptVisitor(this);
            expression.AddJsonValues("initializer", Pop());
            Push(expression);
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
                expression.AddJsonValues("elements", Pop());
            }
            else
            {
                var json = GetInitializerElements(arrayInitializerExpression.Elements);
                expression.AddJsonValues("elements", json);
            }
            Push(expression);
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
                initArr.AddJsonValue(Pop());
            }
            if (initArr.Count == 0)
            {
                initArr = null;
            }
            return initArr;
        }

        public void VisitAsExpression(AsExpression asExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitAsExpression";
            expression.AddJsonValues("expression-type", new JsonElement("as-expression"));
            expression.AddJsonValues("keyword", new JsonElement(AsExpression.AsKeywordRole.Token));
            asExpression.Type.AcceptVisitor(this);
            expression.AddJsonValues("type-info", Pop());

            Push(expression);
            throw new Exception("first time testing");//implement already, but not tested
        }

        public void VisitAssignmentExpression(AssignmentExpression assignmentExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitAssignmentExpression";
            expression.AddJsonValues("expression-type", new JsonElement("assignment-expression"));
            assignmentExpression.Left.AcceptVisitor(this);
            expression.AddJsonValues("left-operand", Pop());
            TokenRole operatorRole = AssignmentExpression.GetOperatorRole(assignmentExpression.Operator);
            expression.AddJsonValues("operator", new JsonElement(operatorRole.Token));
            assignmentExpression.Right.AcceptVisitor(this);
            expression.AddJsonValues("right-operand", Pop());
            Push(expression);
        }

        public void VisitBaseReferenceExpression(BaseReferenceExpression baseReferenceExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitBaseReferenceExpression";
            expression.AddJsonValues("expression-type", new JsonElement("base-reference-expression"));
            expression.AddJsonValues("keyword", new JsonElement("base"));

            Push(expression);
            throw new Exception("first time testing");//implement already, but not tested
        }

        public void VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitBinaryOperatorExpression";
            expression.AddJsonValues("expression-type", new JsonElement("binary-operator-expression"));
            binaryOperatorExpression.Left.AcceptVisitor(this);
            expression.AddJsonValues("left-operand", Pop());
            string opt = BinaryOperatorExpression.GetOperatorRole(binaryOperatorExpression.Operator).Token;
            expression.AddJsonValues("operator", new JsonElement(opt));
            binaryOperatorExpression.Right.AcceptVisitor(this);
            expression.AddJsonValues("right-operand", Pop());
            Push(expression);
        }

        public void VisitCastExpression(CastExpression castExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitCastExpression";
            expression.AddJsonValues("expression-type", new JsonElement("cast-expression"));
            castExpression.Type.AcceptVisitor(this);
            expression.AddJsonValues("type-info", Pop());
            castExpression.Expression.AcceptVisitor(this);
            expression.AddJsonValues("expression", Pop());

            Push(expression);
        }

        public void VisitCheckedExpression(CheckedExpression checkedExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitCheckedExpression";
            expression.AddJsonValues("expression-type", new JsonElement("checked-expression"));
            expression.AddJsonValues("keyword", new JsonElement(CheckedExpression.CheckedKeywordRole.Token));
            checkedExpression.Expression.AcceptVisitor(this);
            expression.AddJsonValues("expression", Pop());

            Push(expression);
            throw new Exception("first time testing");//implement already, but not tested
        }

        public void VisitConditionalExpression(ConditionalExpression conditionalExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitConditionalExpression";
            expression.AddJsonValues("expression-type", new JsonElement("conditional-expression"));
            conditionalExpression.Condition.AcceptVisitor(this);
            expression.AddJsonValues("condition", Pop());
            conditionalExpression.TrueExpression.AcceptVisitor(this);
            expression.AddJsonValues("true-expression", Pop());
            conditionalExpression.FalseExpression.AcceptVisitor(this);
            expression.AddJsonValues("false-expression", Pop());

            Push(expression);
            throw new Exception("first time testing");//implement already, but not tested
        }

        public void VisitDefaultValueExpression(DefaultValueExpression defaultValueExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitDefaultValueExpression";
            expression.AddJsonValues("expression-type", new JsonElement("default-value-expression"));
            expression.AddJsonValues("keyword", new JsonElement(DefaultValueExpression.DefaultKeywordRole.Token));
            defaultValueExpression.Type.AcceptVisitor(this);
            expression.AddJsonValues("type-info", Pop());

            Push(expression);
            throw new Exception("first time testing");//implement already, but not tested
        }

        public void VisitDirectionExpression(DirectionExpression directionExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitDirectionExpression";
            expression.AddJsonValues("expression-type", new JsonElement("direction-expression"));
            switch (directionExpression.FieldDirection)
            {
                case FieldDirection.Out:
                    expression.AddJsonValues("keyword", new JsonElement(DirectionExpression.OutKeywordRole.Token));
                    break;
                case FieldDirection.Ref:
                    expression.AddJsonValues("keyword", new JsonElement(DirectionExpression.RefKeywordRole.Token));
                    break;
                default:
                    throw new NotSupportedException("Invalid value for FieldDirection");
            }
            directionExpression.Expression.AcceptVisitor(this);
            expression.AddJsonValues("expression", Pop());

            Push(expression);
            throw new Exception("first time testing");//implement already, but not tested
        }

        public void VisitIdentifierExpression(IdentifierExpression identifierExpression)
        {
            Push(GetIdentifier(identifierExpression.IdentifierToken));
        }

        public void VisitIndexerExpression(IndexerExpression indexerExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitIndexerExpression";
            expression.AddJsonValues("expression-type", new JsonElement("indexer-expression"));
            indexerExpression.Target.AcceptVisitor(this);
            expression.AddJsonValues("target", Pop());
            expression.AddJsonValues("arguments", GetCommaSeparatedList(indexerExpression.Arguments));
            Push(expression);
        }

        public void VisitInvocationExpression(InvocationExpression invocationExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitInvocationExpression";
            expression.AddJsonValues("expression-type", new JsonElement("invocation"));
            invocationExpression.Target.AcceptVisitor(this);
            expression.AddJsonValues("target", Pop());
            expression.AddJsonValues("arguments", GetCommaSeparatedList(invocationExpression.Arguments));
            Push(expression);
        }

        public void VisitIsExpression(IsExpression isExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitIsExpression";
            expression.AddJsonValues("expression-type", new JsonElement("is-expression"));
            expression.AddJsonValues("keyword", new JsonElement(IsExpression.IsKeywordRole.Token));
            isExpression.Type.AcceptVisitor(this);
            expression.AddJsonValues("type-info", Pop());
            isExpression.Expression.AcceptVisitor(this);
            expression.AddJsonValues("expression", Pop());

            Push(expression);
            throw new Exception("first time testing");//implement already, but not tested
        }

        public void VisitLambdaExpression(LambdaExpression lambdaExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitLambdaExpression";
            expression.AddJsonValues("expression-type", new JsonElement("lambda-expression"));
            if (lambdaExpression.IsAsync)
            {
                expression.AddJsonValues("async-keyword", new JsonElement(LambdaExpression.AsyncModifierRole.Token));
            }
            if (LambdaNeedsParenthesis(lambdaExpression))
            {
                expression.AddJsonValues("parameters", GetCommaSeparatedList(lambdaExpression.Parameters));
            }
            else
            {
                lambdaExpression.Parameters.Single().AcceptVisitor(this);
                expression.AddJsonValues("parameters", Pop());
            }
            lambdaExpression.Body.AcceptVisitor(this);
            expression.AddJsonValues("body", Pop());

            Push(expression);
            throw new Exception("first time testing");//implement already, but not tested
        }

        bool LambdaNeedsParenthesis(LambdaExpression lambdaExpression)
        {
            if (lambdaExpression.Parameters.Count != 1)
            {
                return true;
            }
            var p = lambdaExpression.Parameters.Single();
            return !(p.Type.IsNull && p.ParameterModifier == ParameterModifier.None);
        }

        public void VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitMemberReferenceExpression";
            expression.AddJsonValues("expression-type", new JsonElement("member-reference"));
            expression.AddJsonValues("identifier-name", GetIdentifier(memberReferenceExpression.MemberNameToken));
            memberReferenceExpression.Target.AcceptVisitor(this);
            expression.AddJsonValues("type-info", Pop());
            Push(expression);
        }

        public void VisitNamedArgumentExpression(NamedArgumentExpression namedArgumentExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitNamedArgumentExpression";
            expression.AddJsonValues("expression-type", new JsonElement("named-argument-expression"));
            expression.AddJsonValues("identifier", GetIdentifier(namedArgumentExpression.NameToken));
            namedArgumentExpression.Expression.AcceptVisitor(this);
            expression.AddJsonValues("expression", Pop());

            Push(expression);
            throw new Exception("first time testing");//implement already, but not tested
        }

        public void VisitNamedExpression(NamedExpression namedExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitNamedExpression";
            expression.AddJsonValues("expression-type", new JsonElement("named-expression"));
            expression.AddJsonValues("identifier", GetIdentifier(namedExpression.NameToken));
            namedExpression.Expression.AcceptVisitor(this);
            expression.AddJsonValues("expression", Pop());

            Push(expression);
            throw new Exception("first time testing");//implement already, but not tested
        }

        public void VisitNullReferenceExpression(NullReferenceExpression nullReferenceExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitNullReferenceExpression";
            expression.AddJsonValues("expression-type", new JsonElement("null-reference"));
            expression.AddJsonValues("keyword", new JsonElement("null"));
            Push(expression);
        }

        public void VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitObjectCreateExpression";
            expression.AddJsonValues("expression-type", new JsonElement("ObjectCreate"));
            expression.AddJsonValues("keyword", new JsonElement(ObjectCreateExpression.NewKeywordRole.Token));
            objectCreateExpression.Type.AcceptVisitor(this);
            expression.AddJsonValues("type-info", Pop());
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
            expression.AddJsonValues("initializer", Pop());
            Push(expression);
        }

        public void VisitAnonymousTypeCreateExpression(AnonymousTypeCreateExpression anonymousTypeCreateExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitAnonymousTypeCreateExpression";
            expression.AddJsonValues("expression-type", new JsonElement("anonymous-type-create-expression"));
            expression.AddJsonValues("keyword", new JsonElement(AnonymousTypeCreateExpression.NewKeywordRole.Token));
            expression.AddJsonValues("elements", GetInitializerElements(anonymousTypeCreateExpression.Initializers));
            Push(expression);
            throw new Exception("first time testing");//implement already, but not tested
        }

        public void VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitParenthesizedExpression";
            expression.AddJsonValues("expression-type", new JsonElement("parenthesized-expression"));
            parenthesizedExpression.Expression.AcceptVisitor(this);
            expression.AddJsonValues("expression", Pop());

            Push(expression);
            throw new Exception("first time testing");//implement already, but not tested
        }

        public void VisitPointerReferenceExpression(PointerReferenceExpression pointerReferenceExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitPointerReferenceExpression";
            expression.AddJsonValues("expression-type", new JsonElement("pointer-reference-expression"));
            pointerReferenceExpression.Target.AcceptVisitor(this);
            expression.AddJsonValues("target", Pop());
            expression.AddJsonValues("identifier", GetIdentifier(pointerReferenceExpression.MemberNameToken));
            expression.AddJsonValues("type-arguments", GetTypeArguments(pointerReferenceExpression.TypeArguments));

            Push(expression);

            throw new Exception("first time testing");//implement already, but not tested
        }

        #region VisitPrimitiveExpression
        public void VisitPrimitiveExpression(PrimitiveExpression primitiveExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitPrimitiveExpression";
            expression.AddJsonValues("expression-type", new JsonElement("primitive-expression"));
            expression.AddJsonValues("value", new JsonElement(primitiveExpression.Value.ToString()));
            Push(expression);
        }
        #endregion

        public void VisitSizeOfExpression(SizeOfExpression sizeOfExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitSizeOfExpression";
            expression.AddJsonValues("expression-type", new JsonElement("sizeof-expression"));
            expression.AddJsonValues("keyword", new JsonElement(SizeOfExpression.SizeofKeywordRole.Token));
            sizeOfExpression.Type.AcceptVisitor(this);
            expression.AddJsonValues("type-info", Pop());

            Push(expression);
            throw new Exception("first time testing");//implement already, but not tested
        }

        public void VisitStackAllocExpression(StackAllocExpression stackAllocExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitStackAllocExpression";
            expression.AddJsonValues("expression-type", new JsonElement("stack-alloc-expression"));
            stackAllocExpression.Type.AcceptVisitor(this);
            expression.AddJsonValues("type-info", Pop());
            expression.AddJsonValues("count-expression", GetCommaSeparatedList(new[] { stackAllocExpression.CountExpression }));

            Push(expression);
            throw new Exception("first time testing");//implement already, but not tested
        }

        public void VisitThisReferenceExpression(ThisReferenceExpression thisReferenceExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitThisReferenceExpression";
            expression.AddJsonValues("expression-type", new JsonElement("this-reference-expression"));
            expression.AddJsonValues("keyword", new JsonElement("this"));
            Push(expression);
        }

        public void VisitTypeOfExpression(TypeOfExpression typeOfExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitTypeOfExpression";
            expression.AddJsonValues("expression-type", new JsonElement("typeof-expression"));
            expression.AddJsonValues("keyword", new JsonElement(TypeOfExpression.TypeofKeywordRole.Token));
            typeOfExpression.Type.AcceptVisitor(this);
            expression.AddJsonValues("type-info", Pop());

            Push(expression);
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
            expression.AddJsonValues("expression", Pop());
            if (opType == UnaryOperatorType.PostIncrement || opType == UnaryOperatorType.PostDecrement)
            {
                expression.AddJsonValues("symbol", new JsonElement(opSymbol.Token));
            }
            Push(expression);
        }

        public void VisitUncheckedExpression(UncheckedExpression uncheckedExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitUncheckedExpression";
            expression.AddJsonValues("expression-type", new JsonElement("unchecked-expression"));
            expression.AddJsonValues("keyword", new JsonElement(UncheckedExpression.UncheckedKeywordRole.Token));
            uncheckedExpression.Expression.AcceptVisitor(this);
            expression.AddJsonValues("expression", Pop());

            Push(expression);
            throw new Exception("first time testing");//implement already, but not tested
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
            JsonObject visit = new JsonObject();
            visit.Comment = "VisitAttribute";
            attribute.Type.AcceptVisitor(this);
            visit.AddJsonValues("type", Pop());
            if (attribute.Arguments.Count != 0 || !attribute.GetChildByRole(Roles.LPar).IsNull)
            {
                visit.AddJsonValues("arguments", GetCommaSeparatedList(attribute.Arguments));
            }

            Push(visit);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitAttributeSection(AttributeSection attributeSection)
        {
            JsonObject visit = new JsonObject();
            visit.Comment = "VisitAttributeSection";
            if (!string.IsNullOrEmpty(attributeSection.AttributeTarget))
            {
                visit.AddJsonValues("keyword", new JsonElement(attributeSection.AttributeTarget));
            }
            visit.AddJsonValues("attributes", GetCommaSeparatedList(attributeSection.Attributes));

            Push(visit);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration)
        {
            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitDelegateDeclaration";
            declaration.AddJsonValues("attributes", GetAttributes(delegateDeclaration.Attributes));
            declaration.AddJsonValues("modifier", GetModifiers(delegateDeclaration.ModifierTokens));
            declaration.AddJsonValues("keyword", new JsonElement(Roles.DelegateKeyword.Token));
            delegateDeclaration.ReturnType.AcceptVisitor(this);
            declaration.AddJsonValues("identifier", GetIdentifier(delegateDeclaration.NameToken));
            declaration.AddJsonValues("type-parameters", GetTypeParameters(delegateDeclaration.TypeParameters));
            declaration.AddJsonValues("parameters", GetCommaSeparatedList(delegateDeclaration.Parameters));
            JsonArray contraintList = new JsonArray();
            foreach(Constraint constraint in delegateDeclaration.Constraints)
            {
                constraint.AcceptVisitor(this);
                var temp = Pop();
                if (temp != null)
                {
                    contraintList.AddJsonValue(temp);
                }
            }
            if (contraintList.Count == 0)
            {
                contraintList = null;
            }
            declaration.AddJsonValues("constraint", contraintList);

            Push(declaration);
            //implement already, but not tested
            throw new Exception("first time testing");
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
                Push(null);
                return;
            }
            JsonArray stmtList = new JsonArray();
            foreach(var node in blockStatement.Statements)
            {
                node.AcceptVisitor(this);
                stmtList.AddJsonValue(Pop());
            }
            statement.AddJsonValues("statement-list", stmtList);
            Push(statement);
        }

        public void VisitBreakStatement(BreakStatement breakStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitBreakStatement";
            statement.AddJsonValues("statement-type", new JsonElement("break-statement"));
            statement.AddJsonValues("keyword", new JsonElement("break"));

            Push(statement);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitCheckedStatement(CheckedStatement checkedStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitCheckedStatement";
            statement.AddJsonValues("statement-type", new JsonElement("checked-statement"));
            statement.AddJsonValues("keyword", new JsonElement(CheckedStatement.CheckedKeywordRole.Token));
            checkedStatement.Body.AcceptVisitor(this);
            statement.AddJsonValues("body", Pop());

            Push(statement);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitContinueStatement(ContinueStatement continueStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitContinueStatement";
            statement.AddJsonValues("statement-type", new JsonElement("continue-statement"));
            statement.AddJsonValues("keyword", new JsonElement(ContinueStatement.ContinueKeywordRole.Token));

            Push(statement);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitDoWhileStatement(DoWhileStatement doWhileStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitDoWhileStatement";
            statement.AddJsonValues("statement-type", new JsonElement("do-while-statement"));
            doWhileStatement.Condition.AcceptVisitor(this);
            statement.AddJsonValues("condition", Pop());
            statement.AddJsonValues("statement-list", GetEmbeddedStatement(doWhileStatement.EmbeddedStatement));
            Push(statement);
        }

        public void VisitEmptyStatement(EmptyStatement emptyStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitEmptyStatement";
            statement.AddJsonValues("statement-type", new JsonElement("empty-statement"));
            Push(statement);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitExpressionStatement(ExpressionStatement expressionStatement)
        {
            expressionStatement.Expression.AcceptVisitor(this);
        }

        public void VisitFixedStatement(FixedStatement fixedStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitFixedStatement";
            statement.AddJsonValues("statement-type", new JsonElement("fixed-statement"));
            statement.AddJsonValues("keyword", new JsonElement(FixedStatement.FixedKeywordRole.Token));
            fixedStatement.Type.AcceptVisitor(this);
            statement.AddJsonValues("type-info", Pop());
            statement.AddJsonValues("variables", GetCommaSeparatedList(fixedStatement.Variables));
            statement.AddJsonValues("embedded-statement", GetEmbeddedStatement(fixedStatement.EmbeddedStatement));

            Push(statement);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitForeachStatement(ForeachStatement foreachStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitForeachStatement";
            statement.AddJsonValues("statement-type", new JsonElement("ForEach"));
            statement.AddJsonValues("keyword", new JsonElement(ForeachStatement.ForeachKeywordRole.Token));
            foreachStatement.VariableType.AcceptVisitor(this);
            statement.AddJsonValues("local-variable-type", Pop());
            statement.AddJsonValues("local-variable-name", GetIdentifier(foreachStatement.VariableNameToken));
            foreachStatement.InExpression.AcceptVisitor(this);
            statement.AddJsonValues("in-expression", Pop());
            statement.AddJsonValues("embedded-statement", GetEmbeddedStatement(foreachStatement.EmbeddedStatement));
            Push(statement);
        }

        public void VisitForStatement(ForStatement forStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitForStatement";
            statement.AddJsonValues("keyword", new JsonElement(ForStatement.ForKeywordRole.Token));
            statement.AddJsonValues("initializer", GetCommaSeparatedList(forStatement.Initializers));
            forStatement.Condition.AcceptVisitor(this);
            statement.AddJsonValues("condition", Pop());
            if (forStatement.Iterators.Any())
            {
                statement.AddJsonValues("iterators", GetCommaSeparatedList(forStatement.Iterators));
            }
            statement.AddJsonValues("embedded-statement", GetEmbeddedStatement(forStatement.EmbeddedStatement));
            Push(statement);
        }

        public void VisitGotoCaseStatement(GotoCaseStatement gotoCaseStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitGotoCaseStatement";
            statement.AddJsonValues("statement-type", new JsonElement("goto-case-statement"));
            statement.AddJsonValues("goto-keyword", new JsonElement(GotoCaseStatement.GotoKeywordRole.Token));
            statement.AddJsonValues("case-keyword", new JsonElement(GotoCaseStatement.CaseKeywordRole.Token));
            gotoCaseStatement.LabelExpression.AcceptVisitor(this);
            statement.AddJsonValues("label-expression", Pop());
            Push(statement);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitGotoDefaultStatement(GotoDefaultStatement gotoDefaultStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitGotoDefaultStatement";
            statement.AddJsonValues("statement-type", new JsonElement("goto-default-statement"));
            statement.AddJsonValues("goto-keyword", new JsonElement(GotoDefaultStatement.GotoKeywordRole.Token));
            statement.AddJsonValues("default-keyword", new JsonElement(GotoDefaultStatement.DefaultKeywordRole.Token));
            Push(statement);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitGotoStatement(GotoStatement gotoStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitGotoStatement";
            statement.AddJsonValues("statement-type", new JsonElement("goto-statement"));
            statement.AddJsonValues("keyword", new JsonElement(GotoStatement.GotoKeywordRole.Token));
            statement.AddJsonValues("identifier", GetIdentifier(gotoStatement.GetChildByRole(Roles.Identifier)));

            Push(statement);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitIfElseStatement(IfElseStatement ifElseStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitIfElseStatement";
            ifElseStatement.Condition.AcceptVisitor(this);
            statement.AddJsonValues("condition", Pop());

            ifElseStatement.TrueStatement.AcceptVisitor(this);
            statement.AddJsonValues("true-statement", Pop());
            ifElseStatement.FalseStatement.AcceptVisitor(this);
            statement.AddJsonValues("false-statement", Pop());
            Push(statement);
        }

        public void VisitLabelStatement(LabelStatement labelStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitLabelStatement";
            statement.AddJsonValues("statement-type", new JsonElement("label-statement"));
            statement.AddJsonValues("identifier", GetIdentifier(labelStatement.GetChildByRole(Roles.Identifier)));

            Push(statement);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitLockStatement(LockStatement lockStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitLockStatement";
            statement.AddJsonValues("statement-type", new JsonElement("lock-statement"));
            statement.AddJsonValues("keyword", new JsonElement(LockStatement.LockKeywordRole.Token));
            lockStatement.Expression.AcceptVisitor(this);
            statement.AddJsonValues("expression", Pop());
            statement.AddJsonValues("embedded-statement", GetEmbeddedStatement(lockStatement.EmbeddedStatement));

            Push(statement);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitReturnStatement(ReturnStatement returnStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitReturnStatement";
            statement.AddJsonValues("statement-type", new JsonElement("return-statement"));
            statement.AddJsonValues("keyword", new JsonElement(ReturnStatement.ReturnKeywordRole.Token));
            if (!returnStatement.Expression.IsNull)
            {
                returnStatement.Expression.AcceptVisitor(this);
                statement.AddJsonValues("expression", Pop());
            }
            Push(statement);
        }

        public void VisitSwitchSection(SwitchSection switchSection)
        {
            JsonObject section = new JsonObject();
            section.Comment = "VisitSwitchSection";
            
            JsonArray label = new JsonArray();
            foreach(var lb in switchSection.CaseLabels)
            {
                lb.AcceptVisitor(this);
                label.AddJsonValue(Pop());
            }
            if (label.Count == 0)
            {
                label = null;
            }
            section.AddJsonValues("label", label);
            JsonArray statement = new JsonArray();
            foreach(var stmt in switchSection.Statements)
            {
                stmt.AcceptVisitor(this);
                statement.AddJsonValue(Pop());
            }
            if (statement.Count == 0)
            {
                statement = null;
            }
            section.AddJsonValues("statements", statement);

            Push(section);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitSwitchStatement(SwitchStatement switchStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitSwitchStatement";
            statement.AddJsonValues("statement-type", new JsonElement("switch-statement"));
            statement.AddJsonValues("keyword", new JsonElement(SwitchStatement.SwitchKeywordRole.Token));
            switchStatement.Expression.AcceptVisitor(this);
            statement.AddJsonValues("expression", Pop());
            JsonArray sections = new JsonArray();
            foreach(var sec in switchStatement.SwitchSections)
            {
                sec.AcceptVisitor(this);
                var temp = Pop();
                if(temp != null)
                    sections.AddJsonValue(temp);
            }
            if(sections.Count == 0)
            {
                sections = null;
            }
            statement.AddJsonValues("switch-sections", sections);

            Push(statement);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitCaseLabel(CaseLabel caseLabel)
        {
            JsonObject label = new JsonObject();
            label.Comment = "VisitCaseLabel";
            if (caseLabel.Expression.IsNull)
            {
                label.AddJsonValues("keyword", new JsonElement(CaseLabel.DefaultKeywordRole.Token));
            }
            else
            {
                label.AddJsonValues("keyword", new JsonElement(CaseLabel.CaseKeywordRole.Token));
                caseLabel.Expression.AcceptVisitor(this);
                label.AddJsonValues("expression", Pop());
            }

            Push(label);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitThrowStatement(ThrowStatement throwStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitThrowStatement";
            statement.AddJsonValues("statement-type", new JsonElement("throw-statement"));
            statement.AddJsonValues("keyword", new JsonElement(ThrowStatement.ThrowKeywordRole.Token));
            if (!throwStatement.Expression.IsNull)
            {
                throwStatement.Expression.AcceptVisitor(this);
                statement.AddJsonValues("expression", Pop());
            }
            else
            {
                statement.AddJsonValues("expression", null);
            }
            Push(statement);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitTryCatchStatement(TryCatchStatement tryCatchStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitTryCatchStatement";
            statement.AddJsonValues("statement-type", new JsonElement("try-catch-statement"));
            statement.AddJsonValues("try-keyword", new JsonElement(TryCatchStatement.TryKeywordRole.Token));
            tryCatchStatement.TryBlock.AcceptVisitor(this);
            statement.AddJsonValues("try-block", Pop());
            JsonArray catchClauseList = new JsonArray();
            foreach(var catchClause in tryCatchStatement.CatchClauses)
            {
                catchClause.AcceptVisitor(this);
                catchClauseList.AddJsonValue(Pop());
            }
            statement.AddJsonValues("catch-clause", catchClauseList);
            if (!tryCatchStatement.FinallyBlock.IsNull)
            {
                statement.AddJsonValues("final-keyword", new JsonElement(TryCatchStatement.FinallyKeywordRole.Token));
                tryCatchStatement.FinallyBlock.AcceptVisitor(this);
                statement.AddJsonValues("final-block", Pop());
            }
            Push(statement);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitCatchClause(CatchClause catchClause)
        {
            JsonObject visitCatch = new JsonObject();
            visitCatch.Comment = "VisitCatchClause";
            visitCatch.AddJsonValues("catch-keyword", new JsonElement(CatchClause.CatchKeywordRole.Token));
            if (!catchClause.Type.IsNull)
            {
                catchClause.Type.AcceptVisitor(this);
                visitCatch.AddJsonValues("type-info", Pop());
                if (!string.IsNullOrEmpty(catchClause.VariableName))
                {
                    visitCatch.AddJsonValues("identifier", GetIdentifier(catchClause.VariableNameToken));
                }
            }
            catchClause.Body.AcceptVisitor(this);
            visitCatch.AddJsonValues("catch-body", Pop());

            Push(visitCatch);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitUncheckedStatement(UncheckedStatement uncheckedStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitUncheckedStatement";
            statement.AddJsonValues("statement-type", new JsonElement("unchecked-statement"));
            statement.AddJsonValues("keyword", new JsonElement(UncheckedStatement.UncheckedKeywordRole.Token));
            uncheckedStatement.Body.AcceptVisitor(this);
            statement.AddJsonValues("body", Pop());

            Push(statement);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitUnsafeStatement(UnsafeStatement unsafeStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitUnsafeStatement";
            statement.AddJsonValues("statement-type", new JsonElement("unsafe-statement"));
            statement.AddJsonValues("keyword", new JsonElement(UnsafeStatement.UnsafeKeywordRole.Token));
            unsafeStatement.Body.AcceptVisitor(this);
            statement.AddJsonValues("body", Pop());

            Push(statement);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitUsingStatement(UsingStatement usingStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitUsingStatement";
            statement.AddJsonValues("statement-type", new JsonElement("using-statement"));
            statement.AddJsonValues("keyword", new JsonElement(UsingStatement.UsingKeywordRole.Token));
            usingStatement.ResourceAcquisition.AcceptVisitor(this);
            statement.AddJsonValues("resource-acquisition", Pop());
            statement.AddJsonValues("embeded-statement", GetEmbeddedStatement(usingStatement.EmbeddedStatement));

            Push(statement);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitVariableDeclarationStatement";
            statement.AddJsonValues("statement-type", new JsonElement("variable-declaration"));
            JsonValue modifier = GetModifiers(variableDeclarationStatement.GetChildrenByRole(VariableDeclarationStatement.ModifierRole));
            statement.AddJsonValues("modifier", modifier);
            variableDeclarationStatement.Type.AcceptVisitor(this);
            statement.AddJsonValues("declaration-type-info", Pop());
            statement.AddJsonValues("variables-list", GetCommaSeparatedList(variableDeclarationStatement.Variables));
            Push(statement);
        }

        public void VisitWhileStatement(WhileStatement whileStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitWhileStatement";
            statement.AddJsonValues("statement-type", new JsonElement("while-statement"));
            whileStatement.Condition.AcceptVisitor(this);
            statement.AddJsonValues("condition", Pop());
            statement.AddJsonValues("statement-list", GetEmbeddedStatement(whileStatement.EmbeddedStatement));
            Push(statement);
        }

        public void VisitYieldBreakStatement(YieldBreakStatement yieldBreakStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitYieldBreakStatement";
            statement.AddJsonValues("statement-type", new JsonElement("yield-break-statement"));
            statement.AddJsonValues("yield-keyword", new JsonElement(YieldBreakStatement.YieldKeywordRole.Token));
            statement.AddJsonValues("break-keyword", new JsonElement(YieldBreakStatement.BreakKeywordRole.Token));
            Push(statement);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitYieldReturnStatement(YieldReturnStatement yieldReturnStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitYieldReturnStatement";
            statement.AddJsonValues("statement-type", new JsonElement("yield-return-statement"));
            statement.AddJsonValues("yield-keyword", new JsonElement(YieldReturnStatement.YieldKeywordRole.Token));
            statement.AddJsonValues("return-keyword", new JsonElement(YieldReturnStatement.ReturnKeywordRole.Token));
            yieldReturnStatement.Expression.AcceptVisitor(this);
            statement.AddJsonValues("expression", Pop());

            Push(statement);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        #endregion

        #region TypeMembers

        public void VisitAccessor(Accessor accessor)
        {
            JsonObject visitAccessor = new JsonObject();
            visitAccessor.Comment = "VisitAccessor";
            visitAccessor.AddJsonValues("attributes", GetAttributes(accessor.Attributes));
            visitAccessor.AddJsonValues("modifier", GetModifiers(accessor.ModifierTokens));
            if (accessor.Role == PropertyDeclaration.GetterRole)
            {
                visitAccessor.AddJsonValues("keyword", new JsonElement("get"));
            }
            else if(accessor.Role == PropertyDeclaration.SetterRole)
            {
                visitAccessor.AddJsonValues("keyword", new JsonElement("set"));
            }
            else if (accessor.Role == CustomEventDeclaration.AddAccessorRole)
            {
                visitAccessor.AddJsonValues("keyword", new JsonElement("add"));
            }
            else if (accessor.Role == CustomEventDeclaration.RemoveAccessorRole)
            {
                visitAccessor.AddJsonValues("keyword", new JsonElement("remove"));
            }
            visitAccessor.AddJsonValues("body", GetMethodBody(accessor.Body));

            Push(visitAccessor);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        int constructorNum = 0;
        public void VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration)
        {
            if (constructorNum > 0 || methodNum > 0)
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
                construct.AddJsonValues("initializer", Pop());
            }
            construct.AddJsonValues("body", GetMethodBody(constructorDeclaration.Body));
            Push(construct);
            constructorNum++;
        }

        public void VisitConstructorInitializer(ConstructorInitializer constructorInitializer)
        {
            JsonObject initializer = new JsonObject();
            initializer.Comment = "VisitConstructorInitializer";
            if(constructorInitializer.ConstructorInitializerType == ConstructorInitializerType.This)
            {
                initializer.AddJsonValues("keyword", new JsonElement(ConstructorInitializer.ThisKeywordRole.Token));
            }
            else
            {
                initializer.AddJsonValues("keyword", new JsonElement(ConstructorInitializer.BaseKeywordRole.Token));
            }
            initializer.AddJsonValues("arguments", GetCommaSeparatedList(constructorInitializer.Arguments));

            Push(initializer);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitDestructorDeclaration(DestructorDeclaration destructorDeclaration)
        {
            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitDestructorDeclaration";
            declaration.AddJsonValues("attributes", GetAttributes(destructorDeclaration.Attributes));
            declaration.AddJsonValues("modifier", GetModifiers(destructorDeclaration.ModifierTokens));
            declaration.AddJsonValues("tilde-role", new JsonElement(DestructorDeclaration.TildeRole.Token));
            TypeDeclaration type = destructorDeclaration.Parent as TypeDeclaration;
            if (type != null && type.Name != destructorDeclaration.Name)
            {
                declaration.AddJsonValues("name", GetIdentifier((Identifier)type.NameToken.Clone()));
            }
            else
            {
                declaration.AddJsonValues("name", GetIdentifier(destructorDeclaration.NameToken));
            }
            declaration.AddJsonValues("body", GetMethodBody(destructorDeclaration.Body));

            Push(declaration);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitEnumMemberDeclaration(EnumMemberDeclaration enumMemberDeclaration)
        {
            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitEnumMemberDeclaration";
            declaration.AddJsonValues("attributes", GetAttributes(enumMemberDeclaration.Attributes));
            declaration.AddJsonValues("modifier", GetModifiers(enumMemberDeclaration.ModifierTokens));
            declaration.AddJsonValues("identifier", GetIdentifier(enumMemberDeclaration.NameToken));
            if (!enumMemberDeclaration.Initializer.IsNull)
            {
                declaration.AddJsonValues("assign-role", new JsonElement(Roles.Assign.Token));
                enumMemberDeclaration.Initializer.AcceptVisitor(this);
                declaration.AddJsonValues("initializer", Pop());
            }

            Push(declaration);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitEventDeclaration(EventDeclaration eventDeclaration)
        {
            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitEventDeclaration";
            declaration.AddJsonValues("attributes", GetAttributes(eventDeclaration.Attributes));
            declaration.AddJsonValues("modifier", GetModifiers(eventDeclaration.ModifierTokens));
            declaration.AddJsonValues("keyword", new JsonElement(EventDeclaration.EventKeywordRole.Token));
            eventDeclaration.ReturnType.AcceptVisitor(this);
            declaration.AddJsonValues("return-type", Pop());
            declaration.AddJsonValues("variables", GetCommaSeparatedList(eventDeclaration.Variables));

            Push(declaration);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitCustomEventDeclaration(CustomEventDeclaration customEventDeclaration)
        {
            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitCustomEventDeclaration";
            declaration.AddJsonValues("attributes", GetAttributes(customEventDeclaration.Attributes));
            declaration.AddJsonValues("modifier", GetModifiers(customEventDeclaration.ModifierTokens));
            declaration.AddJsonValues("keyword", new JsonElement(EventDeclaration.EventKeywordRole.Token));
            customEventDeclaration.ReturnType.AcceptVisitor(this);
            declaration.AddJsonValues("return-type", Pop());
            declaration.AddJsonValues("private-implementation-type", GetPrivateImplementationType(customEventDeclaration.PrivateImplementationType));
            declaration.AddJsonValues("identifier", GetIdentifier(customEventDeclaration.NameToken));
            JsonArray children = new JsonArray();
            foreach(AstNode node in customEventDeclaration.Children)
            {
                if (node.Role == CustomEventDeclaration.AddAccessorRole || node.Role == CustomEventDeclaration.RemoveAccessorRole)
                {
                    node.AcceptVisitor(this);
                    var temp = Pop();
                    if (temp != null)
                    {
                        children.AddJsonValue(temp);
                    }
                }
            }
            if (children.Count == 0)
            {
                children = null;
            }
            declaration.AddJsonValues("children", children);

            Push(declaration);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitFieldDeclaration(FieldDeclaration fieldDeclaration)
        {
            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitFieldDeclaration";
            declaration.AddJsonValues("attributes", GetAttributes(fieldDeclaration.Attributes));
            declaration.AddJsonValues("modifier", GetModifiers(fieldDeclaration.ModifierTokens));
            fieldDeclaration.ReturnType.AcceptVisitor(this);
            declaration.AddJsonValues("return-type", Pop());
            declaration.AddJsonValues("variables", GetCommaSeparatedList(fieldDeclaration.Variables));

            Push(declaration);
        }

        public void VisitFixedFieldDeclaration(FixedFieldDeclaration fixedFieldDeclaration)
        {
            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitFixedFieldDeclaration";
            declaration.AddJsonValues("attributes", GetAttributes(fixedFieldDeclaration.Attributes));
            declaration.AddJsonValues("modifier", GetModifiers(fixedFieldDeclaration.ModifierTokens));
            declaration.AddJsonValues("keyword", new JsonElement(FixedFieldDeclaration.FixedKeywordRole.Token));
            fixedFieldDeclaration.ReturnType.AcceptVisitor(this);
            declaration.AddJsonValues("return-type", Pop());
            declaration.AddJsonValues("variables", GetCommaSeparatedList(fixedFieldDeclaration.Variables));

            Push(declaration);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitFixedVariableInitializer(FixedVariableInitializer fixedVariableInitializer)
        {
            JsonObject initializer = new JsonObject();
            initializer.Comment = "VisitFixedVariableInitializer";
            initializer.AddJsonValues("identifier", GetIdentifier(fixedVariableInitializer.NameToken));
            if (!fixedVariableInitializer.CountExpression.IsNull)
            {
                fixedVariableInitializer.CountExpression.AcceptVisitor(this);
                initializer.AddJsonValues("count-expression", Pop());
            }
            else
            {
                initializer.AddJsonValues("count-expression", null);
            }
            Push(initializer);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration)
        {
            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitIndexerDeclaration";
            declaration.AddJsonValues("attributes", GetAttributes(indexerDeclaration.Attributes));
            declaration.AddJsonValues("modifier", GetModifiers(indexerDeclaration.ModifierTokens));
            indexerDeclaration.ReturnType.AcceptVisitor(this);
            declaration.AddJsonValues("return-type", Pop());
            declaration.AddJsonValues("private-implementation-type", GetPrivateImplementationType(indexerDeclaration.PrivateImplementationType));
            declaration.AddJsonValues("keyword", new JsonElement(IndexerDeclaration.ThisKeywordRole.Token));
            declaration.AddJsonValues("parameters", GetCommaSeparatedList(indexerDeclaration.Parameters));
            JsonArray children = new JsonArray();
            foreach(AstNode node in indexerDeclaration.Children)
            {
                if(node.Role == IndexerDeclaration.GetterRole || node.Role == IndexerDeclaration.SetterRole)
                {
                    node.AcceptVisitor(this);
                    var temp = Pop();
                    if (temp != null)
                    {
                        children.AddJsonValue(temp);
                    }
                }
            }
            if (children.Count == 0)
            {
                children = null;
            }
            declaration.AddJsonValues("children", children);

            Push(declaration);
            //implement already, but not tested
            throw new Exception("first time testing");
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
            method.AddJsonValues("return-type", Pop());
            //write parameters
            method.AddJsonValues("parameters", GetCommaSeparatedList(methodDeclaration.Parameters));
            //write body
            method.AddJsonValues("body", GetMethodBody(methodDeclaration.Body));
            //write method type info
            method.AddJsonValues("type-info-list", GetMethodTypeInfo(new List<string>(typeInfoIndex.Keys)));
            methodList.AddJsonValue(method);

            Push(method);
            methodNum++;
        }

        public void VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration)
        {
            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitOperatorDeclaration";
            declaration.AddJsonValues("attributes", GetAttributes(operatorDeclaration.Attributes));
            declaration.AddJsonValues("modifier", GetModifiers(operatorDeclaration.ModifierTokens));
            if (operatorDeclaration.OperatorType == OperatorType.Explicit)
            {
                declaration.AddJsonValues("keyword", new JsonElement(OperatorDeclaration.ExplicitRole.Token));
            }
            else if (operatorDeclaration.OperatorType == OperatorType.Implicit)
            {
                declaration.AddJsonValues("keyword", new JsonElement(OperatorDeclaration.ImplicitRole.Token));
            }
            else
            {
                operatorDeclaration.ReturnType.AcceptVisitor(this);
                declaration.AddJsonValues("return-type", Pop());
            }
            declaration.AddJsonValues("operator-keyword", new JsonElement(OperatorDeclaration.OperatorKeywordRole.Token));
            if(operatorDeclaration.OperatorType== OperatorType.Explicit
                || operatorDeclaration.OperatorType == OperatorType.Implicit)
            {
                operatorDeclaration.ReturnType.AcceptVisitor(this);
                declaration.AddJsonValues("return-type", Pop());
            }
            else
            {
                declaration.AddJsonValues("operator-type", new JsonElement(OperatorDeclaration.GetToken(operatorDeclaration.OperatorType)));
            }
            declaration.AddJsonValues("parameters", GetCommaSeparatedList(operatorDeclaration.Parameters));
            declaration.AddJsonValues("body", GetMethodBody(operatorDeclaration.Body));

            Push(declaration);
            //implement already, but not tested
            throw new Exception("first time testing");
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
            Push(parameter);
        }

        public void VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
        {
            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitPropertyDeclaration";
            declaration.AddJsonValues("attributes", GetAttributes(propertyDeclaration.Attributes));
            declaration.AddJsonValues("modifier", GetModifiers(propertyDeclaration.ModifierTokens));
            propertyDeclaration.ReturnType.AcceptVisitor(this);
            declaration.AddJsonValues("return-type", Pop());
            declaration.AddJsonValues("private-implementation-type", GetPrivateImplementationType(propertyDeclaration.PrivateImplementationType));
            declaration.AddJsonValues("identifier", GetIdentifier(propertyDeclaration.NameToken));
            JsonArray children = new JsonArray();
            foreach (AstNode node in propertyDeclaration.Children)
            {
                if (node.Role == IndexerDeclaration.GetterRole || node.Role == IndexerDeclaration.SetterRole)
                {
                    node.AcceptVisitor(this);
                    var temp = Pop();
                    if (temp != null)
                    {
                        children.AddJsonValue(temp);
                    }
                }
            }
            if (children.Count == 0)
            {
                children = null;
            }
            declaration.AddJsonValues("children", children);

            Push(declaration);
            //implement already, but not tested
            throw new Exception("first time testing");
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
                variable.AddJsonValues("initializer", Pop());
            }
            else
            {
                variable.AddJsonValues("initializer", null);
            }
            Push(variable);
        }

        public void VisitSimpleType(SimpleType simpleType)
        {
            Push(GetTypeInfo(simpleType.IdentifierToken.Name));
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
                arr.AddJsonValue(Pop());
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
            memtype.AddJsonValues("type-info", Pop());
            memtype.AddJsonValues("member-name", GetIdentifier(memberType.MemberNameToken));
            Push(memtype);
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
                arrSpec.AddJsonValue(Pop());
            }
            if (arrSpec.Count == 0)
            {
                arrSpec = null;
            }
            Push(arrSpec);
        }

        public void VisitPrimitiveType(PrimitiveType primitiveType)
        {
            Push(GetTypeInfo(primitiveType.Keyword));
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
                Push(element);
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
            Push(null);
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