//MIT, 2016, Brezza27, EngineKit


using System;
using ICSharpCode.NRefactory.CSharp;
using ManualILSpy.Extention.Json;


namespace ManualILSpy.Extention
{
    partial class AstCsToJsonVisitor : IAstVisitor
    {

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

                    Mono.Cecil.FieldDefinition fieldDef = (Mono.Cecil.FieldDefinition)ano;
                    //write field type info
                    int typeIndex = GetTypeIndex(fieldDef.FieldType.FullName);
                    semanticSymbol.AddJsonValue("t_index", typeIndex);
                    semanticSymbol.AddJsonValue("t_info", fieldDef.FieldType.FullName);
                    //symbol  
                    semanticSymbol.AddJsonValue("kind", "field");
                    semanticSymbol.AddJsonValue("field", fieldDef.ToString());
                }
                else if (ano is Mono.Cecil.MethodDefinition)
                {
                    if (foundSymbol)
                    {   //double symbols?
                        throw new NotSupportedException();
                    }
                    foundSymbol = true;

                    Mono.Cecil.MethodDefinition methodef = (Mono.Cecil.MethodDefinition)ano;
                    //write field type info
                    //TODO: review here
                    semanticSymbol.AddJsonValue("t_index", -1);
                    semanticSymbol.AddJsonValue("t_info", "");

                    semanticSymbol.AddJsonValue("kind", "method");
                    semanticSymbol.AddJsonValue("method", methodef.ToString());

                }
                else if (ano is ICSharpCode.Decompiler.ILAst.ILVariable)
                {
                    if (foundSymbol)
                    {   //double symbols?
                        throw new NotSupportedException();
                    }
                    foundSymbol = true;

                    ICSharpCode.Decompiler.ILAst.ILVariable variable = (ICSharpCode.Decompiler.ILAst.ILVariable)ano;
                    int typeIndex = GetTypeIndex(variable.Type.FullName);
                    semanticSymbol.AddJsonValue("t_index", typeIndex);
                    semanticSymbol.AddJsonValue("t_info", variable.Type.FullName);
                    if (variable.IsParameter)
                    {
                        semanticSymbol.AddJsonValue("kind", "par");
                        semanticSymbol.AddJsonValue("par", variable.OriginalParameter.ToString());
                    }
                    else
                    {
                        semanticSymbol.AddJsonValue("kind", "var");
                        semanticSymbol.AddJsonValue("var", variable.OriginalVariable.ToString());
                    }

                }
                else if (ano is Mono.Cecil.MethodReference)
                {
                    if (foundSymbol)
                    {   //double symbols?
                        throw new NotSupportedException();
                    }
                    foundSymbol = true;
                    Mono.Cecil.MethodReference metRef = (Mono.Cecil.MethodReference)ano;
                    var elementMethod = metRef.GetElementMethod();

#if DEBUG
                    Type t2 = elementMethod.GetType();
#endif  
                    semanticSymbol.AddJsonValue("t_index", -1);
                    semanticSymbol.AddJsonValue("t_info", "");
                    semanticSymbol.AddJsonValue("kind", "methodref");
                    semanticSymbol.AddJsonValue("method", metRef.ToString());
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

                    int typeIndex = GetTypeIndex(decompiler_astTypeInfo.ExpectedType.FullName);
                    semanticSymbol.AddJsonValue("t_index", typeIndex);
                    semanticSymbol.AddJsonValue("t_info", decompiler_astTypeInfo.ExpectedType.FullName);
                    semanticSymbol.AddJsonValue("kind", "expected_type");
                }
                else if (decompiler_astTypeInfo.InferredType != null)
                {
                    foundSymbol = true;

                    int typeIndex = GetTypeIndex(decompiler_astTypeInfo.InferredType.FullName);
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
                int typeIndex = GetTypeIndex("System.Void");
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


