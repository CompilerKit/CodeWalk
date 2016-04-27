//MIT, 2016, Brezza92, EngineKit


using System;
using ICSharpCode.NRefactory.CSharp;
using System.Collections.Generic;
using ManualILSpy.Extention.Json;


namespace ManualILSpy.Extention
{
    partial class AstCsToJsonVisitor : IAstVisitor
    {

        Dictionary<object, SemanticSymbol> memberReferences = new Dictionary<object, SemanticSymbol>();
        Dictionary<object, SemanticSymbol> localSymbolReferenceTable = new Dictionary<object, SemanticSymbol>();

        SemanticSymbol GetMemberSymbolReference(Mono.Cecil.FieldDefinition fieldDef)
        {
            SemanticSymbol fieldSymbol;
            if (!memberReferences.TryGetValue(fieldDef, out fieldSymbol))
            {

                int id = memberReferences.Count;
                fieldSymbol = new SemanticSymbol(id);
                fieldSymbol.Kind = SemanticSymbolKind.Field;
                fieldSymbol.OriginalSymbol = fieldDef;
                fieldSymbol.FullSymbolName = fieldDef.FullName;
                fieldSymbol.FullTypeName = fieldDef.FieldType.FullName;
                fieldSymbol.TypeNameIndex = RegisterType(fieldDef.FieldType.FullName);
                memberReferences.Add(id, fieldSymbol);
                //--------------------------------------------
            }
            return fieldSymbol;
        }
        SemanticSymbol GetMemberSymbolReference(Mono.Cecil.MethodDefinition methodDef)
        {
            SemanticSymbol methodSymbol;
            if (!memberReferences.TryGetValue(methodDef, out methodSymbol))
            {
                int id = memberReferences.Count;
                methodSymbol = new SemanticSymbol(id);
                methodSymbol.Kind = SemanticSymbolKind.Method;
                methodSymbol.OriginalSymbol = methodDef;
                methodSymbol.FullSymbolName = methodDef.FullName;
                switch (methodDef.Name)
                {
                    case ".ctor":
                        methodSymbol.FullTypeName = methodDef.DeclaringType.FullName;
                        methodSymbol.TypeNameIndex = RegisterType(methodDef.DeclaringType.FullName);
                        break;
                    case ".cctor":
                    default:
                        methodSymbol.FullTypeName = methodDef.ReturnType.FullName;
                        methodSymbol.TypeNameIndex = RegisterType(methodDef.ReturnType.FullName);
                        break;
                }
                memberReferences.Add(id, methodSymbol);
                //--------------------------------------------
            }
            return methodSymbol;
        }
        SemanticSymbol GetMemberSymbolReference(Mono.Cecil.MethodReference methodDef)
        {
            SemanticSymbol methodSymbol;
            if (!memberReferences.TryGetValue(methodDef, out methodSymbol))
            {
                int id = memberReferences.Count;
                methodSymbol = new SemanticSymbol(id);
                methodSymbol.Kind = SemanticSymbolKind.Method;
                methodSymbol.OriginalSymbol = methodDef;
                methodSymbol.FullSymbolName = methodDef.FullName;

                switch (methodDef.Name)
                {
                    case ".ctor":
                        methodSymbol.FullTypeName = methodDef.DeclaringType.FullName;
                        methodSymbol.TypeNameIndex = RegisterType(methodDef.DeclaringType.FullName);
                        break;
                    case ".cctor":
                    default:
                        methodSymbol.FullTypeName = methodDef.ReturnType.FullName;
                        methodSymbol.TypeNameIndex = RegisterType(methodDef.ReturnType.FullName);
                        break;
                }
                memberReferences.Add(id, methodSymbol);
                //--------------------------------------------
            }
            return methodSymbol;
        }
        SemanticSymbol GetMemberSymbolReference(Mono.Cecil.PropertyReference propertyRef)
        {
            SemanticSymbol propertySymbol;
            if (!memberReferences.TryGetValue(propertyRef, out propertySymbol))
            {
                int id = memberReferences.Count;
                propertySymbol = new SemanticSymbol(id);
                propertySymbol.Kind = SemanticSymbolKind.Property;
                propertySymbol.OriginalSymbol = propertyRef;

                propertySymbol.FullSymbolName = propertyRef.ToString();
                propertySymbol.FullTypeName = propertyRef.PropertyType.FullName;
                propertySymbol.TypeNameIndex = RegisterType(propertyRef.PropertyType.FullName);

                memberReferences.Add(id, propertySymbol);
            }
            return propertySymbol;
        }

