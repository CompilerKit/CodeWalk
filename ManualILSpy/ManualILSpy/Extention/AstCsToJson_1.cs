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
    partial class AstCsToJsonVisitor : IAstVisitor
    {
        /// <summary>
        /// add type information of the expression to jsonobject
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="jsonObject"></param>
        void AddTypeInformation(JsonObject jsonObject, Expression expression)
        {
            var expressionType = expression.Annotation<ICSharpCode.Decompiler.Ast.TypeInformation>();
            if (expressionType != null)
            {
                if (expressionType.ExpectedType != null)
                {
                    int typeIndex = GetTypeIndex(expressionType.ExpectedType.FullName);
                    jsonObject.AddJsonValue("typeinfo", typeIndex);
                }
                else if (expressionType.InferredType != null)
                {
                    int typeIndex = GetTypeIndex(expressionType.InferredType.FullName);
                    jsonObject.AddJsonValue("typeinfo", typeIndex);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else
            {
                object objectAnonation = expression.Annotation<object>();
                if (objectAnonation != null)
                {
                    Type typeOfObject = objectAnonation.GetType();
                    if (objectAnonation is Mono.Cecil.FieldDefinition)
                    {
                        //refer to field
                        Mono.Cecil.FieldDefinition fieldDef = (Mono.Cecil.FieldDefinition)objectAnonation;
                        //write field type info
                        int typeIndex = GetTypeIndex(fieldDef.FieldType.FullName);
                        jsonObject.AddJsonValue("typeinfo", typeIndex);
                    }
                    else if (objectAnonation is Mono.Cecil.MethodDefinition)
                    {
                        //get deletegate type of this method?
                        //TODO: review here
                        Mono.Cecil.MethodDefinition methodef = (Mono.Cecil.MethodDefinition)objectAnonation;
                        //write field type info
                        //TODO: review here
                        jsonObject.AddJsonValue("typeinfo", -1);
                    }
                    else if (objectAnonation is ICSharpCode.Decompiler.ILAst.ILVariable)
                    {
                        ICSharpCode.Decompiler.ILAst.ILVariable variable = (ICSharpCode.Decompiler.ILAst.ILVariable)objectAnonation;
                        int typeIndex = GetTypeIndex(variable.Type.FullName);
                        jsonObject.AddJsonValue("typeinfo", typeIndex);

                    }
                    else {
                        throw new Exception("typeinfo not found!");
                    }
                }
                else
                {
                    //return void ?
                    jsonObject.AddJsonValue("typeinfo", GetTypeIndex("System.Void"));
                }
            }
        }

        static int visitCount;

        void AddVisitComment<T>(JsonObject jsonObject)
        {

            int vcount = System.Threading.Interlocked.Increment(ref visitCount);
            jsonObject.Comment = "Visit" + typeof(T).Name + " " + (vcount);
#if DEBUG
            if (vcount == 16)
            {
            }
#endif
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
            jsonObject.AddJsonValue("attributes", GetAttributes(entityDecl.Attributes));
        }
    }



}
