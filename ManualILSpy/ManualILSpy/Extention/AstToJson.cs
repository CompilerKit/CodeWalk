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
    public class AstCSharpToJsonVisitor : IAstVisitor
    {
        Stack<JsonValue> jsonValueStack = new Stack<JsonValue>();
        Dictionary<string, int> typeInfo = new Dictionary<string, int>();
        bool isLambda = false;

        public JsonValue LastValue { get; private set; }
        
        public AstCSharpToJsonVisitor(ITextOutput output)
        {
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

        #region Type info
        void ClearTypeInfo()
        {
            typeInfo.Clear();
        }

        List<string> TypeInfoKeys()
        {
            return new List<string>(typeInfo.Keys);
        }

        int GetTypeIndex(string type)
        {
            int index = 0;
            if (!typeInfo.TryGetValue(type, out index))
            {
                index = typeInfo.Count;
                typeInfo.Add(type, index);
            }
            return index;
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

        JsonElement GetIdentifier(Identifier identifier)
        {
            string name = identifier.Name;
            if (name[0] == '<' && name[1] == '>')
            {
                isLambda = true;
            }
            return new JsonElement(name);
        }

        JsonValue GetKeyword(TokenRole tokenRole)
        {
            return new JsonElement(tokenRole.Token);
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
            expression.AddJsonValues("keyword", GetKeyword(AnonymousMethodExpression.AsyncModifierRole));
            expression.AddJsonValues("delegate-keyword", GetKeyword(AnonymousMethodExpression.DelegateKeywordRole));
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
                    expression.AddJsonValues("type-info", GetKeyword(UndocumentedExpression.ArglistKeywordRole));
                    break;
                case UndocumentedExpressionType.MakeRef:
                    expression.AddJsonValues("type-info", GetKeyword(UndocumentedExpression.MakerefKeywordRole));
                    break;
                case UndocumentedExpressionType.RefType:
                    expression.AddJsonValues("type-info", GetKeyword(UndocumentedExpression.ReftypeKeywordRole));
                    break;
                case UndocumentedExpressionType.RefValue:
                    expression.AddJsonValues("type-info", GetKeyword(UndocumentedExpression.RefvalueKeywordRole));
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
            expression.AddJsonValues("keyword", GetKeyword(ArrayCreateExpression.NewKeywordRole));
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
            expression.AddJsonValues("keyword", GetKeyword(AsExpression.AsKeywordRole));
            asExpression.Type.AcceptVisitor(this);
            expression.AddJsonValues("type-info", Pop());

            Push(expression);
        }

        public void VisitAssignmentExpression(AssignmentExpression assignmentExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitAssignmentExpression";
            expression.AddJsonValues("expression-type", new JsonElement("assignment-expression"));
            assignmentExpression.Left.AcceptVisitor(this);
            expression.AddJsonValues("left-operand", Pop());
            TokenRole operatorRole = AssignmentExpression.GetOperatorRole(assignmentExpression.Operator);
            expression.AddJsonValues("operator", GetKeyword(operatorRole));
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
            expression.AddJsonValues("keyword", GetKeyword(CheckedExpression.CheckedKeywordRole));
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
            //throw new Exception("first time testing");//implement already, but not tested
        }

        public void VisitDefaultValueExpression(DefaultValueExpression defaultValueExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitDefaultValueExpression";
            expression.AddJsonValues("expression-type", new JsonElement("default-value-expression"));
            expression.AddJsonValues("keyword", GetKeyword(DefaultValueExpression.DefaultKeywordRole));
            defaultValueExpression.Type.AcceptVisitor(this);
            expression.AddJsonValues("type-info", Pop());

            Push(expression);
            //throw new Exception("first time testing");//implement already, but not tested
        }

        public void VisitDirectionExpression(DirectionExpression directionExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitDirectionExpression";
            expression.AddJsonValues("expression-type", new JsonElement("direction-expression"));
            switch (directionExpression.FieldDirection)
            {
                case FieldDirection.Out:
                    expression.AddJsonValues("keyword", GetKeyword(DirectionExpression.OutKeywordRole));
                    break;
                case FieldDirection.Ref:
                    expression.AddJsonValues("keyword", GetKeyword(DirectionExpression.RefKeywordRole));
                    break;
                default:
                    throw new NotSupportedException("Invalid value for FieldDirection");
            }
            directionExpression.Expression.AcceptVisitor(this);
            expression.AddJsonValues("expression", Pop());

            Push(expression);
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
            expression.AddJsonValues("keyword", GetKeyword(IsExpression.IsKeywordRole));
            isExpression.Type.AcceptVisitor(this);
            expression.AddJsonValues("type-info", Pop());
            isExpression.Expression.AcceptVisitor(this);
            expression.AddJsonValues("expression", Pop());

            Push(expression);
        }

        public void VisitLambdaExpression(LambdaExpression lambdaExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitLambdaExpression";
            expression.AddJsonValues("expression-type", new JsonElement("lambda-expression"));
            if (lambdaExpression.IsAsync)
            {
                expression.AddJsonValues("async-keyword", GetKeyword(LambdaExpression.AsyncModifierRole));
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
            expression.AddJsonValues("keyword", GetKeyword(ObjectCreateExpression.NewKeywordRole));
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
            expression.AddJsonValues("keyword", GetKeyword(AnonymousTypeCreateExpression.NewKeywordRole));
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
            //throw new Exception("first time testing");//implement already, but not tested
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
            expression.AddJsonValues("unsafe-literal-value", new JsonElement(primitiveExpression.UnsafeLiteralValue));
            Push(expression);
        }
        #endregion

        public void VisitSizeOfExpression(SizeOfExpression sizeOfExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitSizeOfExpression";
            expression.AddJsonValues("expression-type", new JsonElement("sizeof-expression"));
            expression.AddJsonValues("keyword", GetKeyword(SizeOfExpression.SizeofKeywordRole));
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
            expression.AddJsonValues("keyword", GetKeyword(TypeOfExpression.TypeofKeywordRole));
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
                expression.AddJsonValues("symbol", GetKeyword(opSymbol));
            }
            else if (!(opType == UnaryOperatorType.PostIncrement || opType == UnaryOperatorType.PostDecrement))
            {
                expression.AddJsonValues("symbol", GetKeyword(opSymbol));
            }
            unaryOperatorExpression.Expression.AcceptVisitor(this);
            expression.AddJsonValues("expression", Pop());
            if (opType == UnaryOperatorType.PostIncrement || opType == UnaryOperatorType.PostDecrement)
            {
                expression.AddJsonValues("symbol", GetKeyword(opSymbol));
            }
            Push(expression);
        }

        public void VisitUncheckedExpression(UncheckedExpression uncheckedExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitUncheckedExpression";
            expression.AddJsonValues("expression-type", new JsonElement("unchecked-expression"));
            expression.AddJsonValues("keyword", GetKeyword(UncheckedExpression.UncheckedKeywordRole));
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
        }

        public void VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration)
        {
            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitDelegateDeclaration";
            declaration.AddJsonValues("attributes", GetAttributes(delegateDeclaration.Attributes));
            declaration.AddJsonValues("modifier", GetModifiers(delegateDeclaration.ModifierTokens));
            declaration.AddJsonValues("keyword", GetKeyword(Roles.DelegateKeyword));
            delegateDeclaration.ReturnType.AcceptVisitor(this);
            declaration.AddJsonValues("return-type", Pop());
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
        }

        public void VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration)
        {
            ClearTypeInfo();
            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitNamespaceDeclaration";
            declaration.AddJsonValues("keyword", GetKeyword(Roles.NamespaceKeyword));
            namespaceDeclaration.NamespaceName.AcceptVisitor(this);
            declaration.AddJsonValues("namespace-name", Pop());
            declaration.AddJsonValues("namespace-info-list", GetMethodTypeInfo(TypeInfoKeys()));
            JsonArray memberList = new JsonArray();
            foreach(var member in namespaceDeclaration.Members)
            {
                member.AcceptVisitor(this);
                var temp = Pop();
                if (temp != null && !isLambda)
                {
                    memberList.AddJsonValue(temp);
                }
                isLambda = false;
            }
            if (memberList.Count == 0)
            {
                memberList = null;
            }
            declaration.AddJsonValues("members", memberList);

            Push(declaration);
        }

        Dictionary<string, TypeDeclaration> lambdaClass = new Dictionary<string, TypeDeclaration>();
        public void VisitTypeDeclaration(TypeDeclaration typeDeclaration)
        {
            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitTypeDeclaration";
            declaration.AddJsonValues("attributes", GetAttributes(typeDeclaration.Attributes));
            declaration.AddJsonValues("modifiers", GetModifiers(typeDeclaration.ModifierTokens));
            switch (typeDeclaration.ClassType)
            {
                case ClassType.Enum:
                    declaration.AddJsonValues("keyword", GetKeyword(Roles.EnumKeyword));
                    break;
                case ClassType.Interface:
                    declaration.AddJsonValues("keyword", GetKeyword(Roles.InterfaceKeyword));
                    break;
                case ClassType.Struct:
                    declaration.AddJsonValues("keyword", GetKeyword(Roles.StructKeyword));
                    break;
                default:
                    declaration.AddJsonValues("keyword", GetKeyword(Roles.ClassKeyword));
                    break;
            }
            JsonElement identifier = GetIdentifier(typeDeclaration.NameToken);
            bool thisTypeIsLamda = false;
            if (isLambda)
            {
                thisTypeIsLamda = true;
                lambdaClass[identifier.ElementValue] = typeDeclaration;
                isLambda = false;
            }
            declaration.AddJsonValues("identifier", identifier);
            declaration.AddJsonValues("parameters", GetTypeParameters(typeDeclaration.TypeParameters));
            if (typeDeclaration.BaseTypes.Any())
            {
                declaration.AddJsonValues("base-types", GetCommaSeparatedList(typeDeclaration.BaseTypes));
            }
            JsonArray constraintArr = new JsonArray();
            foreach(Constraint constraint in typeDeclaration.Constraints)
            {
                constraint.AcceptVisitor(this);
                constraintArr.AddJsonValue(Pop());
            }
            declaration.AddJsonValues("constraint", constraintArr);
            JsonArray memberArr = new JsonArray();
            foreach (var member in typeDeclaration.Members)
            {
                member.AcceptVisitor(this);
                memberArr.AddJsonValue(Pop());
            }
            declaration.AddJsonValues("members", memberArr);
            if (thisTypeIsLamda)
            {
                declaration = null;
            }
            Push(declaration);
            isLambda = false;
        }

        public void VisitUsingAliasDeclaration(UsingAliasDeclaration usingAliasDeclaration)
        {
            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitUsingAliasDeclaration";
            declaration.AddJsonValues("keyword", GetKeyword(UsingAliasDeclaration.UsingKeywordRole));
            declaration.AddJsonValues("identifier", GetIdentifier(usingAliasDeclaration.GetChildByRole(UsingAliasDeclaration.AliasRole)));
            declaration.AddJsonValues("assign-token", GetKeyword(Roles.Assign));
            usingAliasDeclaration.Import.AcceptVisitor(this);
            declaration.AddJsonValues("import", Pop());

            Push(declaration);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitUsingDeclaration(UsingDeclaration usingDeclaration)
        {
            ClearTypeInfo();
            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitUsingDeclaration";
            declaration.AddJsonValues("keyword", GetKeyword(UsingAliasDeclaration.UsingKeywordRole));
            usingDeclaration.Import.AcceptVisitor(this);
            declaration.AddJsonValues("import", Pop());
            declaration.AddJsonValues("import-info-list", GetMethodTypeInfo(TypeInfoKeys()));
            Push(declaration);
        }

        public void VisitExternAliasDeclaration(ExternAliasDeclaration externAliasDeclaration)
        {
            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitExternAliasDeclaration";
            declaration.AddJsonValues("extern-keyword", GetKeyword(Roles.ExternKeyword));
            declaration.AddJsonValues("alias-keyword", GetKeyword(Roles.AliasKeyword));
            declaration.AddJsonValues("identifier", GetIdentifier(externAliasDeclaration.NameToken));

            Push(declaration);
            //implement already, but not tested
            throw new Exception("first time testing");
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
        }

        public void VisitCheckedStatement(CheckedStatement checkedStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitCheckedStatement";
            statement.AddJsonValues("statement-type", new JsonElement("checked-statement"));
            statement.AddJsonValues("keyword", GetKeyword(CheckedStatement.CheckedKeywordRole));
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
            statement.AddJsonValues("keyword", GetKeyword(ContinueStatement.ContinueKeywordRole));

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
            statement.AddJsonValues("keyword", GetKeyword(FixedStatement.FixedKeywordRole));
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
            statement.AddJsonValues("keyword", GetKeyword(ForeachStatement.ForeachKeywordRole));
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
            statement.AddJsonValues("keyword", GetKeyword(ForStatement.ForKeywordRole));
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
            statement.AddJsonValues("goto-keyword", GetKeyword(GotoCaseStatement.GotoKeywordRole));
            statement.AddJsonValues("case-keyword", GetKeyword(GotoCaseStatement.CaseKeywordRole));
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
            statement.AddJsonValues("goto-keyword", GetKeyword(GotoDefaultStatement.GotoKeywordRole));
            statement.AddJsonValues("default-keyword", GetKeyword(GotoDefaultStatement.DefaultKeywordRole));
            Push(statement);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitGotoStatement(GotoStatement gotoStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitGotoStatement";
            statement.AddJsonValues("statement-type", new JsonElement("goto-statement"));
            statement.AddJsonValues("keyword", GetKeyword(GotoStatement.GotoKeywordRole));
            statement.AddJsonValues("identifier", GetIdentifier(gotoStatement.GetChildByRole(Roles.Identifier)));

            Push(statement);
        }
        bool test11;
        public void VisitIfElseStatement(IfElseStatement ifElseStatement)
        {
            test11 = true;
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitIfElseStatement";
            ifElseStatement.Condition.AcceptVisitor(this);
            statement.AddJsonValues("condition", Pop());
            ifElseStatement.TrueStatement.AcceptVisitor(this);
            statement.AddJsonValues("true-statement", Pop());
            ifElseStatement.FalseStatement.AcceptVisitor(this);
            statement.AddJsonValues("false-statement", Pop());
            if (isLambda)
            {
                CreateLamda(statement);
                isLambda = false;
                test11 = false;
                return;
            }
            test11 = false;
            Push(statement);
        }
        void CreateLamda(JsonObject ifElseNode)
        {
            JsonObject condition = GetValue("condition", ifElseNode);
            JsonObject leftOperand = GetValue("left-operand", condition);
            JsonElement identifier = GetElement("left-operand", leftOperand);

            JsonObject trueStatement = GetValue("true-statement", ifElseNode);
            JsonArray list = GetArray("statement-list", trueStatement);
            JsonObject statement = (JsonObject)list.ValueList[0];
            JsonObject rigthOperand = GetValue("right-operand", statement);
            JsonObject arguments = GetValue("arguments", rigthOperand);
            JsonElement methodName = GetElement("identifier-name", arguments);
            JsonObject typeInfo = GetValue("type-info", arguments);
            JsonObject memberRef = GetValue("type-info", typeInfo);
            JsonElement memName = GetElement("member-name", memberRef);
            
            TypeDeclaration typeDeclare;
            JsonObject lambdaExpression = new JsonObject();
            if(lambdaClass.TryGetValue(memName.ElementValue, out typeDeclare))
            {
                lambdaExpression.Comment = "CreateLamda";
                lambdaExpression.AddJsonValues("expression-type", new JsonElement("lambda-expression"));
                foreach(var member in typeDeclare.Members)
                {
                    if(member is MethodDeclaration)
                    {
                        MethodDeclaration method = (MethodDeclaration)member;
                        if (method.Name == methodName.ElementValue)
                        {
                            lambdaExpression.AddJsonValues("parameters", GetCommaSeparatedList(method.Parameters));
                            method.Body.AcceptVisitor(this);
                            lambdaExpression.AddJsonValues("body", Pop());
                        }
                    }
                }
                Push(lambdaExpression);
            }
        }

        JsonObject GetValue(string key, JsonObject obj)
        {
            JsonValue value;
            if(obj.Values.TryGetValue(key, out value))
            {
                if(value is JsonObject)
                {
                    JsonObject result = (JsonObject)value;
                    return result;
                }
            }
            return null;
        }

        JsonArray GetArray(string key, JsonObject obj)
        {
            JsonValue value;
            if (obj.Values.TryGetValue(key, out value))
            {
                if (value is JsonArray)
                {
                    JsonArray result = (JsonArray)value;
                    return result;
                }
            }
            return null;
        }

        JsonElement GetElement(string key, JsonObject obj)
        {
            JsonValue value;
            if (obj.Values.TryGetValue(key, out value))
            {
                if(value is JsonElement)
                {
                    JsonElement element = (JsonElement)value;
                    return element;
                }
            }
            return null;
        }

        public void VisitLabelStatement(LabelStatement labelStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitLabelStatement";
            statement.AddJsonValues("statement-type", new JsonElement("label-statement"));
            statement.AddJsonValues("identifier", GetIdentifier(labelStatement.GetChildByRole(Roles.Identifier)));

            Push(statement);
        }

        public void VisitLockStatement(LockStatement lockStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitLockStatement";
            statement.AddJsonValues("statement-type", new JsonElement("lock-statement"));
            statement.AddJsonValues("keyword", GetKeyword(LockStatement.LockKeywordRole));
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
            statement.AddJsonValues("keyword", GetKeyword(ReturnStatement.ReturnKeywordRole));
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
        }

        public void VisitSwitchStatement(SwitchStatement switchStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitSwitchStatement";
            statement.AddJsonValues("statement-type", new JsonElement("switch-statement"));
            statement.AddJsonValues("keyword", GetKeyword(SwitchStatement.SwitchKeywordRole));
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
        }

        public void VisitCaseLabel(CaseLabel caseLabel)
        {
            JsonObject label = new JsonObject();
            label.Comment = "VisitCaseLabel";
            if (caseLabel.Expression.IsNull)
            {
                label.AddJsonValues("keyword", GetKeyword(CaseLabel.DefaultKeywordRole));
            }
            else
            {
                label.AddJsonValues("keyword", GetKeyword(CaseLabel.CaseKeywordRole));
                caseLabel.Expression.AcceptVisitor(this);
                label.AddJsonValues("expression", Pop());
            }

            Push(label);
        }

        public void VisitThrowStatement(ThrowStatement throwStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitThrowStatement";
            statement.AddJsonValues("statement-type", new JsonElement("throw-statement"));
            statement.AddJsonValues("keyword", GetKeyword(ThrowStatement.ThrowKeywordRole));
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
        }

        public void VisitTryCatchStatement(TryCatchStatement tryCatchStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitTryCatchStatement";
            statement.AddJsonValues("statement-type", new JsonElement("try-catch-statement"));
            statement.AddJsonValues("try-keyword", GetKeyword(TryCatchStatement.TryKeywordRole));
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
                statement.AddJsonValues("final-keyword", GetKeyword(TryCatchStatement.FinallyKeywordRole));
                tryCatchStatement.FinallyBlock.AcceptVisitor(this);
                statement.AddJsonValues("final-block", Pop());
            }
            Push(statement);
        }

        public void VisitCatchClause(CatchClause catchClause)
        {
            JsonObject visitCatch = new JsonObject();
            visitCatch.Comment = "VisitCatchClause";
            visitCatch.AddJsonValues("catch-keyword", GetKeyword(CatchClause.CatchKeywordRole));
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
        }

        public void VisitUncheckedStatement(UncheckedStatement uncheckedStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitUncheckedStatement";
            statement.AddJsonValues("statement-type", new JsonElement("unchecked-statement"));
            statement.AddJsonValues("keyword", GetKeyword(UncheckedStatement.UncheckedKeywordRole));
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
            statement.AddJsonValues("keyword", GetKeyword(UnsafeStatement.UnsafeKeywordRole));
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
            statement.AddJsonValues("keyword", GetKeyword(UsingStatement.UsingKeywordRole));
            usingStatement.ResourceAcquisition.AcceptVisitor(this);
            statement.AddJsonValues("resource-acquisition", Pop());
            statement.AddJsonValues("embeded-statement", GetEmbeddedStatement(usingStatement.EmbeddedStatement));

            Push(statement);
            //implement already, but not tested
            //throw new Exception("first time testing");
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
            statement.AddJsonValues("yield-keyword", GetKeyword(YieldBreakStatement.YieldKeywordRole));
            statement.AddJsonValues("break-keyword", GetKeyword(YieldBreakStatement.BreakKeywordRole));
            Push(statement);
        }

        public void VisitYieldReturnStatement(YieldReturnStatement yieldReturnStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitYieldReturnStatement";
            statement.AddJsonValues("statement-type", new JsonElement("yield-return-statement"));
            statement.AddJsonValues("yield-keyword", GetKeyword(YieldReturnStatement.YieldKeywordRole));
            statement.AddJsonValues("return-keyword", GetKeyword(YieldReturnStatement.ReturnKeywordRole));
            yieldReturnStatement.Expression.AcceptVisitor(this);
            statement.AddJsonValues("expression", Pop());

            Push(statement);
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
            //throw new Exception("first time testing");
        }
        
        public void VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration)
        {
            ClearTypeInfo();
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
        }

        public void VisitConstructorInitializer(ConstructorInitializer constructorInitializer)
        {
            JsonObject initializer = new JsonObject();
            initializer.Comment = "VisitConstructorInitializer";
            if(constructorInitializer.ConstructorInitializerType == ConstructorInitializerType.This)
            {
                initializer.AddJsonValues("keyword", GetKeyword(ConstructorInitializer.ThisKeywordRole));
            }
            else
            {
                initializer.AddJsonValues("keyword", GetKeyword(ConstructorInitializer.BaseKeywordRole));
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
            declaration.AddJsonValues("tilde-role", GetKeyword(DestructorDeclaration.TildeRole));
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
                declaration.AddJsonValues("assign-role", GetKeyword(Roles.Assign));
                enumMemberDeclaration.Initializer.AcceptVisitor(this);
                declaration.AddJsonValues("initializer", Pop());
            }

            Push(declaration);
        }

        public void VisitEventDeclaration(EventDeclaration eventDeclaration)
        {
            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitEventDeclaration";
            declaration.AddJsonValues("attributes", GetAttributes(eventDeclaration.Attributes));
            declaration.AddJsonValues("modifier", GetModifiers(eventDeclaration.ModifierTokens));
            declaration.AddJsonValues("keyword", GetKeyword(EventDeclaration.EventKeywordRole));
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
            declaration.AddJsonValues("keyword", GetKeyword(EventDeclaration.EventKeywordRole));
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
            declaration.AddJsonValues("keyword", GetKeyword(FixedFieldDeclaration.FixedKeywordRole));
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
            declaration.AddJsonValues("keyword", GetKeyword(IndexerDeclaration.ThisKeywordRole));
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
        
        public void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
        {
            ClearTypeInfo();
            JsonObject method = new JsonObject();
            method.Comment = "VisitMethodDeclaration";
            method.AddJsonValues("name", new JsonElement(methodDeclaration.Name));
            //write modifier
            method.AddJsonValues("modifier", GetModifiers(methodDeclaration.ModifierTokens));
            //write return type
            methodDeclaration.ReturnType.AcceptVisitor(this);
            method.AddJsonValues("return-type", Pop());
            //write parameters
            //ParameterDeclaration param = new ParameterDeclaration("test", ParameterModifier.None);
            //PrimitiveType type = new PrimitiveType("string");
            //param.Type = type;
            //methodDeclaration.AddChild(param, Roles.Parameter);
            method.AddJsonValues("parameters", GetCommaSeparatedList(methodDeclaration.Parameters));
            //write body
            method.AddJsonValues("body", GetMethodBody(methodDeclaration.Body));
            //write method type info
            method.AddJsonValues("type-info-list", GetMethodTypeInfo(TypeInfoKeys()));
            
            Push(method);
        }

        public void VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration)
        {
            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitOperatorDeclaration";
            declaration.AddJsonValues("attributes", GetAttributes(operatorDeclaration.Attributes));
            declaration.AddJsonValues("modifier", GetModifiers(operatorDeclaration.ModifierTokens));
            if (operatorDeclaration.OperatorType == OperatorType.Explicit)
            {
                declaration.AddJsonValues("keyword", GetKeyword(OperatorDeclaration.ExplicitRole));
            }
            else if (operatorDeclaration.OperatorType == OperatorType.Implicit)
            {
                declaration.AddJsonValues("keyword", GetKeyword(OperatorDeclaration.ImplicitRole));
            }
            else
            {
                operatorDeclaration.ReturnType.AcceptVisitor(this);
                declaration.AddJsonValues("return-type", Pop());
            }
            declaration.AddJsonValues("operator-keyword", GetKeyword(OperatorDeclaration.OperatorKeywordRole));
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
            parameter.AddJsonValues("attributes", GetAttributes(parameterDeclaration.Attributes));
            JsonValue keyword;
            switch (parameterDeclaration.ParameterModifier)
            {
                case ParameterModifier.Out:
                    keyword = GetKeyword(ParameterDeclaration.OutModifierRole);
                    break;
                case ParameterModifier.Params:
                    keyword = GetKeyword(ParameterDeclaration.ParamsModifierRole);
                    break;
                case ParameterModifier.Ref:
                    keyword = GetKeyword(ParameterDeclaration.RefModifierRole);
                    break;
                case ParameterModifier.This:
                    keyword = GetKeyword(ParameterDeclaration.ThisModifierRole);
                    break;
                default:
                    keyword = null;
                    break;
            }
            parameter.AddJsonValues("modifier", keyword);
            parameterDeclaration.Type.AcceptVisitor(this);
            parameter.AddJsonValues("type-info", Pop());
            parameter.AddJsonValues("name", new JsonElement(parameterDeclaration.Name));
            if (parameterDeclaration.DefaultExpression.IsNull)
            {
                parameterDeclaration.DefaultExpression.AcceptVisitor(this);
                parameter.AddJsonValues("default-expression", Pop());
            }
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
            //throw new Exception("first time testing");
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
            int index = GetTypeIndex(typeKeyword);
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
            JsonObject visit = new JsonObject();
            visit.Comment = "VisitComment";
            switch (comment.CommentType)
            {
                case CommentType.Documentation:
                    visit.AddJsonValues("comment-type", new JsonElement("documentation"));
                    break;
                case CommentType.InactiveCode:
                    visit.AddJsonValues("comment-type", new JsonElement("inactive-code"));
                    break;
                case CommentType.MultiLine:
                    visit.AddJsonValues("comment-type", new JsonElement("multiline"));
                    break;
                case CommentType.MultiLineDocumentation:
                    visit.AddJsonValues("comment-type", new JsonElement("multiline-documentation"));
                    break;
                case CommentType.SingleLine:
                    visit.AddJsonValues("comment-type", new JsonElement("single-line"));
                    break;
                default:
                    throw new NotSupportedException("Invalid value for CommentType");
            }
            visit.AddJsonValues("comment-content", new JsonElement(comment.Content));

            Push(visit);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitNewLine(NewLineNode newLineNode)
        {
            //unused
            throw new Exception("NewLineNode Unused");
        }

        public void VisitWhitespace(WhitespaceNode whitespaceNode)
        {
            //unused
            throw new Exception("WhitespaceNode Unused");
        }

        public void VisitText(TextNode textNode)
        {
            //unused
            throw new Exception("TextNode Unused");
        }

        public void VisitPreProcessorDirective(PreProcessorDirective preProcessorDirective)
        {
            JsonObject visit = new JsonObject();
            visit.Comment = "VisitPreProcessorDirective";
            var type = preProcessorDirective.Type;
            string typeStr = "#" + type.ToString().ToLowerInvariant();
            visit.AddJsonValues("preprocessordirective-type", new JsonElement(typeStr));
            if (!string.IsNullOrEmpty(preProcessorDirective.Argument))
            {
                visit.AddJsonValues("argument", new JsonElement(preProcessorDirective.Argument));
            }
            else
            {
                visit.AddJsonValues("argument", null);
            }

            Push(visit);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitTypeParameterDeclaration(TypeParameterDeclaration typeParameterDeclaration)
        {
            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitTypeParameterDeclaration";
            declaration.AddJsonValues("attributes", GetAttributes(typeParameterDeclaration.Attributes));
            switch (typeParameterDeclaration.Variance)
            {
                case VarianceModifier.Invariant:
                    break;
                case VarianceModifier.Covariant:
                    declaration.AddJsonValues("keyword", GetKeyword(TypeParameterDeclaration.OutVarianceKeywordRole));
                    break;
                case VarianceModifier.Contravariant:
                    declaration.AddJsonValues("keyword", GetKeyword(TypeParameterDeclaration.InVarianceKeywordRole));
                    break;
                default:
                    throw new NotSupportedException("Invalid value for VarianceModifier");
            }
            declaration.AddJsonValues("identifier", GetIdentifier(typeParameterDeclaration.NameToken));

            Push(declaration);
            //implement already, but not tested
            //throw new Exception("first time testing");
        }

        public void VisitConstraint(Constraint constraint)
        {
            JsonObject visit = new JsonObject();
            visit.Comment = "VisitConstraint";
            visit.AddJsonValues("keyword", GetKeyword(Roles.WhereKeyword));
            constraint.TypeParameter.AcceptVisitor(this);
            visit.AddJsonValues("type-parameter", Pop());
            visit.AddJsonValues("base-types", GetCommaSeparatedList(constraint.BaseTypes));

            Push(visit);
            //implement already, but not tested
            throw new Exception("first time testing");
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
            Push(GetIdentifier(identifier));
        }

        void IAstVisitor.VisitErrorNode(AstNode errorNode)
        {
            throw new Exception("Error");
        }

        void IAstVisitor.VisitNullNode(AstNode nullNode)
        {
            Push(null);
        }

        #endregion

        #region Pattern Nodes

        public void VisitPatternPlaceholder(AstNode placeholder, Pattern pattern)
        {
            throw new Exception("VisitPatternPlaceholder");
        }

        #endregion

        #region Documentation Reference
        public void VisitDocumentationReference(DocumentationReference documentationReference)
        {
            throw new Exception("VisitDocumentationReference");
        }
        #endregion

    }
}