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
    public partial class AstCSharpToJsonVisitor : IAstVisitor
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

        JsonValue GetTypeInfoList(List<string> typeInfoList)
        {
            JsonArray typeArr = new JsonArray();
            typeArr.Comment = "GetTypeInfoList";
            foreach (string value in typeInfoList)
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
                embedded.AddJsonValue("block-statement", Pop());
            }
            else
            {
                embeddedStatement.AcceptVisitor(this);
                embedded.AddJsonValue("statement", Pop());
            }
            return embedded;
        }

        JsonValue GetAttributes(IEnumerable<AttributeSection> attributes)
        {
            JsonArray attrArray = new JsonArray();
            foreach (AttributeSection attr in attributes)
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
            expression.AddJsonValue("expression-type", new JsonElement("anonymous-method-expression"));
            expression.AddJsonValue("keyword", GetKeyword(AnonymousMethodExpression.AsyncModifierRole));
            expression.AddJsonValue("delegate-keyword", GetKeyword(AnonymousMethodExpression.DelegateKeywordRole));
            if (anonymousMethodExpression.HasParameterList)
            {
                expression.AddJsonValue("arguments", GetCommaSeparatedList(anonymousMethodExpression.Parameters));
            }
            anonymousMethodExpression.Body.AcceptVisitor(this);
            expression.AddJsonValue("body", Pop());
            Push(expression);
        }

        public void VisitUndocumentedExpression(UndocumentedExpression undocumentedExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitUndocumentedExpression";
            expression.AddJsonValue("expression-type", new JsonElement("undocumented-expression"));
            switch (undocumentedExpression.UndocumentedExpressionType)
            {
                case UndocumentedExpressionType.ArgList:
                case UndocumentedExpressionType.ArgListAccess:
                    GetTypeInfo(UndocumentedExpression.ArglistKeywordRole.Token);
                    expression.AddJsonValue("type-info", GetTypeInfo(UndocumentedExpression.ArglistKeywordRole.Token));
                    break;
                case UndocumentedExpressionType.MakeRef:
                    expression.AddJsonValue("type-info", GetTypeInfo(UndocumentedExpression.MakerefKeywordRole.Token));
                    break;
                case UndocumentedExpressionType.RefType:
                    expression.AddJsonValue("type-info", GetTypeInfo(UndocumentedExpression.ReftypeKeywordRole.Token));
                    break;
                case UndocumentedExpressionType.RefValue:
                    expression.AddJsonValue("type-info", GetTypeInfo(UndocumentedExpression.RefvalueKeywordRole.Token));
                    break;
                default:
                    throw new Exception("unknowed type");
            }
            if (undocumentedExpression.Arguments.Count > 0)
            {
                expression.AddJsonValue("arguments", GetCommaSeparatedList(undocumentedExpression.Arguments));
            }
            else
            {
                expression.AddJsonNull("arguments");
            }
            Push(expression);
            //throw new Exception("first time testing");//implement already, but not tested
        }

        public void VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitArrayCreateExpression";
            expression.AddJsonValue("expression-type", new JsonElement("array-create-expression"));
            expression.AddJsonValue("keyword", GetKeyword(ArrayCreateExpression.NewKeywordRole));
            arrayCreateExpression.Type.AcceptVisitor(this);
            expression.AddJsonValue("array-type", Pop());
            if (arrayCreateExpression.Arguments.Count > 0)
            {
                expression.AddJsonValue("arguments", GetCommaSeparatedList(arrayCreateExpression.Arguments));
            }
            JsonArray specifierArr = new JsonArray();
            foreach (var specifier in arrayCreateExpression.AdditionalArraySpecifiers)
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
            expression.AddJsonValue("specifier", specifierArr);
            arrayCreateExpression.Initializer.AcceptVisitor(this);
            expression.AddJsonValue("initializer", Pop());
            Push(expression);
        }

        public void VisitArrayInitializerExpression(ArrayInitializerExpression arrayInitializerExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitArrayInitializerExpression";
            expression.AddJsonValue("expression-type", new JsonElement("array-initializer-expression"));
            bool bracesAreOptional = arrayInitializerExpression.Elements.Count == 1
                && IsObjectOrCollectionInitializer(arrayInitializerExpression.Parent)
                && !CanBeConfusedWithObjectInitializer(arrayInitializerExpression.Elements.Single());
            if (bracesAreOptional && arrayInitializerExpression.LBraceToken.IsNull)
            {
                arrayInitializerExpression.Elements.Single().AcceptVisitor(this);
                expression.AddJsonValue("elements", Pop());
            }
            else
            {
                var json = GetInitializerElements(arrayInitializerExpression.Elements);
                expression.AddJsonValue("elements", json);
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
            expression.AddJsonValue("expression-type", new JsonElement("as-expression"));
            expression.AddJsonValue("keyword", GetKeyword(AsExpression.AsKeywordRole));
            asExpression.Type.AcceptVisitor(this);
            expression.AddJsonValue("type-info", Pop());

            Push(expression);
        }

        public void VisitAssignmentExpression(AssignmentExpression assignmentExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitAssignmentExpression";
            expression.AddJsonValue("expression-type", new JsonElement("assignment-expression"));
            assignmentExpression.Left.AcceptVisitor(this);
            expression.AddJsonValue("left-operand", Pop());
            TokenRole operatorRole = AssignmentExpression.GetOperatorRole(assignmentExpression.Operator);
            expression.AddJsonValue("operator", GetKeyword(operatorRole));
            assignmentExpression.Right.AcceptVisitor(this);
            expression.AddJsonValue("right-operand", Pop());
            Push(expression);
        }

        public void VisitBaseReferenceExpression(BaseReferenceExpression baseReferenceExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitBaseReferenceExpression";
            expression.AddJsonValue("expression-type", new JsonElement("base-reference-expression"));
            expression.AddJsonValue("keyword", new JsonElement("base"));

            Push(expression);
            //throw new Exception("first time testing");//implement already, but not tested
        }

        public void VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression)
        {
            JsonObject expression = CreateJsonExpression(binaryOperatorExpression);
            expression.Comment = "VisitBinaryOperatorExpression";
            expression.AddJsonValue("expression-type", new JsonElement("binary-operator-expression"));
            binaryOperatorExpression.Left.AcceptVisitor(this);
            expression.AddJsonValue("left-operand", Pop());
            string opt = BinaryOperatorExpression.GetOperatorRole(binaryOperatorExpression.Operator).Token;
            expression.AddJsonValue("operator", new JsonElement(opt));
            binaryOperatorExpression.Right.AcceptVisitor(this);
            expression.AddJsonValue("right-operand", Pop());


            Push(expression);
        }
        public void VisitCastExpression(CastExpression castExpression)
        {
            JsonObject expression = CreateJsonExpression(castExpression);

            expression.AddJsonValue("expression-type", new JsonElement("cast-expression"));
            castExpression.Type.AcceptVisitor(this);
            expression.AddJsonValue("type-info", Pop());
            castExpression.Expression.AcceptVisitor(this);
            expression.AddJsonValue("expression", Pop());

            Push(expression);

        }
        public void VisitCheckedExpression(CheckedExpression checkedExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitCheckedExpression";
            expression.AddJsonValue("expression-type", new JsonElement("checked-expression"));
            expression.AddJsonValue("keyword", GetKeyword(CheckedExpression.CheckedKeywordRole));
            checkedExpression.Expression.AcceptVisitor(this);
            expression.AddJsonValue("expression", Pop());

            Push(expression);
            throw new Exception("first time testing");//implement already, but not tested
        }

        public void VisitConditionalExpression(ConditionalExpression conditionalExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitConditionalExpression";
            expression.AddJsonValue("expression-type", new JsonElement("conditional-expression"));
            conditionalExpression.Condition.AcceptVisitor(this);
            expression.AddJsonValue("condition", Pop());
            conditionalExpression.TrueExpression.AcceptVisitor(this);
            expression.AddJsonValue("true-expression", Pop());
            conditionalExpression.FalseExpression.AcceptVisitor(this);
            expression.AddJsonValue("false-expression", Pop());

            Push(expression);
            //throw new Exception("first time testing");//implement already, but not tested
        }

        public void VisitDefaultValueExpression(DefaultValueExpression defaultValueExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitDefaultValueExpression";
            expression.AddJsonValue("expression-type", new JsonElement("default-value-expression"));
            expression.AddJsonValue("keyword", GetKeyword(DefaultValueExpression.DefaultKeywordRole));
            defaultValueExpression.Type.AcceptVisitor(this);
            expression.AddJsonValue("type-info", Pop());

            Push(expression);
            //throw new Exception("first time testing");//implement already, but not tested
        }

        public void VisitDirectionExpression(DirectionExpression directionExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitDirectionExpression";
            expression.AddJsonValue("expression-type", new JsonElement("direction-expression"));
            switch (directionExpression.FieldDirection)
            {
                case FieldDirection.Out:
                    expression.AddJsonValue("keyword", GetKeyword(DirectionExpression.OutKeywordRole));
                    break;
                case FieldDirection.Ref:
                    expression.AddJsonValue("keyword", GetKeyword(DirectionExpression.RefKeywordRole));
                    break;
                default:
                    throw new NotSupportedException("Invalid value for FieldDirection");
            }
            directionExpression.Expression.AcceptVisitor(this);
            expression.AddJsonValue("expression", Pop());

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
            expression.AddJsonValue("expression-type", new JsonElement("indexer-expression"));
            indexerExpression.Target.AcceptVisitor(this);
            expression.AddJsonValue("target", Pop());
            expression.AddJsonValue("arguments", GetCommaSeparatedList(indexerExpression.Arguments));
            Push(expression);
        }

        public void VisitInvocationExpression(InvocationExpression invocationExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitInvocationExpression";
            expression.AddJsonValue("expression-type", new JsonElement("invocation"));
            invocationExpression.Target.AcceptVisitor(this);
            expression.AddJsonValue("target", Pop());
            expression.AddJsonValue("arguments", GetCommaSeparatedList(invocationExpression.Arguments));
            Push(expression);
        }

        public void VisitIsExpression(IsExpression isExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitIsExpression";
            expression.AddJsonValue("expression-type", new JsonElement("is-expression"));
            expression.AddJsonValue("keyword", GetKeyword(IsExpression.IsKeywordRole));
            isExpression.Type.AcceptVisitor(this);
            expression.AddJsonValue("type-info", Pop());
            isExpression.Expression.AcceptVisitor(this);
            expression.AddJsonValue("expression", Pop());

            Push(expression);
        }

        public void VisitLambdaExpression(LambdaExpression lambdaExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitLambdaExpression";
            expression.AddJsonValue("expression-type", new JsonElement("lambda-expression"));
            if (lambdaExpression.IsAsync)
            {
                expression.AddJsonValue("async-keyword", GetKeyword(LambdaExpression.AsyncModifierRole));
            }
            if (LambdaNeedsParenthesis(lambdaExpression))
            {
                expression.AddJsonValue("parameters", GetCommaSeparatedList(lambdaExpression.Parameters));
            }
            else
            {
                lambdaExpression.Parameters.Single().AcceptVisitor(this);
                expression.AddJsonValue("parameters", Pop());
            }
            lambdaExpression.Body.AcceptVisitor(this);
            expression.AddJsonValue("body", Pop());

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
            expression.AddJsonValue("expression-type", new JsonElement("member-reference"));
            memberReferenceExpression.Target.AcceptVisitor(this);
            expression.AddJsonValue("target", Pop());
            expression.AddJsonValue("identifier-name", GetIdentifier(memberReferenceExpression.MemberNameToken));

            Push(expression);
        }

        public void VisitNamedArgumentExpression(NamedArgumentExpression namedArgumentExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitNamedArgumentExpression";
            expression.AddJsonValue("expression-type", new JsonElement("named-argument-expression"));
            expression.AddJsonValue("identifier", GetIdentifier(namedArgumentExpression.NameToken));
            namedArgumentExpression.Expression.AcceptVisitor(this);
            expression.AddJsonValue("expression", Pop());

            Push(expression);
            throw new Exception("first time testing");//implement already, but not tested
        }

        public void VisitNamedExpression(NamedExpression namedExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitNamedExpression";
            expression.AddJsonValue("expression-type", new JsonElement("named-expression"));
            expression.AddJsonValue("identifier", GetIdentifier(namedExpression.NameToken));
            namedExpression.Expression.AcceptVisitor(this);
            expression.AddJsonValue("expression", Pop());

            Push(expression);
            //throw new Exception("first time testing");//implement already, but not tested
        }

        public void VisitNullReferenceExpression(NullReferenceExpression nullReferenceExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitNullReferenceExpression";
            expression.AddJsonValue("expression-type", new JsonElement("null-reference"));
            expression.AddJsonValue("keyword", new JsonElement("null"));
            Push(expression);
        }

        public void VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitObjectCreateExpression";
            expression.AddJsonValue("expression-type", new JsonElement("ObjectCreate"));
            expression.AddJsonValue("keyword", GetKeyword(ObjectCreateExpression.NewKeywordRole));
            objectCreateExpression.Type.AcceptVisitor(this);
            expression.AddJsonValue("type-info", Pop());
            bool useParenthesis = objectCreateExpression.Arguments.Any() || objectCreateExpression.Initializer.IsNull;
            if (!objectCreateExpression.LParToken.IsNull)
            {
                useParenthesis = true;
            }
            if (useParenthesis)
            {
                expression.AddJsonValue("arguments", GetCommaSeparatedList(objectCreateExpression.Arguments));
            }
            objectCreateExpression.Initializer.AcceptVisitor(this);
            expression.AddJsonValue("initializer", Pop());
            Push(expression);
        }

        public void VisitAnonymousTypeCreateExpression(AnonymousTypeCreateExpression anonymousTypeCreateExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitAnonymousTypeCreateExpression";
            expression.AddJsonValue("expression-type", new JsonElement("anonymous-type-create-expression"));
            expression.AddJsonValue("keyword", GetKeyword(AnonymousTypeCreateExpression.NewKeywordRole));
            expression.AddJsonValue("elements", GetInitializerElements(anonymousTypeCreateExpression.Initializers));
            Push(expression);
            //throw new Exception("first time testing");//implement already, but not tested
        }

        public void VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitParenthesizedExpression";
            expression.AddJsonValue("expression-type", new JsonElement("parenthesized-expression"));
            parenthesizedExpression.Expression.AcceptVisitor(this);
            expression.AddJsonValue("expression", Pop());

            Push(expression);
            //throw new Exception("first time testing");//implement already, but not tested
        }

        public void VisitPointerReferenceExpression(PointerReferenceExpression pointerReferenceExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitPointerReferenceExpression";
            expression.AddJsonValue("expression-type", new JsonElement("pointer-reference-expression"));
            pointerReferenceExpression.Target.AcceptVisitor(this);
            expression.AddJsonValue("target", Pop());
            expression.AddJsonValue("identifier", GetIdentifier(pointerReferenceExpression.MemberNameToken));
            expression.AddJsonValue("type-arguments", GetTypeArguments(pointerReferenceExpression.TypeArguments));

            Push(expression);

            throw new Exception("first time testing");//implement already, but not tested
        }

        #region VisitPrimitiveExpression
        public void VisitPrimitiveExpression(PrimitiveExpression primitiveExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitPrimitiveExpression";
            expression.AddJsonValue("expression-type", new JsonElement("primitive-expression"));
            expression.AddJsonValue("value", new JsonElement(primitiveExpression.Value.ToString()));
            expression.AddJsonValue("unsafe-literal-value", new JsonElement(primitiveExpression.UnsafeLiteralValue));
            Push(expression);
        }
        #endregion

        public void VisitSizeOfExpression(SizeOfExpression sizeOfExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitSizeOfExpression";
            expression.AddJsonValue("expression-type", new JsonElement("sizeof-expression"));
            expression.AddJsonValue("keyword", GetKeyword(SizeOfExpression.SizeofKeywordRole));
            sizeOfExpression.Type.AcceptVisitor(this);
            expression.AddJsonValue("type-info", Pop());

            Push(expression);
            throw new Exception("first time testing");//implement already, but not tested
        }

        public void VisitStackAllocExpression(StackAllocExpression stackAllocExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitStackAllocExpression";
            expression.AddJsonValue("expression-type", new JsonElement("stack-alloc-expression"));
            expression.AddJsonValue("keyword", GetKeyword(StackAllocExpression.StackallocKeywordRole));
            stackAllocExpression.Type.AcceptVisitor(this);
            expression.AddJsonValue("type-info", Pop());
            expression.AddJsonValue("count-expression", GetCommaSeparatedList(new[] { stackAllocExpression.CountExpression }));

            Push(expression);
            //throw new Exception("first time testing");//implement already, but not tested
        }

        public void VisitThisReferenceExpression(ThisReferenceExpression thisReferenceExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitThisReferenceExpression";
            expression.AddJsonValue("expression-type", new JsonElement("this-reference-expression"));
            expression.AddJsonValue("keyword", new JsonElement("this"));

            AddTypeInformation(expression, thisReferenceExpression);

            Push(expression);
        }

        public void VisitTypeOfExpression(TypeOfExpression typeOfExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitTypeOfExpression";
            expression.AddJsonValue("expression-type", new JsonElement("typeof-expression"));
            expression.AddJsonValue("keyword", GetKeyword(TypeOfExpression.TypeofKeywordRole));
            typeOfExpression.Type.AcceptVisitor(this);
            expression.AddJsonValue("type-info", Pop());

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
            expression.AddJsonValue("expression-type", new JsonElement("unary-operator-expression"));
            UnaryOperatorType opType = unaryOperatorExpression.Operator;
            var opSymbol = UnaryOperatorExpression.GetOperatorRole(opType);
            if (opType == UnaryOperatorType.Await)
            {
                expression.AddJsonValue("symbol", GetKeyword(opSymbol));
            }
            else if (!(opType == UnaryOperatorType.PostIncrement || opType == UnaryOperatorType.PostDecrement))
            {
                expression.AddJsonValue("symbol", GetKeyword(opSymbol));
            }
            unaryOperatorExpression.Expression.AcceptVisitor(this);
            expression.AddJsonValue("expression", Pop());
            if (opType == UnaryOperatorType.PostIncrement || opType == UnaryOperatorType.PostDecrement)
            {
                expression.AddJsonValue("symbol", GetKeyword(opSymbol));
            }
            Push(expression);
        }

        public void VisitUncheckedExpression(UncheckedExpression uncheckedExpression)
        {
            JsonObject expression = new JsonObject();
            expression.Comment = "VisitUncheckedExpression";
            expression.AddJsonValue("expression-type", new JsonElement("unchecked-expression"));
            expression.AddJsonValue("keyword", GetKeyword(UncheckedExpression.UncheckedKeywordRole));
            uncheckedExpression.Expression.AcceptVisitor(this);
            expression.AddJsonValue("expression", Pop());

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
            visit.AddJsonValue("type", Pop());
            if (attribute.Arguments.Count != 0 || !attribute.GetChildByRole(Roles.LPar).IsNull)
            {
                visit.AddJsonValue("arguments", GetCommaSeparatedList(attribute.Arguments));
            }

            Push(visit);
        }

        public void VisitAttributeSection(AttributeSection attributeSection)
        {
            JsonObject visit = new JsonObject();
            visit.Comment = "VisitAttributeSection";
            if (!string.IsNullOrEmpty(attributeSection.AttributeTarget))
            {
                visit.AddJsonValue("keyword", new JsonElement(attributeSection.AttributeTarget));
            }
            visit.AddJsonValue("attributes", GetCommaSeparatedList(attributeSection.Attributes));

            Push(visit);
        }

        public void VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration)
        {
            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitDelegateDeclaration";

            declaration.AddJsonValue("attributes", GetAttributes(delegateDeclaration.Attributes));
            declaration.AddJsonValue("modifier", GetModifiers(delegateDeclaration.ModifierTokens));
            declaration.AddJsonValue("keyword", GetKeyword(Roles.DelegateKeyword));
            delegateDeclaration.ReturnType.AcceptVisitor(this);
            declaration.AddJsonValue("return-type", Pop());
            declaration.AddJsonValue("identifier", GetIdentifier(delegateDeclaration.NameToken));
            declaration.AddJsonValue("type-parameters", GetTypeParameters(delegateDeclaration.TypeParameters));
            declaration.AddJsonValue("parameters", GetCommaSeparatedList(delegateDeclaration.Parameters));

            JsonArray contraintList = new JsonArray();
            foreach (Constraint constraint in delegateDeclaration.Constraints)
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
            declaration.AddJsonValue("constraint", contraintList);

            Push(declaration);
        }

        public void VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration)
        {
            ClearTypeInfo();
            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitNamespaceDeclaration";
            declaration.AddJsonValue("keyword", GetKeyword(Roles.NamespaceKeyword));
            namespaceDeclaration.NamespaceName.AcceptVisitor(this);
            declaration.AddJsonValue("namespace-name", Pop());
            declaration.AddJsonValue("namespace-info-list", GetTypeInfoList(TypeInfoKeys()));
            JsonArray memberList = new JsonArray();
            foreach (var member in namespaceDeclaration.Members)
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
            declaration.AddJsonValue("members", memberList);

            Push(declaration);
        }

        Dictionary<string, TypeDeclaration> lambdaClass = new Dictionary<string, TypeDeclaration>();
        public void VisitTypeDeclaration(TypeDeclaration typeDeclaration)
        {
            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitTypeDeclaration";
            declaration.AddJsonValue("attributes", GetAttributes(typeDeclaration.Attributes));
            declaration.AddJsonValue("modifiers", GetModifiers(typeDeclaration.ModifierTokens));
            switch (typeDeclaration.ClassType)
            {
                case ClassType.Enum:
                    declaration.AddJsonValue("keyword", GetKeyword(Roles.EnumKeyword));
                    break;
                case ClassType.Interface:
                    declaration.AddJsonValue("keyword", GetKeyword(Roles.InterfaceKeyword));
                    break;
                case ClassType.Struct:
                    declaration.AddJsonValue("keyword", GetKeyword(Roles.StructKeyword));
                    break;
                default:
                    declaration.AddJsonValue("keyword", GetKeyword(Roles.ClassKeyword));
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
            declaration.AddJsonValue("identifier", identifier);
            declaration.AddJsonValue("parameters", GetTypeParameters(typeDeclaration.TypeParameters));
            if (typeDeclaration.BaseTypes.Any())
            {
                declaration.AddJsonValue("base-types", GetCommaSeparatedList(typeDeclaration.BaseTypes));
            }
            JsonArray constraintArr = new JsonArray();
            foreach (Constraint constraint in typeDeclaration.Constraints)
            {
                constraint.AcceptVisitor(this);
                constraintArr.AddJsonValue(Pop());
            }
            declaration.AddJsonValue("constraint", constraintArr);
            JsonArray memberArr = new JsonArray();
            foreach (var member in typeDeclaration.Members)
            {
                member.AcceptVisitor(this);
                memberArr.AddJsonValue(Pop());
            }
            declaration.AddJsonValue("members", memberArr);
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
            declaration.AddJsonValue("keyword", GetKeyword(UsingAliasDeclaration.UsingKeywordRole));
            declaration.AddJsonValue("identifier", GetIdentifier(usingAliasDeclaration.GetChildByRole(UsingAliasDeclaration.AliasRole)));
            declaration.AddJsonValue("assign-token", GetKeyword(Roles.Assign));
            usingAliasDeclaration.Import.AcceptVisitor(this);
            declaration.AddJsonValue("import", Pop());

            Push(declaration);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitUsingDeclaration(UsingDeclaration usingDeclaration)
        {
            ClearTypeInfo();
            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitUsingDeclaration";
            declaration.AddJsonValue("keyword", GetKeyword(UsingDeclaration.UsingKeywordRole));
            usingDeclaration.Import.AcceptVisitor(this);
            declaration.AddJsonValue("import", Pop());
            declaration.AddJsonValue("import-info-list", GetTypeInfoList(TypeInfoKeys()));
            Push(declaration);
        }

        public void VisitExternAliasDeclaration(ExternAliasDeclaration externAliasDeclaration)
        {
            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitExternAliasDeclaration";

            declaration.AddJsonValue("extern-keyword", GetKeyword(Roles.ExternKeyword));
            declaration.AddJsonValue("alias-keyword", GetKeyword(Roles.AliasKeyword));
            declaration.AddJsonValue("identifier", GetIdentifier(externAliasDeclaration.NameToken));

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
            statement.AddJsonValue("statement-type", new JsonElement("block-statement"));
            int count = blockStatement.Statements.Count;
            if (count == 0)
            {
                Push(null);
                return;
            }
            JsonArray stmtList = new JsonArray();
            foreach (var node in blockStatement.Statements)
            {
                node.AcceptVisitor(this);
                stmtList.AddJsonValue(Pop());
            }
            statement.AddJsonValue("statement-list", stmtList);
            Push(statement);
        }

        public void VisitBreakStatement(BreakStatement breakStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitBreakStatement";
            statement.AddJsonValue("statement-type", new JsonElement("break-statement"));
            statement.AddJsonValue("keyword", new JsonElement("break"));

            Push(statement);
        }

        public void VisitCheckedStatement(CheckedStatement checkedStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitCheckedStatement";
            statement.AddJsonValue("statement-type", new JsonElement("checked-statement"));
            statement.AddJsonValue("keyword", GetKeyword(CheckedStatement.CheckedKeywordRole));
            checkedStatement.Body.AcceptVisitor(this);
            statement.AddJsonValue("body", Pop());

            Push(statement);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitContinueStatement(ContinueStatement continueStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitContinueStatement";
            statement.AddJsonValue("statement-type", new JsonElement("continue-statement"));
            statement.AddJsonValue("keyword", GetKeyword(ContinueStatement.ContinueKeywordRole));

            Push(statement);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitDoWhileStatement(DoWhileStatement doWhileStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitDoWhileStatement";
            statement.AddJsonValue("statement-type", new JsonElement("do-while-statement"));
            doWhileStatement.Condition.AcceptVisitor(this);
            statement.AddJsonValue("condition", Pop());
            statement.AddJsonValue("statement-list", GetEmbeddedStatement(doWhileStatement.EmbeddedStatement));
            Push(statement);
        }

        public void VisitEmptyStatement(EmptyStatement emptyStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitEmptyStatement";
            statement.AddJsonValue("statement-type", new JsonElement("empty-statement"));
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
            statement.AddJsonValue("statement-type", new JsonElement("fixed-statement"));
            statement.AddJsonValue("keyword", GetKeyword(FixedStatement.FixedKeywordRole));
            fixedStatement.Type.AcceptVisitor(this);
            statement.AddJsonValue("type-info", Pop());
            statement.AddJsonValue("variables", GetCommaSeparatedList(fixedStatement.Variables));
            statement.AddJsonValue("embedded-statement", GetEmbeddedStatement(fixedStatement.EmbeddedStatement));

            Push(statement);
            //implement already, but not tested
            //throw new Exception("first time testing");
        }

        public void VisitForeachStatement(ForeachStatement foreachStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitForeachStatement";
            statement.AddJsonValue("statement-type", new JsonElement("ForEach"));
            statement.AddJsonValue("keyword", GetKeyword(ForeachStatement.ForeachKeywordRole));
            foreachStatement.VariableType.AcceptVisitor(this);
            statement.AddJsonValue("local-variable-type", Pop());
            statement.AddJsonValue("local-variable-name", GetIdentifier(foreachStatement.VariableNameToken));
            foreachStatement.InExpression.AcceptVisitor(this);
            statement.AddJsonValue("in-expression", Pop());
            statement.AddJsonValue("embedded-statement", GetEmbeddedStatement(foreachStatement.EmbeddedStatement));
            Push(statement);
        }

        public void VisitForStatement(ForStatement forStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitForStatement";
            statement.AddJsonValue("keyword", GetKeyword(ForStatement.ForKeywordRole));
            statement.AddJsonValue("initializer", GetCommaSeparatedList(forStatement.Initializers));
            forStatement.Condition.AcceptVisitor(this);
            statement.AddJsonValue("condition", Pop());
            if (forStatement.Iterators.Any())
            {
                statement.AddJsonValue("iterators", GetCommaSeparatedList(forStatement.Iterators));
            }
            statement.AddJsonValue("embedded-statement", GetEmbeddedStatement(forStatement.EmbeddedStatement));
            Push(statement);
        }

        public void VisitGotoCaseStatement(GotoCaseStatement gotoCaseStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitGotoCaseStatement";
            statement.AddJsonValue("statement-type", new JsonElement("goto-case-statement"));
            statement.AddJsonValue("goto-keyword", GetKeyword(GotoCaseStatement.GotoKeywordRole));
            statement.AddJsonValue("case-keyword", GetKeyword(GotoCaseStatement.CaseKeywordRole));
            gotoCaseStatement.LabelExpression.AcceptVisitor(this);
            statement.AddJsonValue("label-expression", Pop());
            Push(statement);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitGotoDefaultStatement(GotoDefaultStatement gotoDefaultStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitGotoDefaultStatement";
            statement.AddJsonValue("statement-type", new JsonElement("goto-default-statement"));
            statement.AddJsonValue("goto-keyword", GetKeyword(GotoDefaultStatement.GotoKeywordRole));
            statement.AddJsonValue("default-keyword", GetKeyword(GotoDefaultStatement.DefaultKeywordRole));
            Push(statement);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitGotoStatement(GotoStatement gotoStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitGotoStatement";
            statement.AddJsonValue("statement-type", new JsonElement("goto-statement"));
            statement.AddJsonValue("keyword", GetKeyword(GotoStatement.GotoKeywordRole));
            statement.AddJsonValue("identifier", GetIdentifier(gotoStatement.GetChildByRole(Roles.Identifier)));

            Push(statement);
        }
        bool test11;
        public void VisitIfElseStatement(IfElseStatement ifElseStatement)
        {
            test11 = true;
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitIfElseStatement";
            ifElseStatement.Condition.AcceptVisitor(this);
            statement.AddJsonValue("condition", Pop());
            ifElseStatement.TrueStatement.AcceptVisitor(this);
            statement.AddJsonValue("true-statement", Pop());
            ifElseStatement.FalseStatement.AcceptVisitor(this);
            statement.AddJsonValue("false-statement", Pop());
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
            if (lambdaClass.TryGetValue(memName.ElementValue, out typeDeclare))
            {
                lambdaExpression.Comment = "CreateLamda";
                lambdaExpression.AddJsonValue("expression-type", new JsonElement("lambda-expression"));
                foreach (var member in typeDeclare.Members)
                {
                    if (member is MethodDeclaration)
                    {
                        MethodDeclaration method = (MethodDeclaration)member;
                        if (method.Name == methodName.ElementValue)
                        {
                            lambdaExpression.AddJsonValue("parameters", GetCommaSeparatedList(method.Parameters));
                            method.Body.AcceptVisitor(this);
                            lambdaExpression.AddJsonValue("body", Pop());
                        }
                    }
                }
                Push(lambdaExpression);
            }
        }

        JsonObject GetValue(string key, JsonObject obj)
        {
            JsonValue value;
            if (obj.Values.TryGetValue(key, out value))
            {
                if (value is JsonObject)
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
                if (value is JsonElement)
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
            statement.AddJsonValue("statement-type", new JsonElement("label-statement"));
            statement.AddJsonValue("identifier", GetIdentifier(labelStatement.GetChildByRole(Roles.Identifier)));

            Push(statement);
        }

        public void VisitLockStatement(LockStatement lockStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitLockStatement";
            statement.AddJsonValue("statement-type", new JsonElement("lock-statement"));
            statement.AddJsonValue("keyword", GetKeyword(LockStatement.LockKeywordRole));
            lockStatement.Expression.AcceptVisitor(this);
            statement.AddJsonValue("expression", Pop());
            statement.AddJsonValue("embedded-statement", GetEmbeddedStatement(lockStatement.EmbeddedStatement));

            Push(statement);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitReturnStatement(ReturnStatement returnStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitReturnStatement";
            statement.AddJsonValue("statement-type", new JsonElement("return-statement"));
            statement.AddJsonValue("keyword", GetKeyword(ReturnStatement.ReturnKeywordRole));
            if (!returnStatement.Expression.IsNull)
            {
                returnStatement.Expression.AcceptVisitor(this);
                statement.AddJsonValue("expression", Pop());
            }
            Push(statement);
        }

        public void VisitSwitchSection(SwitchSection switchSection)
        {
            JsonObject section = new JsonObject();
            section.Comment = "VisitSwitchSection";

            JsonArray label = new JsonArray();
            foreach (var lb in switchSection.CaseLabels)
            {
                lb.AcceptVisitor(this);
                label.AddJsonValue(Pop());
            }
            if (label.Count == 0)
            {
                label = null;
            }
            section.AddJsonValue("label", label);
            JsonArray statement = new JsonArray();
            foreach (var stmt in switchSection.Statements)
            {
                stmt.AcceptVisitor(this);
                statement.AddJsonValue(Pop());
            }
            if (statement.Count == 0)
            {
                statement = null;
            }
            section.AddJsonValue("statements", statement);

            Push(section);
        }

        public void VisitSwitchStatement(SwitchStatement switchStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitSwitchStatement";
            statement.AddJsonValue("statement-type", new JsonElement("switch-statement"));
            statement.AddJsonValue("keyword", GetKeyword(SwitchStatement.SwitchKeywordRole));
            switchStatement.Expression.AcceptVisitor(this);
            statement.AddJsonValue("expression", Pop());
            JsonArray sections = new JsonArray();
            foreach (var sec in switchStatement.SwitchSections)
            {
                sec.AcceptVisitor(this);
                var temp = Pop();
                if (temp != null)
                    sections.AddJsonValue(temp);
            }
            if (sections.Count == 0)
            {
                sections = null;
            }
            statement.AddJsonValue("switch-sections", sections);

            Push(statement);
        }

        public void VisitCaseLabel(CaseLabel caseLabel)
        {
            JsonObject label = new JsonObject();
            label.Comment = "VisitCaseLabel";
            if (caseLabel.Expression.IsNull)
            {
                label.AddJsonValue("keyword", GetKeyword(CaseLabel.DefaultKeywordRole));
            }
            else
            {
                label.AddJsonValue("keyword", GetKeyword(CaseLabel.CaseKeywordRole));
                caseLabel.Expression.AcceptVisitor(this);
                label.AddJsonValue("expression", Pop());
            }

            Push(label);
        }

        public void VisitThrowStatement(ThrowStatement throwStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitThrowStatement";
            statement.AddJsonValue("statement-type", new JsonElement("throw-statement"));
            statement.AddJsonValue("keyword", GetKeyword(ThrowStatement.ThrowKeywordRole));
            if (!throwStatement.Expression.IsNull)
            {
                throwStatement.Expression.AcceptVisitor(this);
                statement.AddJsonValue("expression", Pop());
            }
            else
            {
                statement.AddJsonNull("expression");
            }
            Push(statement);
        }

        public void VisitTryCatchStatement(TryCatchStatement tryCatchStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitTryCatchStatement";
            statement.AddJsonValue("statement-type", new JsonElement("try-catch-statement"));
            statement.AddJsonValue("try-keyword", GetKeyword(TryCatchStatement.TryKeywordRole));
            tryCatchStatement.TryBlock.AcceptVisitor(this);
            statement.AddJsonValue("try-block", Pop());
            JsonArray catchClauseList = new JsonArray();
            foreach (var catchClause in tryCatchStatement.CatchClauses)
            {
                catchClause.AcceptVisitor(this);
                catchClauseList.AddJsonValue(Pop());
            }
            statement.AddJsonValue("catch-clause", catchClauseList);
            if (!tryCatchStatement.FinallyBlock.IsNull)
            {
                statement.AddJsonValue("final-keyword", GetKeyword(TryCatchStatement.FinallyKeywordRole));
                tryCatchStatement.FinallyBlock.AcceptVisitor(this);
                statement.AddJsonValue("final-block", Pop());
            }
            Push(statement);
        }

        public void VisitCatchClause(CatchClause catchClause)
        {
            JsonObject visitCatch = new JsonObject();
            visitCatch.Comment = "VisitCatchClause";
            visitCatch.AddJsonValue("catch-keyword", GetKeyword(CatchClause.CatchKeywordRole));
            if (!catchClause.Type.IsNull)
            {
                catchClause.Type.AcceptVisitor(this);
                visitCatch.AddJsonValue("type-info", Pop());
                if (!string.IsNullOrEmpty(catchClause.VariableName))
                {
                    visitCatch.AddJsonValue("identifier", GetIdentifier(catchClause.VariableNameToken));
                }
            }
            catchClause.Body.AcceptVisitor(this);
            visitCatch.AddJsonValue("catch-body", Pop());

            Push(visitCatch);
        }

        public void VisitUncheckedStatement(UncheckedStatement uncheckedStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitUncheckedStatement";
            statement.AddJsonValue("statement-type", new JsonElement("unchecked-statement"));
            statement.AddJsonValue("keyword", GetKeyword(UncheckedStatement.UncheckedKeywordRole));
            uncheckedStatement.Body.AcceptVisitor(this);
            statement.AddJsonValue("body", Pop());

            Push(statement);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitUnsafeStatement(UnsafeStatement unsafeStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitUnsafeStatement";
            statement.AddJsonValue("statement-type", new JsonElement("unsafe-statement"));
            statement.AddJsonValue("keyword", GetKeyword(UnsafeStatement.UnsafeKeywordRole));
            unsafeStatement.Body.AcceptVisitor(this);
            statement.AddJsonValue("body", Pop());

            Push(statement);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitUsingStatement(UsingStatement usingStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitUsingStatement";
            statement.AddJsonValue("statement-type", new JsonElement("using-statement"));
            statement.AddJsonValue("keyword", GetKeyword(UsingStatement.UsingKeywordRole));
            usingStatement.ResourceAcquisition.AcceptVisitor(this);
            statement.AddJsonValue("resource-acquisition", Pop());
            statement.AddJsonValue("embeded-statement", GetEmbeddedStatement(usingStatement.EmbeddedStatement));

            Push(statement);
            //implement already, but not tested
            //throw new Exception("first time testing");
        }

        public void VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitVariableDeclarationStatement";
            statement.AddJsonValue("statement-type", new JsonElement("variable-declaration"));
            JsonValue modifier = GetModifiers(variableDeclarationStatement.GetChildrenByRole(VariableDeclarationStatement.ModifierRole));
            statement.AddJsonValue("modifier", modifier);
            variableDeclarationStatement.Type.AcceptVisitor(this);
            statement.AddJsonValue("declaration-type-info", Pop());
            statement.AddJsonValue("variables-list", GetCommaSeparatedList(variableDeclarationStatement.Variables));
            Push(statement);
        }

        public void VisitWhileStatement(WhileStatement whileStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitWhileStatement";
            statement.AddJsonValue("statement-type", new JsonElement("while-statement"));
            whileStatement.Condition.AcceptVisitor(this);
            statement.AddJsonValue("condition", Pop());
            statement.AddJsonValue("statement-list", GetEmbeddedStatement(whileStatement.EmbeddedStatement));
            Push(statement);
        }

        public void VisitYieldBreakStatement(YieldBreakStatement yieldBreakStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitYieldBreakStatement";
            statement.AddJsonValue("statement-type", new JsonElement("yield-break-statement"));
            statement.AddJsonValue("yield-keyword", GetKeyword(YieldBreakStatement.YieldKeywordRole));
            statement.AddJsonValue("break-keyword", GetKeyword(YieldBreakStatement.BreakKeywordRole));
            Push(statement);
        }

        public void VisitYieldReturnStatement(YieldReturnStatement yieldReturnStatement)
        {
            JsonObject statement = new JsonObject();
            statement.Comment = "VisitYieldReturnStatement";
            statement.AddJsonValue("statement-type", new JsonElement("yield-return-statement"));
            statement.AddJsonValue("yield-keyword", GetKeyword(YieldReturnStatement.YieldKeywordRole));
            statement.AddJsonValue("return-keyword", GetKeyword(YieldReturnStatement.ReturnKeywordRole));
            yieldReturnStatement.Expression.AcceptVisitor(this);
            statement.AddJsonValue("expression", Pop());

            Push(statement);
        }

        #endregion

        #region TypeMembers
        public void VisitAccessor(Accessor accessor)
        {
            JsonObject visitAccessor = new JsonObject();
            visitAccessor.Comment = "VisitAccessor";
            visitAccessor.AddJsonValue("attributes", GetAttributes(accessor.Attributes));
            visitAccessor.AddJsonValue("modifier", GetModifiers(accessor.ModifierTokens));
            if (accessor.Role == PropertyDeclaration.GetterRole)
            {
                visitAccessor.AddJsonValue("keyword", new JsonElement("get"));
            }
            else if (accessor.Role == PropertyDeclaration.SetterRole)
            {
                visitAccessor.AddJsonValue("keyword", new JsonElement("set"));
            }
            else if (accessor.Role == CustomEventDeclaration.AddAccessorRole)
            {
                visitAccessor.AddJsonValue("keyword", new JsonElement("add"));
            }
            else if (accessor.Role == CustomEventDeclaration.RemoveAccessorRole)
            {
                visitAccessor.AddJsonValue("keyword", new JsonElement("remove"));
            }
            visitAccessor.AddJsonValue("body", GetMethodBody(accessor.Body));

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
                construct.AddJsonValue("type", GetIdentifier((Identifier)type.NameToken.Clone()));
            }
            else
            {
                construct.AddJsonValue("name", new JsonElement(constructorDeclaration.Name));
            }
            construct.AddJsonValue("modifier", GetModifiers(constructorDeclaration.ModifierTokens));
            construct.AddJsonValue("parameters", GetCommaSeparatedList(constructorDeclaration.Parameters));
            if (!constructorDeclaration.Initializer.IsNull)
            {
                constructorDeclaration.Initializer.AcceptVisitor(this);
                construct.AddJsonValue("initializer", Pop());
            }
            construct.AddJsonValue("body", GetMethodBody(constructorDeclaration.Body));
            Push(construct);
        }

        public void VisitConstructorInitializer(ConstructorInitializer constructorInitializer)
        {
            JsonObject initializer = new JsonObject();
            initializer.Comment = "VisitConstructorInitializer";
            if (constructorInitializer.ConstructorInitializerType == ConstructorInitializerType.This)
            {
                initializer.AddJsonValue("keyword", GetKeyword(ConstructorInitializer.ThisKeywordRole));
            }
            else
            {
                initializer.AddJsonValue("keyword", GetKeyword(ConstructorInitializer.BaseKeywordRole));
            }
            initializer.AddJsonValue("arguments", GetCommaSeparatedList(constructorInitializer.Arguments));

            Push(initializer);
            //implement already, but not tested
            // throw new Exception("first time testing");
        }

        public void VisitDestructorDeclaration(DestructorDeclaration destructorDeclaration)
        {
            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitDestructorDeclaration";
            declaration.AddJsonValue("attributes", GetAttributes(destructorDeclaration.Attributes));
            declaration.AddJsonValue("modifier", GetModifiers(destructorDeclaration.ModifierTokens));
            declaration.AddJsonValue("tilde-role", GetKeyword(DestructorDeclaration.TildeRole));
            TypeDeclaration type = destructorDeclaration.Parent as TypeDeclaration;
            if (type != null && type.Name != destructorDeclaration.Name)
            {
                declaration.AddJsonValue("name", GetIdentifier((Identifier)type.NameToken.Clone()));
            }
            else
            {
                declaration.AddJsonValue("name", GetIdentifier(destructorDeclaration.NameToken));
            }
            declaration.AddJsonValue("body", GetMethodBody(destructorDeclaration.Body));

            Push(declaration);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitEnumMemberDeclaration(EnumMemberDeclaration enumMemberDeclaration)
        {
            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitEnumMemberDeclaration";
            declaration.AddJsonValue("attributes", GetAttributes(enumMemberDeclaration.Attributes));
            declaration.AddJsonValue("modifier", GetModifiers(enumMemberDeclaration.ModifierTokens));
            declaration.AddJsonValue("identifier", GetIdentifier(enumMemberDeclaration.NameToken));
            if (!enumMemberDeclaration.Initializer.IsNull)
            {
                declaration.AddJsonValue("assign-role", GetKeyword(Roles.Assign));
                enumMemberDeclaration.Initializer.AcceptVisitor(this);
                declaration.AddJsonValue("initializer", Pop());
            }

            Push(declaration);
        }

        public void VisitEventDeclaration(EventDeclaration eventDeclaration)
        {
            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitEventDeclaration";
            declaration.AddJsonValue("attributes", GetAttributes(eventDeclaration.Attributes));
            declaration.AddJsonValue("modifier", GetModifiers(eventDeclaration.ModifierTokens));
            declaration.AddJsonValue("keyword", GetKeyword(EventDeclaration.EventKeywordRole));
            eventDeclaration.ReturnType.AcceptVisitor(this);
            declaration.AddJsonValue("return-type", Pop());
            declaration.AddJsonValue("variables", GetCommaSeparatedList(eventDeclaration.Variables));

            Push(declaration);
            //implement already, but not tested
            //throw new Exception("first time testing");
        }

        public void VisitCustomEventDeclaration(CustomEventDeclaration customEventDeclaration)
        {
            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitCustomEventDeclaration";
            declaration.AddJsonValue("attributes", GetAttributes(customEventDeclaration.Attributes));
            declaration.AddJsonValue("modifier", GetModifiers(customEventDeclaration.ModifierTokens));
            declaration.AddJsonValue("keyword", GetKeyword(EventDeclaration.EventKeywordRole));
            customEventDeclaration.ReturnType.AcceptVisitor(this);
            declaration.AddJsonValue("return-type", Pop());
            declaration.AddJsonValue("private-implementation-type", GetPrivateImplementationType(customEventDeclaration.PrivateImplementationType));
            declaration.AddJsonValue("identifier", GetIdentifier(customEventDeclaration.NameToken));
            JsonArray children = new JsonArray();
            foreach (AstNode node in customEventDeclaration.Children)
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
            declaration.AddJsonValue("children", children);

            Push(declaration);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitFieldDeclaration(FieldDeclaration fieldDeclaration)
        {
            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitFieldDeclaration";
            declaration.AddJsonValue("attributes", GetAttributes(fieldDeclaration.Attributes));
            declaration.AddJsonValue("modifier", GetModifiers(fieldDeclaration.ModifierTokens));
            fieldDeclaration.ReturnType.AcceptVisitor(this);
            declaration.AddJsonValue("return-type", Pop());
            declaration.AddJsonValue("variables", GetCommaSeparatedList(fieldDeclaration.Variables));

            Push(declaration);
        }

        public void VisitFixedFieldDeclaration(FixedFieldDeclaration fixedFieldDeclaration)
        {
            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitFixedFieldDeclaration";
            declaration.AddJsonValue("attributes", GetAttributes(fixedFieldDeclaration.Attributes));
            declaration.AddJsonValue("modifier", GetModifiers(fixedFieldDeclaration.ModifierTokens));
            declaration.AddJsonValue("keyword", GetKeyword(FixedFieldDeclaration.FixedKeywordRole));
            fixedFieldDeclaration.ReturnType.AcceptVisitor(this);
            declaration.AddJsonValue("return-type", Pop());
            declaration.AddJsonValue("variables", GetCommaSeparatedList(fixedFieldDeclaration.Variables));

            Push(declaration);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitFixedVariableInitializer(FixedVariableInitializer fixedVariableInitializer)
        {
            JsonObject initializer = new JsonObject();
            initializer.Comment = "VisitFixedVariableInitializer";
            initializer.AddJsonValue("identifier", GetIdentifier(fixedVariableInitializer.NameToken));
            if (!fixedVariableInitializer.CountExpression.IsNull)
            {
                fixedVariableInitializer.CountExpression.AcceptVisitor(this);
                initializer.AddJsonValue("count-expression", Pop());
            }
            else
            {
                initializer.AddJsonNull("count-expression");
            }
            Push(initializer);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration)
        {
            ClearTypeInfo();
            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitIndexerDeclaration";
            declaration.AddJsonValue("attributes", GetAttributes(indexerDeclaration.Attributes));
            declaration.AddJsonValue("modifier", GetModifiers(indexerDeclaration.ModifierTokens));
            indexerDeclaration.ReturnType.AcceptVisitor(this);
            declaration.AddJsonValue("return-type", Pop());
            declaration.AddJsonValue("private-implementation-type", GetPrivateImplementationType(indexerDeclaration.PrivateImplementationType));
            declaration.AddJsonValue("keyword", GetKeyword(IndexerDeclaration.ThisKeywordRole));
            declaration.AddJsonValue("parameters", GetCommaSeparatedList(indexerDeclaration.Parameters));
            JsonArray children = new JsonArray();
            foreach (AstNode node in indexerDeclaration.Children)
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
            declaration.AddJsonValue("children", children);
            declaration.AddJsonValue("type-info-list", GetTypeInfoList(TypeInfoKeys()));
            Push(declaration);
            ////implement already, but not tested
            //throw new Exception("first time testing");
        }

        public void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
        {
            ClearTypeInfo();
            JsonObject method = new JsonObject();
            method.Comment = "VisitMethodDeclaration";
            method.AddJsonValue("name", new JsonElement(methodDeclaration.Name));
            //write modifier
            method.AddJsonValue("modifier", GetModifiers(methodDeclaration.ModifierTokens));
            //write return type
            methodDeclaration.ReturnType.AcceptVisitor(this);
            method.AddJsonValue("return-type", Pop());
            //write parameters
            //ParameterDeclaration param = new ParameterDeclaration("test", ParameterModifier.None);
            //PrimitiveType type = new PrimitiveType("string");
            //param.Type = type;
            //methodDeclaration.AddChild(param, Roles.Parameter);
            method.AddJsonValue("parameters", GetCommaSeparatedList(methodDeclaration.Parameters));
            //write body
            method.AddJsonValue("body", GetMethodBody(methodDeclaration.Body));
            //write method type info
            method.AddJsonValue("type-info-list", GetTypeInfoList(TypeInfoKeys()));

            Push(method);
        }

        public void VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration)
        {
            ClearTypeInfo();
            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitOperatorDeclaration";
            declaration.AddJsonValue("attributes", GetAttributes(operatorDeclaration.Attributes));
            declaration.AddJsonValue("modifier", GetModifiers(operatorDeclaration.ModifierTokens));
            if (operatorDeclaration.OperatorType == OperatorType.Explicit)
            {
                declaration.AddJsonValue("keyword", GetKeyword(OperatorDeclaration.ExplicitRole));
            }
            else if (operatorDeclaration.OperatorType == OperatorType.Implicit)
            {
                declaration.AddJsonValue("keyword", GetKeyword(OperatorDeclaration.ImplicitRole));
            }
            else
            {
                operatorDeclaration.ReturnType.AcceptVisitor(this);
                declaration.AddJsonValue("return-type", Pop());
            }
            declaration.AddJsonValue("operator-keyword", GetKeyword(OperatorDeclaration.OperatorKeywordRole));
            if (operatorDeclaration.OperatorType == OperatorType.Explicit
                || operatorDeclaration.OperatorType == OperatorType.Implicit)
            {
                operatorDeclaration.ReturnType.AcceptVisitor(this);
                declaration.AddJsonValue("return-type", Pop());
            }
            else
            {
                declaration.AddJsonValue("operator-type", new JsonElement(OperatorDeclaration.GetToken(operatorDeclaration.OperatorType)));
            }
            declaration.AddJsonValue("parameters", GetCommaSeparatedList(operatorDeclaration.Parameters));
            declaration.AddJsonValue("type-info-list", GetTypeInfoList(TypeInfoKeys()));
            declaration.AddJsonValue("body", GetMethodBody(operatorDeclaration.Body));

            Push(declaration);
            //implement already, but not tested
            //throw new Exception("first time testing");
        }

        public void VisitParameterDeclaration(ParameterDeclaration parameterDeclaration)
        {
            JsonObject parameter = new JsonObject();
            parameter.Comment = "VisitParameterDeclaration";
            parameter.AddJsonValue("attributes", GetAttributes(parameterDeclaration.Attributes));
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
            parameter.AddJsonValue("modifier", keyword);
            parameterDeclaration.Type.AcceptVisitor(this);
            parameter.AddJsonValue("type-info", Pop());
            parameter.AddJsonValue("name", new JsonElement(parameterDeclaration.Name));
            if (parameterDeclaration.DefaultExpression.IsNull)
            {
                parameterDeclaration.DefaultExpression.AcceptVisitor(this);
                parameter.AddJsonValue("default-expression", Pop());
            }
            Push(parameter);
        }

        public void VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
        {
            ClearTypeInfo();
            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitPropertyDeclaration";
            declaration.AddJsonValue("attributes", GetAttributes(propertyDeclaration.Attributes));
            declaration.AddJsonValue("modifier", GetModifiers(propertyDeclaration.ModifierTokens));
            propertyDeclaration.ReturnType.AcceptVisitor(this);
            declaration.AddJsonValue("return-type", Pop());
            declaration.AddJsonValue("private-implementation-type", GetPrivateImplementationType(propertyDeclaration.PrivateImplementationType));
            declaration.AddJsonValue("identifier", GetIdentifier(propertyDeclaration.NameToken));
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
            declaration.AddJsonValue("children", children);
            declaration.AddJsonValue("type-info-list", GetTypeInfoList(TypeInfoKeys()));

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
            variable.AddJsonValue("variable-name", GetIdentifier(variableInitializer.NameToken));
            if (!variableInitializer.Initializer.IsNull)
            {
                variableInitializer.Initializer.AcceptVisitor(this);
                variable.AddJsonValue("initializer", Pop());
            }
            else
            {
                variable.AddJsonNull("initializer");
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
            memtype.AddJsonValue("type-info", Pop());
            memtype.AddJsonValue("member-name", GetIdentifier(memberType.MemberNameToken));
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
                    visit.AddJsonValue("comment-type", new JsonElement("documentation"));
                    break;
                case CommentType.InactiveCode:
                    visit.AddJsonValue("comment-type", new JsonElement("inactive-code"));
                    break;
                case CommentType.MultiLine:
                    visit.AddJsonValue("comment-type", new JsonElement("multiline"));
                    break;
                case CommentType.MultiLineDocumentation:
                    visit.AddJsonValue("comment-type", new JsonElement("multiline-documentation"));
                    break;
                case CommentType.SingleLine:
                    visit.AddJsonValue("comment-type", new JsonElement("single-line"));
                    break;
                default:
                    throw new NotSupportedException("Invalid value for CommentType");
            }
            visit.AddJsonValue("comment-content", new JsonElement(comment.Content));

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
            visit.AddJsonValue("preprocessordirective-type", new JsonElement(typeStr));
            if (!string.IsNullOrEmpty(preProcessorDirective.Argument))
            {
                visit.AddJsonValue("argument", new JsonElement(preProcessorDirective.Argument));
            }
            else
            {
                visit.AddJsonNull("argument");
            }

            Push(visit);
            //implement already, but not tested
            throw new Exception("first time testing");
        }

        public void VisitTypeParameterDeclaration(TypeParameterDeclaration typeParameterDeclaration)
        {
            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitTypeParameterDeclaration";
            declaration.AddJsonValue("attributes", GetAttributes(typeParameterDeclaration.Attributes));
            switch (typeParameterDeclaration.Variance)
            {
                case VarianceModifier.Invariant:
                    break;
                case VarianceModifier.Covariant:
                    declaration.AddJsonValue("keyword", GetKeyword(TypeParameterDeclaration.OutVarianceKeywordRole));
                    break;
                case VarianceModifier.Contravariant:
                    declaration.AddJsonValue("keyword", GetKeyword(TypeParameterDeclaration.InVarianceKeywordRole));
                    break;
                default:
                    throw new NotSupportedException("Invalid value for VarianceModifier");
            }
            declaration.AddJsonValue("identifier", GetIdentifier(typeParameterDeclaration.NameToken));

            Push(declaration);
            //implement already, but not tested
            //throw new Exception("first time testing");
        }

        public void VisitConstraint(Constraint constraint)
        {
            JsonObject visit = new JsonObject();
            visit.Comment = "VisitConstraint";
            visit.AddJsonValue("keyword", GetKeyword(Roles.WhereKeyword));
            constraint.TypeParameter.AcceptVisitor(this);
            visit.AddJsonValue("type-parameter", Pop());
            visit.AddJsonValue("base-types", GetCommaSeparatedList(constraint.BaseTypes));

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