        SemanticSymbol GetLocalVarReference(ICSharpCode.Decompiler.ILAst.ILVariable variable)
        {
            SemanticSymbol localSymbol;
            if (!localSymbolReferenceTable.TryGetValue(variable, out localSymbol))
            {
                int id = localSymbolReferenceTable.Count;
                localSymbol = new SemanticSymbol(id);
                //registerto type member symbol table
                int typeIndex = RegisterType(variable.Type.FullName);
                localSymbol.TypeNameIndex = typeIndex;
                localSymbol.FullTypeName = variable.Type.FullName;
                localSymbol.OriginalSymbol = variable;
                if (variable.IsParameter)
                {
                    localSymbol.Kind = SemanticSymbolKind.MethodParameter;
                }
                else
                {
                    localSymbol.Kind = SemanticSymbolKind.LocalVar;
                }

                localSymbolReferenceTable.Add(variable, localSymbol);
            }
            return localSymbol;
        }
        /// <summary>
        /// clear symbol table for type member
        /// </summary>
        void ClearLocalSymbolReferenecs()
        {
            localSymbolReferenceTable.Clear();
        }
        JsonArray GetLocalSymbolReferences()
        {
            JsonArray jsonArray = new JsonArray();
            foreach (SemanticSymbol s in localSymbolReferenceTable.Values)
            {
                //info about 
                switch (s.Kind)
                {
                    case SemanticSymbolKind.LocalVar:
                        {

                            ICSharpCode.Decompiler.ILAst.ILVariable localvar = (ICSharpCode.Decompiler.ILAst.ILVariable)s.OriginalSymbol;
                            JsonObject localVarSymbol = new JsonObject();
                            localVarSymbol.AddJsonValue("kind", "local");
                            localVarSymbol.AddJsonValue("name", localvar.Name);
                            if (localvar.OriginalVariable != null)
                            {
                                localVarSymbol.AddJsonValue("original", localvar.OriginalVariable.ToString());
                            }
                            else
                            {
                                localVarSymbol.AddJsonValue("original", "null");
                            }
                            jsonArray.AddJsonValue(localVarSymbol);
                        }

                        break;
                    case SemanticSymbolKind.MethodParameter:
                        {
                            ICSharpCode.Decompiler.ILAst.ILVariable par = (ICSharpCode.Decompiler.ILAst.ILVariable)s.OriginalSymbol;

                            JsonObject localVarSymbol = new JsonObject();
                            localVarSymbol.AddJsonValue("kind", "par");
                            localVarSymbol.AddJsonValue("name", par.Name);
                            localVarSymbol.AddJsonValue("original", par.OriginalParameter.ToString());
                            jsonArray.AddJsonValue(localVarSymbol);
                        }
                        break;
                    default:
                        throw new NotSupportedException();
                }


            }
            return jsonArray;
        }
        JsonObject GenSemanticSymbol(System.Collections.Generic.IEnumerable<object> anotations)
        {
            JsonObject semanticSymbol = new JsonObject();
            bool foundSymbol = false;
            ICSharpCode.Decompiler.Ast.TypeInformation decompiler_astTypeInfo = null;

            foreach (object ano in anotations)
            {
                //what is this object
#if DEBUG
                Type typeOfObject = ano.GetType();
#endif
                if (ano is Mono.Cecil.FieldDefinition)
                {
                    if (foundSymbol)
                    {   //double symbols?
                        throw new NotSupportedException();
                    }
                    foundSymbol = true;
                    SemanticSymbol fieldSymbol = GetMemberSymbolReference((Mono.Cecil.FieldDefinition)ano);
                    //write field type info 
                    semanticSymbol.AddJsonValue("t_index", fieldSymbol.TypeNameIndex);
                    semanticSymbol.AddJsonValue("t_info", fieldSymbol.FullTypeName);
                    semanticSymbol.AddJsonValue("kind", "field");
                    semanticSymbol.AddJsonValue("ref_index", fieldSymbol.Index);
                }
                else if (ano is Mono.Cecil.MethodDefinition)
                {
                    if (foundSymbol)
                    {   //double symbols?
                        throw new NotSupportedException();
                    }
                    foundSymbol = true;

                    SemanticSymbol methodSymbol = GetMemberSymbolReference((Mono.Cecil.MethodDefinition)ano);
                    //write field type info
                    //TODO: review here
                    semanticSymbol.AddJsonValue("t_index", -1);
                    semanticSymbol.AddJsonValue("t_info", "");
                    semanticSymbol.AddJsonValue("kind", "method");
                    semanticSymbol.AddJsonValue("method", methodSymbol.Index);

                }
                else if (ano is Mono.Cecil.MethodReference)
                {
                    if (foundSymbol)
                    {   //double symbols?
                        throw new NotSupportedException();
                    }
                    foundSymbol = true;

                    Mono.Cecil.MethodReference metRef = (Mono.Cecil.MethodReference)ano;

                    SemanticSymbol methodSymbol = GetMemberSymbolReference((Mono.Cecil.MethodReference)ano);
                    semanticSymbol.AddJsonValue("t_index", methodSymbol.TypeNameIndex);//return type of method
                    semanticSymbol.AddJsonValue("t_info", methodSymbol.FullTypeName);//return type of method 
                    switch (metRef.Name)
                    {
                        case ".ctor":
                            semanticSymbol.AddJsonValue("kind", ".ctor");
                            semanticSymbol.AddJsonValue(".ctor", methodSymbol.Index);
                            break;
                        case ".cctor":
                            semanticSymbol.AddJsonValue("kind", ".cctor");
                            semanticSymbol.AddJsonValue(".cctor", methodSymbol.Index);
                            break;
                        default:
                            semanticSymbol.AddJsonValue("kind", "method");
                            semanticSymbol.AddJsonValue("method", methodSymbol.Index);
                            break;
                    }
                }
                else if (ano is ICSharpCode.Decompiler.ILAst.ILVariable)
                {
                    if (foundSymbol)
                    {   //double symbols?
                        throw new NotSupportedException();
                    }
                    foundSymbol = true;
                    //registerto type member symbol table 
                    SemanticSymbol varSymbol = GetLocalVarReference((ICSharpCode.Decompiler.ILAst.ILVariable)ano);
                    semanticSymbol.AddJsonValue("t_index", varSymbol.TypeNameIndex);
                    semanticSymbol.AddJsonValue("t_info", varSymbol.FullSymbolName);
                    if (varSymbol.Kind == SemanticSymbolKind.MethodParameter)
                    {
                        semanticSymbol.AddJsonValue("kind", "par");
                        semanticSymbol.AddJsonValue("par", varSymbol.Index);
                    }
                    else
                    {
                        semanticSymbol.AddJsonValue("kind", "localvar");
                        semanticSymbol.AddJsonValue("var", varSymbol.Index);
                    }
                }
                else if (ano is Mono.Cecil.PropertyDefinition)
                {
                    //just skip *** 
                    //if (foundSymbol)
                    //{   //double symbols?
                    //    throw new NotSupportedException();
                    //}
                    //foundSymbol = true;

                    //Mono.Cecil.PropertyDefinition propdef = (Mono.Cecil.PropertyDefinition)ano;
                    //int typeIndex = GetTypeIndex(propdef.PropertyType.FullName);
                    //semanticSymbol.AddJsonValue("t_index", typeIndex);
                    //semanticSymbol.AddJsonValue("t_info", propdef.PropertyType.FullName);
                    //semanticSymbol.AddJsonValue("kind", "propertydef");
                    //semanticSymbol.AddJsonValue("property", propdef.FullName);



                }
                else if (ano is Mono.Cecil.IMemberDefinition)
                {
                    throw new NotSupportedException();
                }
                else if (ano is ICSharpCode.Decompiler.Ast.TypeInformation)
                {
                    decompiler_astTypeInfo = (ICSharpCode.Decompiler.Ast.TypeInformation)ano;
                }
            }
            //========
            //second chance ***
            if (!foundSymbol && decompiler_astTypeInfo != null)
            {
                if (decompiler_astTypeInfo.ExpectedType != null)
                {
                    foundSymbol = true;

                    int typeIndex = RegisterType(decompiler_astTypeInfo.ExpectedType.FullName);
                    semanticSymbol.AddJsonValue("t_index", typeIndex);
                    semanticSymbol.AddJsonValue("t_info", decompiler_astTypeInfo.ExpectedType.FullName);
                    semanticSymbol.AddJsonValue("kind", "expected_type");
                }
                else if (decompiler_astTypeInfo.InferredType != null)
                {
                    foundSymbol = true;

                    int typeIndex = RegisterType(decompiler_astTypeInfo.InferredType.FullName);
                    semanticSymbol.AddJsonValue("t_index", typeIndex);
                    semanticSymbol.AddJsonValue("t_info", decompiler_astTypeInfo.InferredType.FullName);
                    semanticSymbol.AddJsonValue("kind", "inferred_type");
                }
                else
                {
                    throw new NotSupportedException();
                }

            }

            if (!foundSymbol)
            {
                //may be void ?
                int typeIndex = RegisterType("System.Void");
                semanticSymbol.AddJsonValue("t_index", typeIndex);
                semanticSymbol.AddJsonValue("t_info", "System.Void");
                semanticSymbol.AddJsonValue("kind", "inferred_type_void");
            }
            return semanticSymbol;
        }
        /// <summary>
        /// add type information of the expression to jsonobject
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="jsonObject"></param>
        void AddTypeInformation(JsonObject jsonObject, Expression expression)
        {
            //record symbols here
            jsonObject.AddJsonValue("symbol", GenSemanticSymbol(expression.Annotations));
        }

