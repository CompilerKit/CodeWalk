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

        JsonValue GetAttributes(IEnumerable<AttributeSection> attributes)
        {
            JsonArray attrArray = new JsonArray();
            foreach(AttributeSection attr in attributes)
            {
                attr.AcceptVisitor(this);
                attrArray.AddJsonValue(jsonValueStack.Pop());
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
            expression.AddJsonValues("body", jsonValueStack.Pop());
            jsonValueStack.Push(expression);
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
            jsonValueStack.Push(expression);
            throw new Exception("first time testing");//implement already, but not tested
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
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitAsExpression";
            expression.AddJsonValues("expression-type", new JsonElement("as-expression"));
            expression.AddJsonValues("keyword", new JsonElement(AsExpression.AsKeywordRole.Token));
            asExpression.Type.AcceptVisitor(this);
            expression.AddJsonValues("type-info", jsonValueStack.Pop());

            jsonValueStack.Push(expression);
            throw new Exception("first time testing");//implement already, but not tested
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
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitBaseReferenceExpression";
            expression.AddJsonValues("expression-type", new JsonElement("base-reference-expression"));
            expression.AddJsonValues("keyword", new JsonElement("base"));

            jsonValueStack.Push(expression);
            throw new Exception("first time testing");//implement already, but not tested
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
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitCastExpression";
            expression.AddJsonValues("expression-type", new JsonElement("cast-expression"));
            castExpression.Type.AcceptVisitor(this);
            expression.AddJsonValues("type-info", jsonValueStack.Pop());
            castExpression.Expression.AcceptVisitor(this);
            expression.AddJsonValues("expression", jsonValueStack.Pop());

            jsonValueStack.Push(expression);
        }

        public void VisitCheckedExpression(CheckedExpression checkedExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitCheckedExpression";
            expression.AddJsonValues("expression-type", new JsonElement("checked-expression"));
            expression.AddJsonValues("keyword", new JsonElement(CheckedExpression.CheckedKeywordRole.Token));
            checkedExpression.Expression.AcceptVisitor(this);
            expression.AddJsonValues("expression", jsonValueStack.Pop());

            jsonValueStack.Push(expression);
            throw new Exception("first time testing");//implement already, but not tested
        }

        public void VisitConditionalExpression(ConditionalExpression conditionalExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitConditionalExpression";
            expression.AddJsonValues("expression-type", new JsonElement("conditional-expression"));
            conditionalExpression.Condition.AcceptVisitor(this);
            expression.AddJsonValues("condition", jsonValueStack.Pop());
            conditionalExpression.TrueExpression.AcceptVisitor(this);
            expression.AddJsonValues("true-expression", jsonValueStack.Pop());
            conditionalExpression.FalseExpression.AcceptVisitor(this);
            expression.AddJsonValues("false-expression", jsonValueStack.Pop());

            jsonValueStack.Push(expression);
            throw new Exception("first time testing");//implement already, but not tested
        }

        public void VisitDefaultValueExpression(DefaultValueExpression defaultValueExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitDefaultValueExpression";
            expression.AddJsonValues("expression-type", new JsonElement("default-value-expression"));
            expression.AddJsonValues("keyword", new JsonElement(DefaultValueExpression.DefaultKeywordRole.Token));
            defaultValueExpression.Type.AcceptVisitor(this);
            expression.AddJsonValues("type-info", jsonValueStack.Pop());

            jsonValueStack.Push(expression);
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
            expression.AddJsonValues("expression", jsonValueStack.Pop());

            jsonValueStack.Push(expression);
            throw new Exception("first time testing");//implement already, but not tested
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
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitIsExpression";
            expression.AddJsonValues("expression-type", new JsonElement("is-expression"));
            expression.AddJsonValues("keyword", new JsonElement(IsExpression.IsKeywordRole.Token));
            isExpression.Type.AcceptVisitor(this);
            expression.AddJsonValues("type-info", jsonValueStack.Pop());
            isExpression.Expression.AcceptVisitor(this);
            expression.AddJsonValues("expression", jsonValueStack.Pop());

            jsonValueStack.Push(expression);
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
                expression.AddJsonValues("parameters", jsonValueStack.Pop());
            }
            lambdaExpression.Body.AcceptVisitor(this);
            expression.AddJsonValues("body", jsonValueStack.Pop());

            jsonValueStack.Push(expression);
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
            expression.AddJsonValues("type-info", jsonValueStack.Pop());
            jsonValueStack.Push(expression);
        }

        public void VisitNamedArgumentExpression(NamedArgumentExpression namedArgumentExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitNamedArgumentExpression";
            expression.AddJsonValues("expression-type", new JsonElement("named-argument-expression"));
            expression.AddJsonValues("identifier", GetIdentifier(namedArgumentExpression.NameToken));
            namedArgumentExpression.Expression.AcceptVisitor(this);
            expression.AddJsonValues("expression", jsonValueStack.Pop());

            jsonValueStack.Push(expression);
            throw new Exception("first time testing");//implement already, but not tested
        }

        public void VisitNamedExpression(NamedExpression namedExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitNamedExpression";
            expression.AddJsonValues("expression-type", new JsonElement("named-expression"));
            expression.AddJsonValues("identifier", GetIdentifier(namedExpression.NameToken));
            namedExpression.Expression.AcceptVisitor(this);
            expression.AddJsonValues("expression", jsonValueStack.Pop());

            jsonValueStack.Push(expression);
            throw new Exception("first time testing");//implement already, but not tested
        }

        public void VisitNullReferenceExpression(NullReferenceExpression nullReferenceExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitNullReferenceExpression";
            expression.AddJsonValues("expression-type", new JsonElement("null-reference"));
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
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitAnonymousTypeCreateExpression";
            expression.AddJsonValues("expression-type", new JsonElement("anonymous-type-create-expression"));
            expression.AddJsonValues("keyword", new JsonElement(AnonymousTypeCreateExpression.NewKeywordRole.Token));
            expression.AddJsonValues("elements", GetInitializerElements(anonymousTypeCreateExpression.Initializers));
            jsonValueStack.Push(expression);
            throw new Exception("first time testing");//implement already, but not tested
        }

        public void VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitParenthesizedExpression";
            expression.AddJsonValues("expression-type", new JsonElement("parenthesized-expression"));
            parenthesizedExpression.Expression.AcceptVisitor(this);
            expression.AddJsonValues("expression", jsonValueStack.Pop());

            jsonValueStack.Push(expression);
            throw new Exception("first time testing");//implement already, but not tested
        }

        public void VisitPointerReferenceExpression(PointerReferenceExpression pointerReferenceExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitPointerReferenceExpression";
            expression.AddJsonValues("expression-type", new JsonElement("pointer-reference-expression"));
            pointerReferenceExpression.Target.AcceptVisitor(this);
            expression.AddJsonValues("target", jsonValueStack.Pop());
            expression.AddJsonValues("identifier", GetIdentifier(pointerReferenceExpression.MemberNameToken));
            expression.AddJsonValues("type-arguments", GetTypeArguments(pointerReferenceExpression.TypeArguments));

            jsonValueStack.Push(expression);

            throw new Exception("first time testing");//implement already, but not tested
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
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitSizeOfExpression";
            expression.AddJsonValues("expression-type", new JsonElement("sizeof-expression"));
            expression.AddJsonValues("keyword", new JsonElement(SizeOfExpression.SizeofKeywordRole.Token));
            sizeOfExpression.Type.AcceptVisitor(this);
            expression.AddJsonValues("type-info", jsonValueStack.Pop());

            jsonValueStack.Push(expression);
            throw new Exception("first time testing");//implement already, but not tested
        }

        public void VisitStackAllocExpression(StackAllocExpression stackAllocExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitStackAllocExpression";
            expression.AddJsonValues("expression-type", new JsonElement("stack-alloc-expression"));
            stackAllocExpression.Type.AcceptVisitor(this);
            expression.AddJsonValues("type-info", jsonValueStack.Pop());
            expression.AddJsonValues("count-expression", GetCommaSeparatedList(new[] { stackAllocExpression.CountExpression }));

            jsonValueStack.Push(expression);
            throw new Exception("first time testing");//implement already, but not tested
        }

        public void VisitThisReferenceExpression(ThisReferenceExpression thisReferenceExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitThisReferenceExpression";
            expression.AddJsonValues("expression-type", new JsonElement("this-reference-expression"));
            expression.AddJsonValues("keyword", new JsonElement("this"));
            jsonValueStack.Push(expression);
        }

        public void VisitTypeOfExpression(TypeOfExpression typeOfExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitTypeOfExpression";
            expression.AddJsonValues("expression-type", new JsonElement("typeof-expression"));
            expression.AddJsonValues("keyword", new JsonElement(TypeOfExpression.TypeofKeywordRole.Token));
            typeOfExpression.Type.AcceptVisitor(this);
            expression.AddJsonValues("type-info", jsonValueStack.Pop());

            jsonValueStack.Push(expression);
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
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitUncheckedExpression";
            expression.AddJsonValues("expression-type", new JsonElement("unchecked-expression"));
            expression.AddJsonValues("keyword", new JsonElement(UncheckedExpression.UncheckedKeywordRole.Token));
            uncheckedExpression.Expression.AcceptVisitor(this);
            expression.AddJsonValues("expression", jsonValueStack.Pop());

            jsonValueStack.Push(expression);
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
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitBreakStatement";
            statement.AddJsonValues("statement-type", new JsonElement("break-statement"));
            statement.AddJsonValues("keyword", new JsonElement("break"));

            jsonValueStack.Push(statement);
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
            statement.AddJsonValues("body", jsonValueStack.Pop());

            jsonValueStack.Push(statement);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitContinueStatement(ContinueStatement continueStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitContinueStatement";
            statement.AddJsonValues("statement-type", new JsonElement("continue-statement"));
            statement.AddJsonValues("keyword", new JsonElement(ContinueStatement.ContinueKeywordRole.Token));

            jsonValueStack.Push(statement);
            //implement already, but not tested
            throw new Exception("first time testing");
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
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitEmptyStatement";
            statement.AddJsonValues("statement-type", new JsonElement("empty-statement"));
            jsonValueStack.Push(statement);
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
            statement.AddJsonValues("type-info", jsonValueStack.Pop());
            statement.AddJsonValues("variables", GetCommaSeparatedList(fixedStatement.Variables));
            statement.AddJsonValues("embedded-statement", GetEmbeddedStatement(fixedStatement.EmbeddedStatement));

            jsonValueStack.Push(statement);
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
            statement.AddJsonValues("local-variable-type", jsonValueStack.Pop());
            statement.AddJsonValues("local-variable-name", GetIdentifier(foreachStatement.VariableNameToken));
            foreachStatement.InExpression.AcceptVisitor(this);
            statement.AddJsonValues("in-expression", jsonValueStack.Pop());
            statement.AddJsonValues("embedded-statement", GetEmbeddedStatement(foreachStatement.EmbeddedStatement));
            jsonValueStack.Push(statement);
        }

        public void VisitForStatement(ForStatement forStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitForStatement";
            statement.AddJsonValues("keyword", new JsonElement(ForStatement.ForKeywordRole.Token));
            statement.AddJsonValues("initializer", GetCommaSeparatedList(forStatement.Initializers));
            forStatement.Condition.AcceptVisitor(this);
            statement.AddJsonValues("condition", jsonValueStack.Pop());
            if (forStatement.Iterators.Any())
            {
                statement.AddJsonValues("iterators", GetCommaSeparatedList(forStatement.Iterators));
            }
            statement.AddJsonValues("embedded-statement", GetEmbeddedStatement(forStatement.EmbeddedStatement));
            jsonValueStack.Push(statement);
        }

        public void VisitGotoCaseStatement(GotoCaseStatement gotoCaseStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitGotoCaseStatement";
            statement.AddJsonValues("statement-type", new JsonElement("goto-case-statement"));
            statement.AddJsonValues("goto-keyword", new JsonElement(GotoCaseStatement.GotoKeywordRole.Token));
            statement.AddJsonValues("case-keyword", new JsonElement(GotoCaseStatement.CaseKeywordRole.Token));
            gotoCaseStatement.LabelExpression.AcceptVisitor(this);
            statement.AddJsonValues("label-expression", jsonValueStack.Pop());
            jsonValueStack.Push(statement);
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
            jsonValueStack.Push(statement);
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

            jsonValueStack.Push(statement);
            //implement already, but not tested
            throw new Exception("first time testing");
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
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitLabelStatement";
            statement.AddJsonValues("statement-type", new JsonElement("label-statement"));
            statement.AddJsonValues("identifier", GetIdentifier(labelStatement.GetChildByRole(Roles.Identifier)));

            jsonValueStack.Push(statement);
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
            statement.AddJsonValues("expression", jsonValueStack.Pop());
            statement.AddJsonValues("embedded-statement", GetEmbeddedStatement(lockStatement.EmbeddedStatement));

            jsonValueStack.Push(statement);
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
                statement.AddJsonValues("expression", jsonValueStack.Pop());
            }
            jsonValueStack.Push(statement);
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
            JsonObject fieldDeclare = new JsonObject();
            fieldDeclare.Comment = "VisitFieldDeclaration";
            fieldDeclare.AddJsonValues("attributes", GetAttributes(fieldDeclaration.Attributes));
            fieldDeclare.AddJsonValues("modifier", GetModifiers(fieldDeclaration.ModifierTokens));
            fieldDeclaration.ReturnType.AcceptVisitor(this);
            fieldDeclare.AddJsonValues("return-type", jsonValueStack.Pop());
            fieldDeclare.AddJsonValues("variables", GetCommaSeparatedList(fieldDeclaration.Variables));

            jsonValueStack.Push(fieldDeclare);
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