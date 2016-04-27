//MIT, 2016, Brezza92, EngineKit

// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

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
    public partial class AstCsToJsonVisitor : IAstVisitor
    {
        Stack<JsonValue> jsonValueStack = new Stack<JsonValue>();
        Dictionary<string, int> typeReferences = new Dictionary<string, int>();



        public JsonValue LastValue { get; private set; }

        public AstCsToJsonVisitor(ITextOutput output)
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
        void CreateGlobalSymbolTable()
        {
            typeReferences.Clear();
            memberReferences.Clear();
        }

        int RegisterType(string type)
        {
            int index = 0;
            if (!typeReferences.TryGetValue(type, out index))
            {
                index = typeReferences.Count;
                typeReferences.Add(type, index);
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
            return new JsonElement(identifier.Name);
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
            JsonObject expression = CreateJsonExpression(anonymousMethodExpression);
            AddKeyword(expression, AnonymousMethodExpression.AsyncModifierRole);
            expression.AddJsonValue("delegate-keyword", GetKeyword(AnonymousMethodExpression.DelegateKeywordRole));
            if (anonymousMethodExpression.HasParameterList)
            {
                expression.AddJsonValue("arguments", GetCommaSeparatedList(anonymousMethodExpression.Parameters));
            }

            expression.AddJsonValue("body", GenStatement(anonymousMethodExpression.Body));
            Push(expression);
        }
        public void VisitUndocumentedExpression(UndocumentedExpression undocumentedExpression)
        {
            JsonObject expression = CreateJsonExpression(undocumentedExpression);


            switch (undocumentedExpression.UndocumentedExpressionType)
            {
                case UndocumentedExpressionType.ArgList:
                case UndocumentedExpressionType.ArgListAccess:
                    GetTypeIndex(UndocumentedExpression.ArglistKeywordRole.Token);
                    expression.AddJsonValue("type-info", GetTypeIndex(UndocumentedExpression.ArglistKeywordRole.Token));
                    break;
                case UndocumentedExpressionType.MakeRef:
                    expression.AddJsonValue("type-info", GetTypeIndex(UndocumentedExpression.MakerefKeywordRole.Token));
                    break;
                case UndocumentedExpressionType.RefType:
                    expression.AddJsonValue("type-info", GetTypeIndex(UndocumentedExpression.ReftypeKeywordRole.Token));
                    break;
                case UndocumentedExpressionType.RefValue:
                    expression.AddJsonValue("type-info", GetTypeIndex(UndocumentedExpression.RefvalueKeywordRole.Token));
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

        }

        public void VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression)
        {
            JsonObject expression = CreateJsonExpression(arrayCreateExpression);
            AddKeyword(expression, ArrayCreateExpression.NewKeywordRole);
            expression.AddJsonValue("array-type", GenTypeInfo(arrayCreateExpression.Type));
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
                    specifierArr.AddJsonValue(pop);
                }
            }
            if (specifierArr.Count == 0)
            {
                specifierArr = null;
            }
            expression.AddJsonValue("specifier", specifierArr);
            expression.AddJsonValue("initializer", GenExpression(arrayCreateExpression.Initializer));
            Push(expression);
        }

        public void VisitArrayInitializerExpression(ArrayInitializerExpression arrayInitializerExpression)
        {
            JsonObject expression = CreateJsonExpression(arrayInitializerExpression);

            bool bracesAreOptional = arrayInitializerExpression.Elements.Count == 1
                && IsObjectOrCollectionInitializer(arrayInitializerExpression.Parent)
                && !CanBeConfusedWithObjectInitializer(arrayInitializerExpression.Elements.Single());
            if (bracesAreOptional && arrayInitializerExpression.LBraceToken.IsNull)
            {

                expression.AddJsonValue("elements", GenExpression(arrayInitializerExpression.Elements.Single()));
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
            JsonObject expression = CreateJsonExpression(asExpression);
            AddKeyword(expression, AsExpression.AsKeywordRole);
            expression.AddJsonValue("type-info", GenTypeInfo(asExpression.Type));
            Push(expression);
        }

        public void VisitAssignmentExpression(AssignmentExpression assignmentExpression)
        {
            JsonObject expression = CreateJsonExpression(assignmentExpression);
            expression.AddJsonValue("left-operand", GenExpression(assignmentExpression.Left));
            TokenRole operatorRole = AssignmentExpression.GetOperatorRole(assignmentExpression.Operator);
            expression.AddJsonValue("operator", GetKeyword(operatorRole));
            expression.AddJsonValue("right-operand", GenExpression(assignmentExpression.Right));
            Push(expression);
        }

        public void VisitBaseReferenceExpression(BaseReferenceExpression baseReferenceExpression)
        {
            JsonObject expression = CreateJsonExpression(baseReferenceExpression);
            Push(expression);
        }

        public void VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression)
        {
            JsonObject expression = CreateJsonExpression(binaryOperatorExpression);

            expression.AddJsonValue("left-operand", GenExpression(binaryOperatorExpression.Left));
            expression.AddJsonValue("operator", BinaryOperatorExpression.GetOperatorRole(binaryOperatorExpression.Operator).Token);
            expression.AddJsonValue("right-operand", GenExpression(binaryOperatorExpression.Right));

            Push(expression);
        }
        public void VisitCastExpression(CastExpression castExpression)
        {
            JsonObject expression = CreateJsonExpression(castExpression);
            expression.AddJsonValue("type-info", GenTypeInfo(castExpression.Type));
            expression.AddJsonValue("expression", GenExpression(castExpression.Expression));
            Push(expression);

        }
        public void VisitCheckedExpression(CheckedExpression checkedExpression)
        {
            JsonObject expression = CreateJsonExpression(checkedExpression);
            AddKeyword(expression, CheckedExpression.CheckedKeywordRole);
            expression.AddJsonValue("expression", GenExpression(checkedExpression.Expression));
            Push(expression);
        }

        public void VisitConditionalExpression(ConditionalExpression conditionalExpression)
        {
            JsonObject expression = CreateJsonExpression(conditionalExpression);

            expression.AddJsonValue("condition", GenExpression(conditionalExpression.Condition));
            expression.AddJsonValue("true-expression", GenExpression(conditionalExpression.TrueExpression));
            expression.AddJsonValue("false-expression", GenExpression(conditionalExpression.FalseExpression));

            Push(expression);

        }

        public void VisitDefaultValueExpression(DefaultValueExpression defaultValueExpression)
        {
            JsonObject expression = CreateJsonExpression(defaultValueExpression);
            AddKeyword(expression, DefaultValueExpression.DefaultKeywordRole);
            expression.AddJsonValue("type-info", GenTypeInfo(defaultValueExpression.Type));

            Push(expression);
        }

        public void VisitDirectionExpression(DirectionExpression directionExpression)
        {
            JsonObject expression = CreateJsonExpression(directionExpression);
            switch (directionExpression.FieldDirection)
            {
                case FieldDirection.Out:
                    //essential
                    expression.AddJsonValue("keyword", GetKeyword(DirectionExpression.OutKeywordRole));
                    break;
                case FieldDirection.Ref:
                    //essential
                    expression.AddJsonValue("keyword", GetKeyword(DirectionExpression.RefKeywordRole));
                    break;
                default:
                    throw new NotSupportedException("Invalid value for FieldDirection");
            }

            expression.AddJsonValue("expression", GenExpression(directionExpression.Expression));

            Push(expression);
        }

        public void VisitIdentifierExpression(IdentifierExpression identifierExpression)
        {
            JsonValue getIdentifier = GetIdentifier(identifierExpression.IdentifierToken);
            JsonObject jsonIdenExpr = CreateJsonExpression(identifierExpression);
            jsonIdenExpr.AddJsonValue("name", getIdentifier);
            Push(jsonIdenExpr);
        }

        public void VisitIndexerExpression(IndexerExpression indexerExpression)
        {
            JsonObject expression = CreateJsonExpression(indexerExpression);

            expression.AddJsonValue("target", GenExpression(indexerExpression.Target));
            expression.AddJsonValue("arguments", GetCommaSeparatedList(indexerExpression.Arguments));
            Push(expression);
        }

        public void VisitInvocationExpression(InvocationExpression invocationExpression)
        {
            JsonObject expression = CreateJsonExpression(invocationExpression);
            expression.AddJsonValue("target", GenExpression(invocationExpression.Target));
            expression.AddJsonValue("arguments", GetCommaSeparatedList(invocationExpression.Arguments));
            Push(expression);
        }

        public void VisitIsExpression(IsExpression isExpression)
        {
            JsonObject expression = CreateJsonExpression(isExpression);

            AddKeyword(expression, IsExpression.IsKeywordRole);
            expression.AddJsonValue("type-info", GenTypeInfo(isExpression.Type));
            expression.AddJsonValue("expression", GenExpression(isExpression.Expression));

            Push(expression);
        }

        public void VisitLambdaExpression(LambdaExpression lambdaExpression)
        {
            JsonObject expression = CreateJsonExpression(lambdaExpression);

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
            JsonObject expression = CreateJsonExpression(memberReferenceExpression);
            expression.AddJsonValue("target", GenExpression(memberReferenceExpression.Target));
            expression.AddJsonValue("identifier-name", GetIdentifier(memberReferenceExpression.MemberNameToken));

            Push(expression);
        }

        public void VisitNamedArgumentExpression(NamedArgumentExpression namedArgumentExpression)
        {
            JsonObject expression = CreateJsonExpression(namedArgumentExpression);
            expression.AddJsonValue("identifier", GetIdentifier(namedArgumentExpression.NameToken));
            expression.AddJsonValue("expression", GenExpression(namedArgumentExpression.Expression));

            Push(expression);
            throw new FirstTimeUseException();
        }

        public void VisitNamedExpression(NamedExpression namedExpression)
        {
            JsonObject expression = CreateJsonExpression(namedExpression);
            expression.AddJsonValue("identifier", GetIdentifier(namedExpression.NameToken));
            expression.AddJsonValue("expression", GenExpression(namedExpression.Expression));

            Push(expression);

        }

        public void VisitNullReferenceExpression(NullReferenceExpression nullReferenceExpression)
        {
            JsonObject expression = CreateJsonExpression(nullReferenceExpression);
            Push(expression);
        }

        public void VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression)
        {
            JsonObject expression = CreateJsonExpression(objectCreateExpression);
            AddKeyword(expression, ObjectCreateExpression.NewKeywordRole);
            expression.AddJsonValue("type-info", GenTypeInfo(objectCreateExpression.Type));

            bool useParenthesis = objectCreateExpression.Arguments.Any() || objectCreateExpression.Initializer.IsNull;
            if (!objectCreateExpression.LParToken.IsNull)
            {
                useParenthesis = true;
            }
            if (useParenthesis)
            {
                expression.AddJsonValue("arguments", GetCommaSeparatedList(objectCreateExpression.Arguments));
            }

            expression.AddJsonValue("initializer", GenExpression(objectCreateExpression.Initializer));
            Push(expression);
        }

        public void VisitAnonymousTypeCreateExpression(AnonymousTypeCreateExpression anonymousTypeCreateExpression)
        {
            JsonObject expression = CreateJsonExpression(anonymousTypeCreateExpression);
            AddKeyword(expression, AnonymousTypeCreateExpression.NewKeywordRole);
            expression.AddJsonValue("elements", GetInitializerElements(anonymousTypeCreateExpression.Initializers));
            Push(expression);

        }
        public void VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression)
        {
            JsonObject expression = CreateJsonExpression(parenthesizedExpression);
            expression.AddJsonValue("expression", GenExpression(parenthesizedExpression.Expression));
            Push(expression);
        }
        public void VisitPointerReferenceExpression(PointerReferenceExpression pointerReferenceExpression)
        {
            JsonObject expression = CreateJsonExpression(pointerReferenceExpression);

            expression.AddJsonValue("target", GenExpression(pointerReferenceExpression.Target));
            expression.AddJsonValue("identifier", GetIdentifier(pointerReferenceExpression.MemberNameToken));
            expression.AddJsonValue("type-arguments", GetTypeArguments(pointerReferenceExpression.TypeArguments));

            Push(expression);
        }
        public void VisitPrimitiveExpression(PrimitiveExpression primitiveExpression)
        {
            JsonObject expression = CreateJsonExpression(primitiveExpression);
            expression.AddJsonValue("value", primitiveExpression.Value.ToString());
            if (primitiveExpression.UnsafeLiteralValue != null)
            {
                expression.AddJsonValue("unsafe-literal-value", primitiveExpression.UnsafeLiteralValue);
            }
            Push(expression);
        }


        public void VisitSizeOfExpression(SizeOfExpression sizeOfExpression)
        {
            JsonObject expression = CreateJsonExpression(sizeOfExpression);
            AddKeyword(expression, SizeOfExpression.SizeofKeywordRole);
            expression.AddJsonValue("type-info", GenTypeInfo(sizeOfExpression.Type));

            Push(expression);
        }

        public void VisitStackAllocExpression(StackAllocExpression stackAllocExpression)
        {
            JsonObject expression = CreateJsonExpression(stackAllocExpression);
            AddKeyword(expression, StackAllocExpression.StackallocKeywordRole);
            expression.AddJsonValue("type-info", GenTypeInfo(stackAllocExpression.Type));
            expression.AddJsonValue("count-expression", GetCommaSeparatedList(new[] { stackAllocExpression.CountExpression }));

            Push(expression);
        }
        public void VisitThisReferenceExpression(ThisReferenceExpression thisReferenceExpression)
        {
            JsonObject expression = CreateJsonExpression(thisReferenceExpression);
            Push(expression);
        }
        public void VisitTypeOfExpression(TypeOfExpression typeOfExpression)
        {
            JsonObject expression = CreateJsonExpression(typeOfExpression);
            AddKeyword(expression, TypeOfExpression.TypeofKeywordRole);
            expression.AddJsonValue("type-info", GenTypeInfo(typeOfExpression.Type));
            Push(expression);
        }
        public void VisitTypeReferenceExpression(TypeReferenceExpression typeReferenceExpression)
        {
            typeReferenceExpression.Type.AcceptVisitor(this);
        }
        public void VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression)
        {
            JsonObject expression = CreateJsonExpression(unaryOperatorExpression);
            UnaryOperatorType opType = unaryOperatorExpression.Operator;
            var opSymbol = UnaryOperatorExpression.GetOperatorRole(opType);
            if (opType == UnaryOperatorType.Await)
            {
                expression.AddJsonValue("operator", GetKeyword(opSymbol));
            }
            else if (!(opType == UnaryOperatorType.PostIncrement || opType == UnaryOperatorType.PostDecrement))
            {
                expression.AddJsonValue("operator", GetKeyword(opSymbol));
            }

            expression.AddJsonValue("expression", GenExpression(unaryOperatorExpression.Expression));
            if (opType == UnaryOperatorType.PostIncrement || opType == UnaryOperatorType.PostDecrement)
            {
                expression.AddJsonValue("operator", GetKeyword(opSymbol));
            }
            Push(expression);
        }

        public void VisitUncheckedExpression(UncheckedExpression uncheckedExpression)
        {
            JsonObject expression = CreateJsonExpression(uncheckedExpression);

            AddKeyword(expression, UncheckedExpression.UncheckedKeywordRole);
            expression.AddJsonValue("expression", GenExpression(uncheckedExpression.Expression));

            Push(expression);
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

            visit.AddJsonValue("type", GenTypeInfo(attribute.Type));
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
                visit.AddJsonValue("target", new JsonElement(attributeSection.AttributeTarget));
            }
            visit.AddJsonValue("attributes", GetCommaSeparatedList(attributeSection.Attributes));

            Push(visit);
        }

        public void VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration)
        {
            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitDelegateDeclaration";
            AddAttributes(declaration, delegateDeclaration);
            AddModifiers(declaration, delegateDeclaration);
            AddReturnType(declaration, delegateDeclaration);
            AddKeyword(declaration, Roles.DelegateKeyword);
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


            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitNamespaceDeclaration";
            AddKeyword(declaration, Roles.NamespaceKeyword);
            declaration.AddJsonValue("namespace-name", GenerateNamespaceString(namespaceDeclaration.NamespaceName));

            JsonArray memberList = new JsonArray();
            foreach (var member in namespaceDeclaration.Members)
            {
                member.AcceptVisitor(this);
                var temp = Pop();
                if (temp != null)
                {
                    memberList.AddJsonValue(temp);
                }
            }
            if (memberList.Count == 0)
            {
                memberList = null;
            }
            declaration.AddJsonValue("members", memberList);

            Push(declaration);
        } 
         
        public void VisitTypeDeclaration(TypeDeclaration typeDeclaration)
        {
            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitTypeDeclaration";
            AddAttributes(declaration, typeDeclaration);
            AddModifiers(declaration, typeDeclaration);

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
        }

        public void VisitUsingAliasDeclaration(UsingAliasDeclaration usingAliasDeclaration)
        {
            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitUsingAliasDeclaration";

            AddKeyword(declaration, UsingAliasDeclaration.UsingKeywordRole);
            declaration.AddJsonValue("identifier", GetIdentifier(usingAliasDeclaration.GetChildByRole(UsingAliasDeclaration.AliasRole)));
            declaration.AddJsonValue("assign-token", GetKeyword(Roles.Assign));
            declaration.AddJsonValue("import", GenTypeInfo(usingAliasDeclaration.Import));

            Push(declaration);
            //implement already, but not tested
            throw new FirstTimeUseException();
        }

        public void VisitUsingDeclaration(UsingDeclaration usingDeclaration)
        {


            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitUsingDeclaration";
            AddKeyword(declaration, UsingDeclaration.UsingKeywordRole);
            declaration.AddJsonValue("import", GenerateNamespaceString(usingDeclaration.Import));

            Push(declaration);
        }
        string GenerateNamespaceString(AstType astType)
        {
            return astType.ToString();
        }
        public void VisitExternAliasDeclaration(ExternAliasDeclaration externAliasDeclaration)
        {
            JsonObject declaration = new JsonObject();
            declaration.Comment = "VisitExternAliasDeclaration";
            AddKeyword(declaration, "extern-keyword", Roles.ExternKeyword);
            AddKeyword(declaration, "alias-keyword", Roles.AliasKeyword);
            declaration.AddJsonValue("identifier", GetIdentifier(externAliasDeclaration.NameToken));

            Push(declaration);
            //implement already, but not tested
            throw new FirstTimeUseException();
        }
        #endregion

        #region Statements
        public void VisitBlockStatement(BlockStatement blockStatement)
        {
            JsonObject statement = CreateJsonStatement(blockStatement);
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
            JsonObject statement = CreateJsonStatement(breakStatement);
            AddKeyword(statement, BreakStatement.BreakKeywordRole);
            Push(statement);
        }

        public void VisitCheckedStatement(CheckedStatement checkedStatement)
        {
            JsonObject statement = CreateJsonStatement(checkedStatement);
            AddKeyword(statement, CheckedStatement.CheckedKeywordRole);
            statement.AddJsonValue("body", GenStatement(checkedStatement.Body));
            Push(statement);
        }

        public void VisitContinueStatement(ContinueStatement continueStatement)
        {
            JsonObject statement = CreateJsonStatement(continueStatement);
            AddKeyword(statement, ContinueStatement.ContinueKeywordRole);
            Push(statement);
        }

        public void VisitDoWhileStatement(DoWhileStatement doWhileStatement)
        {
            JsonObject statement = CreateJsonStatement(doWhileStatement);
            statement.AddJsonValue("condition", GenExpression(doWhileStatement.Condition));
            statement.AddJsonValue("statement-list", GenStatement(doWhileStatement.EmbeddedStatement));
            Push(statement);
        }

        public void VisitEmptyStatement(EmptyStatement emptyStatement)
        {
            //empty statement

            JsonObject statement = CreateJsonStatement(emptyStatement);
            Push(statement);
        }

        public void VisitExpressionStatement(ExpressionStatement expressionStatement)
        {
            //expression statemenrt !!
            expressionStatement.Expression.AcceptVisitor(this);
        }

        public void VisitFixedStatement(FixedStatement fixedStatement)
        {
            JsonObject statement = CreateJsonStatement(fixedStatement);

            AddKeyword(statement, FixedStatement.FixedKeywordRole);
            statement.AddJsonValue("type-info", GenTypeInfo(fixedStatement.Type));
            statement.AddJsonValue("variables", GetCommaSeparatedList(fixedStatement.Variables));
            statement.AddJsonValue("embedded-statement", GenStatement(fixedStatement.EmbeddedStatement));

            Push(statement);
        }

        public void VisitForeachStatement(ForeachStatement foreachStatement)
        {
            JsonObject statement = CreateJsonStatement(foreachStatement);

            AddKeyword(statement, ForeachStatement.ForeachKeywordRole);
            statement.AddJsonValue("local-variable-type", GenTypeInfo(foreachStatement.VariableType));
            statement.AddJsonValue("local-variable-name", GetIdentifier(foreachStatement.VariableNameToken));
            statement.AddJsonValue("in-expression", GenExpression(foreachStatement.InExpression));
            statement.AddJsonValue("embedded-statement", GenStatement(foreachStatement.EmbeddedStatement));
            Push(statement);
        }

        public void VisitForStatement(ForStatement forStatement)
        {
            JsonObject statement = CreateJsonStatement(forStatement);

            AddKeyword(statement, ForStatement.ForKeywordRole);
            statement.AddJsonValue("initializer", GetCommaSeparatedList(forStatement.Initializers));
            statement.AddJsonValue("condition", GenExpression(forStatement.Condition));
            if (forStatement.Iterators.Any())
            {
                statement.AddJsonValue("iterators", GetCommaSeparatedList(forStatement.Iterators));
            }
            statement.AddJsonValue("embedded-statement", GenStatement(forStatement.EmbeddedStatement));
            Push(statement);
        }

        public void VisitGotoCaseStatement(GotoCaseStatement gotoCaseStatement)
        {
            JsonObject statement = CreateJsonStatement(gotoCaseStatement);

            AddKeyword(statement, GotoCaseStatement.GotoKeywordRole);
            AddKeyword(statement, "case-keyword", GotoCaseStatement.CaseKeywordRole);
            statement.AddJsonValue("label-expression", GenExpression(gotoCaseStatement.LabelExpression));
            Push(statement);
            //implement already, but not tested
            throw new FirstTimeUseException();
        }

        public void VisitGotoDefaultStatement(GotoDefaultStatement gotoDefaultStatement)
        {
            JsonObject statement = CreateJsonStatement(gotoDefaultStatement);
            AddKeyword(statement, GotoDefaultStatement.GotoKeywordRole);
            AddKeyword(statement, "default-keyword", GotoDefaultStatement.DefaultKeywordRole);
            Push(statement);
            throw new FirstTimeUseException();

        }
        public void VisitGotoStatement(GotoStatement gotoStatement)
        {
            JsonObject statement = CreateJsonStatement(gotoStatement);
            AddKeyword(statement, GotoStatement.GotoKeywordRole);
            statement.AddJsonValue("identifier", GetIdentifier(gotoStatement.GetChildByRole(Roles.Identifier)));
            Push(statement);
        }
        public void VisitIfElseStatement(IfElseStatement ifElseStatement)
        {

            JsonObject statement = CreateJsonStatement(ifElseStatement);
            statement.AddJsonValue("condition", GenExpression(ifElseStatement.Condition));
            statement.AddJsonValue("true-statement", GenStatement(ifElseStatement.TrueStatement));
            statement.AddJsonValue("false-statement", GenStatement(ifElseStatement.FalseStatement));
           
            Push(statement);
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
            JsonObject statement = CreateJsonStatement(labelStatement);
            statement.AddJsonValue("identifier", GetIdentifier(labelStatement.GetChildByRole(Roles.Identifier)));

            Push(statement);
        }

        public void VisitLockStatement(LockStatement lockStatement)
        {
            JsonObject statement = CreateJsonStatement(lockStatement);
            AddKeyword(statement, LockStatement.LockKeywordRole);

            statement.AddJsonValue("expression", GenExpression(lockStatement.Expression));
            statement.AddJsonValue("embedded-statement", GenStatement(lockStatement.EmbeddedStatement));

            Push(statement);
        }

        public void VisitReturnStatement(ReturnStatement returnStatement)
        {
            JsonObject statement = CreateJsonStatement(returnStatement);

            AddKeyword(statement, ReturnStatement.ReturnKeywordRole);
            if (!returnStatement.Expression.IsNull)
            {
                statement.AddJsonValue("expression", GenExpression(returnStatement.Expression));
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
            JsonObject statement = CreateJsonStatement(switchStatement);

            AddKeyword(statement, SwitchStatement.SwitchKeywordRole);
            statement.AddJsonValue("expression", GenExpression(switchStatement.Expression));
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
                AddKeyword(label, CaseLabel.DefaultKeywordRole);

            }
            else
            {
                AddKeyword(label, CaseLabel.CaseKeywordRole);
                label.AddJsonValue("expression", GenExpression(caseLabel.Expression));
            }

            Push(label);
        }

        public void VisitThrowStatement(ThrowStatement throwStatement)
        {
            JsonObject statement = CreateJsonStatement(throwStatement);

            AddKeyword(statement, ThrowStatement.ThrowKeywordRole);
            if (!throwStatement.Expression.IsNull)
            {
                statement.AddJsonValue("expression", GenExpression(throwStatement.Expression));
            }
            else
            {
                statement.AddJsonNull("expression");
            }
            Push(statement);
        }

        public void VisitTryCatchStatement(TryCatchStatement tryCatchStatement)
        {
            JsonObject statement = CreateJsonStatement(tryCatchStatement);
            AddKeyword(statement, "try-keyword", TryCatchStatement.TryKeywordRole);

            statement.AddJsonValue("try-block", GenStatement(tryCatchStatement.TryBlock));
            JsonArray catchClauseList = new JsonArray();
            foreach (var catchClause in tryCatchStatement.CatchClauses)
            {
                catchClause.AcceptVisitor(this);
                catchClauseList.AddJsonValue(Pop());
            }
            statement.AddJsonValue("catch-clause", catchClauseList);
            if (!tryCatchStatement.FinallyBlock.IsNull)
            {
                AddKeyword(statement, "final-keyword", TryCatchStatement.FinallyKeywordRole);
                statement.AddJsonValue("final-block", GenStatement(tryCatchStatement.FinallyBlock));
            }
            Push(statement);
        }

        public void VisitCatchClause(CatchClause catchClause)
        {
            JsonObject visitCatch = new JsonObject();
            visitCatch.Comment = "VisitCatchClause";

            AddKeyword(visitCatch, "catch-keyword", CatchClause.CatchKeywordRole);

            if (!catchClause.Type.IsNull)
            {
                visitCatch.AddJsonValue("type-info", GenTypeInfo(catchClause.Type));
                if (!string.IsNullOrEmpty(catchClause.VariableName))
                {
                    visitCatch.AddJsonValue("identifier", GetIdentifier(catchClause.VariableNameToken));
                }
            }

            visitCatch.AddJsonValue("catch-body", GenStatement(catchClause.Body));

            Push(visitCatch);
        }

        public void VisitUncheckedStatement(UncheckedStatement uncheckedStatement)
        {
            JsonObject statement = CreateJsonStatement(uncheckedStatement);

            AddKeyword(statement, UncheckedStatement.UncheckedKeywordRole);
            statement.AddJsonValue("body", GenStatement(uncheckedStatement.Body));
            Push(statement);

            throw new FirstTimeUseException();
        }

        public void VisitUnsafeStatement(UnsafeStatement unsafeStatement)
        {
            JsonObject statement = CreateJsonStatement(unsafeStatement);

            AddKeyword(statement, UnsafeStatement.UnsafeKeywordRole);
            statement.AddJsonValue("body", GenStatement(unsafeStatement.Body));

            Push(statement);
            throw new FirstTimeUseException();
        }

        public void VisitUsingStatement(UsingStatement usingStatement)
        {
            JsonObject statement = CreateJsonStatement(usingStatement);

            AddKeyword(statement, UsingStatement.UsingKeywordRole);
            usingStatement.ResourceAcquisition.AcceptVisitor(this);
            statement.AddJsonValue("resource-acquisition", Pop());
            statement.AddJsonValue("embeded-statement", GenStatement(usingStatement.EmbeddedStatement));

            Push(statement);
        }

        public void VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement)
        {
            JsonObject statement = CreateJsonStatement(variableDeclarationStatement);
            JsonValue modifier = GetModifiers(variableDeclarationStatement.GetChildrenByRole(VariableDeclarationStatement.ModifierRole));
            if (modifier != null && modifier.ValueType != JsonValueType.Null)
            {
                statement.AddJsonValue("modifier", modifier);
            }
            statement.AddJsonValue("declaration-type-info", GenTypeInfo(variableDeclarationStatement.Type));
            statement.AddJsonValue("variables-list", GetCommaSeparatedList(variableDeclarationStatement.Variables));
            Push(statement);
        }

        public void VisitWhileStatement(WhileStatement whileStatement)
        {
            JsonObject statement = CreateJsonStatement(whileStatement);
            statement.AddJsonValue("condition", GenExpression(whileStatement.Condition));
            statement.AddJsonValue("statement-list", GenStatement(whileStatement.EmbeddedStatement));
            Push(statement);
        }

        public void VisitYieldBreakStatement(YieldBreakStatement yieldBreakStatement)
        {
            JsonObject statement = CreateJsonStatement(yieldBreakStatement);
            statement.AddJsonValue("yield-keyword", GetKeyword(YieldBreakStatement.YieldKeywordRole));
            statement.AddJsonValue("break-keyword", GetKeyword(YieldBreakStatement.BreakKeywordRole));
            Push(statement);
        }

        public void VisitYieldReturnStatement(YieldReturnStatement yieldReturnStatement)
        {
            JsonObject statement = CreateJsonStatement(yieldReturnStatement);
            statement.AddJsonValue("yield-keyword", GetKeyword(YieldReturnStatement.YieldKeywordRole));
            statement.AddJsonValue("return-keyword", GetKeyword(YieldReturnStatement.ReturnKeywordRole));
            statement.AddJsonValue("expression", GenExpression(yieldReturnStatement.Expression));

            Push(statement);
        }

        #endregion

        #region TypeMembers
        public void VisitAccessor(Accessor accessor)
        {
            JsonObject visitAccessor = CreateJsonEntityDeclaration(accessor);

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
        }

        public void VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration)
        {
            ClearLocalSymbolReferenecs();
            JsonObject construct = CreateJsonEntityDeclaration(constructorDeclaration);

            TypeDeclaration type = constructorDeclaration.Parent as TypeDeclaration;
            if (type != null && type.Name != constructorDeclaration.Name)
            {
                construct.AddJsonValue("type", GetIdentifier((Identifier)type.NameToken.Clone()));
            }
            else
            {
                construct.AddJsonValue("name", new JsonElement(constructorDeclaration.Name));
            }

            construct.AddJsonValue("parameters", GetCommaSeparatedList(constructorDeclaration.Parameters));

            if (!constructorDeclaration.Initializer.IsNull)
            {
                constructorDeclaration.Initializer.AcceptVisitor(this);
                construct.AddJsonValue("initializer", Pop());
            }
            construct.AddJsonValue("body", GetMethodBody(constructorDeclaration.Body));
            construct.AddJsonValue("local_symbols", GetLocalSymbolReferences());

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
        }

        public void VisitDestructorDeclaration(DestructorDeclaration destructorDeclaration)
        {
            JsonObject declaration = CreateJsonEntityDeclaration(destructorDeclaration);
            AddKeyword(declaration, DestructorDeclaration.TildeRole);
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
        }

        public void VisitEnumMemberDeclaration(EnumMemberDeclaration enumMemberDeclaration)
        {
            JsonObject declaration = CreateJsonEntityDeclaration(enumMemberDeclaration);
            declaration.AddJsonValue("identifier", GetIdentifier(enumMemberDeclaration.NameToken));
            if (!enumMemberDeclaration.Initializer.IsNull)
            {
                declaration.AddJsonValue("assign-role", GetKeyword(Roles.Assign));
                declaration.AddJsonValue("initializer", GenExpression(enumMemberDeclaration.Initializer));
            }

            Push(declaration);
        }

        public void VisitEventDeclaration(EventDeclaration eventDeclaration)
        {
            JsonObject declaration = CreateJsonEntityDeclaration(eventDeclaration);

            AddKeyword(declaration, EventDeclaration.EventKeywordRole);
            declaration.AddJsonValue("variables", GetCommaSeparatedList(eventDeclaration.Variables));
            Push(declaration);
        }

        public void VisitCustomEventDeclaration(CustomEventDeclaration customEventDeclaration)
        {
            JsonObject declaration = CreateJsonEntityDeclaration(customEventDeclaration);

            AddKeyword(declaration, EventDeclaration.EventKeywordRole);
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
        }

        public void VisitFieldDeclaration(FieldDeclaration fieldDeclaration)
        {
            JsonObject declaration = CreateJsonEntityDeclaration(fieldDeclaration);
            declaration.AddJsonValue("variables", GetCommaSeparatedList(fieldDeclaration.Variables));
            Push(declaration);
        }

        public void VisitFixedFieldDeclaration(FixedFieldDeclaration fixedFieldDeclaration)
        {
            JsonObject declaration = CreateJsonEntityDeclaration(fixedFieldDeclaration);


            AddKeyword(declaration, FixedFieldDeclaration.FixedKeywordRole);
            declaration.AddJsonValue("variables", GetCommaSeparatedList(fixedFieldDeclaration.Variables));

            Push(declaration);
            throw new FirstTimeUseException();
        }

        public void VisitFixedVariableInitializer(FixedVariableInitializer fixedVariableInitializer)
        {
            JsonObject initializer = new JsonObject();
            initializer.Comment = "VisitFixedVariableInitializer";
            initializer.AddJsonValue("identifier", GetIdentifier(fixedVariableInitializer.NameToken));
            if (!fixedVariableInitializer.CountExpression.IsNull)
            {

                initializer.AddJsonValue("count-expression", GenExpression(fixedVariableInitializer.CountExpression));
            }

            Push(initializer);
            throw new FirstTimeUseException();
        }

        public void VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration)
        {
            ClearLocalSymbolReferenecs();
            JsonObject declaration = CreateJsonEntityDeclaration(indexerDeclaration);
            declaration.AddJsonValue("private-implementation-type", GetPrivateImplementationType(indexerDeclaration.PrivateImplementationType));

            AddKeyword(declaration, IndexerDeclaration.ThisKeywordRole);
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

            declaration.AddJsonValue("local_symbols", GetLocalSymbolReferences());
            Push(declaration);

        }

        public void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
        {
            ClearLocalSymbolReferenecs();
            JsonObject method = CreateJsonEntityDeclaration(methodDeclaration);
            method.AddJsonValue("name", new JsonElement(methodDeclaration.Name));
            //write parameters
            //ParameterDeclaration param = new ParameterDeclaration("test", ParameterModifier.None);
            //PrimitiveType type = new PrimitiveType("string");
            //param.Type = type;
            //methodDeclaration.AddChild(param, Roles.Parameter);
            method.AddJsonValue("parameters", GetCommaSeparatedList(methodDeclaration.Parameters));
            //write body
            method.AddJsonValue("body", GetMethodBody(methodDeclaration.Body));
            //write method type info 
            method.AddJsonValue("local_symbols", GetLocalSymbolReferences());
            Push(method);
        }

        public void VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration)
        {
            ClearLocalSymbolReferenecs();
            JsonObject declaration = CreateJsonEntityDeclaration(operatorDeclaration);
            if (operatorDeclaration.OperatorType == OperatorType.Explicit)
            {
                //essential
                declaration.AddJsonValue("keyword", GetKeyword(OperatorDeclaration.ExplicitRole));
            }
            else if (operatorDeclaration.OperatorType == OperatorType.Implicit)
            {
                //essential
                declaration.AddJsonValue("keyword", GetKeyword(OperatorDeclaration.ImplicitRole));
            }
            else
            {
                //operatorDeclaration.ReturnType.AcceptVisitor(this);
                //declaration.AddJsonValue("return-type", Pop());
            }
            declaration.AddJsonValue("operator-keyword", GetKeyword(OperatorDeclaration.OperatorKeywordRole));
            if (operatorDeclaration.OperatorType == OperatorType.Explicit
                || operatorDeclaration.OperatorType == OperatorType.Implicit)
            {
                //operatorDeclaration.ReturnType.AcceptVisitor(this);
                //declaration.AddJsonValue("return-type", Pop());
            }
            else
            {
                declaration.AddJsonValue("operator-type", new JsonElement(OperatorDeclaration.GetToken(operatorDeclaration.OperatorType)));
            }
            declaration.AddJsonValue("parameters", GetCommaSeparatedList(operatorDeclaration.Parameters));
            declaration.AddJsonValue("body", GetMethodBody(operatorDeclaration.Body));
            declaration.AddJsonValue("local_symbols", GetLocalSymbolReferences());
            Push(declaration);
        }

        public void VisitParameterDeclaration(ParameterDeclaration parameterDeclaration)
        {
            JsonObject parameter = new JsonObject();
            parameter.Comment = "VisitParameterDeclaration";
            if (parameterDeclaration.Attributes.Count > 0)
            {
                parameter.AddJsonValue("attributes", GetAttributes(parameterDeclaration.Attributes));
            }
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
            if (keyword != null)
            {
                parameter.AddJsonValue("modifier", keyword);
            }
            parameter.AddJsonValue("type-info", GenTypeInfo(parameterDeclaration.Type));
            parameter.AddJsonValue("name", parameterDeclaration.Name);

            if (!parameterDeclaration.DefaultExpression.IsNull)
            {
                parameter.AddJsonValue("default-expression", GenExpression(parameterDeclaration.DefaultExpression));
            }

            Push(parameter);
        }

        public void VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
        {
            ClearLocalSymbolReferenecs();
            JsonObject declaration = CreateJsonEntityDeclaration(propertyDeclaration);
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
            declaration.AddJsonValue("local_symbols", GetLocalSymbolReferences());
            Push(declaration);
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
                variable.AddJsonValue("initializer", GenExpression(variableInitializer.Initializer));
            }
            else
            {
                variable.AddJsonNull("initializer");
            }
            Push(variable);
        }
        public void VisitSimpleType(SimpleType simpleType)
        {
            Push(GetTypeIndex(simpleType.IdentifierToken.Name));
        }
        JsonElement GetTypeIndex(string typeKeyword)
        {
            int index = RegisterType(typeKeyword);
            return new JsonElement(index);
        }

        public void VisitSyntaxTree(SyntaxTree syntaxTree)
        {
            JsonArray arr = new JsonArray();
            arr.Comment = "VisitSyntaxTree";
            int counter = 0;
            CreateGlobalSymbolTable();

            foreach (AstNode node in syntaxTree.Children)
            {
                node.AcceptVisitor(this);
                arr.AddJsonValue(Pop());
                counter++;
            }


            //-----------
            //type reference table
            JsonObject symbolInformations = new JsonObject();
            JsonArray typerefs = new JsonArray();
            symbolInformations.AddJsonValue("typerefs", typerefs);
            foreach (string k in this.typeReferences.Keys)
            {
                //type reference
                typerefs.AddJsonValue(new JsonElement(k));
            }
            arr.AddJsonValue(symbolInformations);
            //-----------

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
            memtype.AddJsonValue("type-info", GenTypeInfo(memberType.Target));
            memtype.AddJsonValue("member-name", GetIdentifier(memberType.MemberNameToken));
            Push(memtype);
        }

        public void VisitComposedType(ComposedType composedType)
        {
            JsonObject jsonComposedType = new JsonObject();
            jsonComposedType.Comment = "VisitComposedType";
            composedType.BaseType.AcceptVisitor(this);
            jsonComposedType.AddJsonValue("basetype", Pop());
            if (composedType.HasNullableSpecifier)
            {
                jsonComposedType.AddJsonValue("nullable-specifier", ComposedType.NullableRole.Token);
            }
            jsonComposedType.AddJsonValue("pointerrank", composedType.PointerRank);
            JsonArray arraySpecifier = new JsonArray();
            foreach (var node in composedType.ArraySpecifiers)
            {
                node.AcceptVisitor(this);
                arraySpecifier.AddJsonValue(Pop());
            }
            Push(jsonComposedType);
        }

        public void VisitArraySpecifier(ArraySpecifier arraySpecifier)
        {
            JsonObject arrSpec = new JsonObject();
            arrSpec.Comment = "VisitArraySpecifier";
            arrSpec.AddJsonValue("array-specifier", arraySpecifier.GetChildrenByRole(Roles.Comma).Count);
            if (arraySpecifier.GetChildrenByRole(Roles.Comma).Count == 0)
            {
                arrSpec = null;
            }
            Push(arrSpec);
        }

        public void VisitPrimitiveType(PrimitiveType primitiveType)
        {
            Push(GetTypeIndex(primitiveType.Keyword));
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
            throw new FirstTimeUseException();
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
            throw new FirstTimeUseException();
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

        }

        public void VisitConstraint(Constraint constraint)
        {
            JsonObject visit = new JsonObject();
            visit.Comment = "VisitConstraint";

            AddKeyword(visit, Roles.WhereKeyword);
            visit.AddJsonValue("type-parameter", GenTypeInfo(constraint.TypeParameter));
            visit.AddJsonValue("base-types", GetCommaSeparatedList(constraint.BaseTypes));

            Push(visit);
        }
        int tokennodeCounter = 0;
        public void VisitCSharpTokenNode(CSharpTokenNode cSharpTokenNode)
        {
            tokennodeCounter++;
            if (tokennodeCounter == 26)
            {

            }
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