        static int visitCount;

        void AddVisitComment<T>(JsonObject jsonObject)
        {

            int vcount = System.Threading.Interlocked.Increment(ref visitCount);
            jsonObject.Comment = "Visit" + typeof(T).Name + " " + (vcount);
            if (vcount == 226)
            {
            }
            //jsonObject.Comment = (vcount).ToString();
        }
        JsonObject CreateJsonExpression<T>(T expression)
        where T : Expression
        {
            JsonObject jsonObject = new JsonObject();
            //1. add visit comment
            AddVisitComment<T>(jsonObject);
            //2. cs spec expression type
            jsonObject.AddJsonValue("expression-type", CsSpecAstName.GetCsSpecName<T>());
            //3. add type info of the expression
            AddTypeInformation(jsonObject, expression);

            return jsonObject;
        }


        JsonValue GenExpression(Expression expression)
        {
            expression.AcceptVisitor(this);
            return Pop();
        }
        JsonValue GenTypeInfo(AstType astType)
        {
            astType.AcceptVisitor(this);
            return Pop();
        }
        JsonValue GenStatement(Statement stmt)
        {
            stmt.AcceptVisitor(this);
            return Pop();
        }
        JsonObject CreateJsonEntityDeclaration<T>(T entityDecl)
        where T : EntityDeclaration
        {
            JsonObject jsonEntityDecl = new JsonObject();
            AddVisitComment<T>(jsonEntityDecl);
            AddAttributes(jsonEntityDecl, entityDecl);
            AddModifiers(jsonEntityDecl, entityDecl);
            AddReturnType(jsonEntityDecl, entityDecl);
            return jsonEntityDecl;
        }

        void AddReturnType(JsonObject jsonObject, EntityDeclaration entityDecl)


        {
            jsonObject.AddJsonValue("return-type", GenTypeInfo(entityDecl.ReturnType));
        }
        void AddModifiers(JsonObject jsonObject, EntityDeclaration entityDecl)
        {
            jsonObject.AddJsonValue("modifiers", GetModifiers(entityDecl.ModifierTokens));
        }
        void AddAttributes(JsonObject jsonObject, EntityDeclaration entityDecl)
        {
            if (entityDecl.Attributes.Count > 0)
            {
                //no attrs
                jsonObject.AddJsonValue("attributes", GetAttributes(entityDecl.Attributes));
            }

        }

        JsonObject CreateJsonStatement<T>(T statement)
            where T : Statement
        {
            JsonObject jsonEntityDecl = new JsonObject();
            AddVisitComment<T>(jsonEntityDecl);
            jsonEntityDecl.AddJsonValue("statement-type", CsSpecAstName.GetCsSpecName<T>());
            return jsonEntityDecl;
        }
        void AddKeyword(JsonObject jsonObject, TokenRole tkrole)


        {
            //we may skip this  ...
            ///jsonObject.AddJsonValue("keyword", GetKeyword(tkrole));
        }
        void AddKeyword(JsonObject jsonObject, string keyName, TokenRole tkrole)
        {
            //we may skip this  ...
            ///jsonObject.AddJsonValue("keyword", GetKeyword(tkrole));
            ///visitCatch.AddJsonValue(keyName, GetKeyword(CatchClause.CatchKeywordRole));
        }
    }



}


