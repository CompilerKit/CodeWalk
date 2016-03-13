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
    partial class AstCSharpToJsonVisitor : IAstVisitor
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
                        Mono.Cecil.MethodDefinition methodef = (Mono.Cecil.MethodDefinition)objectAnonation;
                        //write field type info
                        int typeIndex = GetTypeIndex(methodef.ReturnType.FullName);
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

        void AddVisitComment<T>(JsonObject jsonObject)
        {
            jsonObject.Comment = "Visit" + typeof(T).Name;
        }
        JsonObject CreateJsonExpression<T>(T expression)
            where T : Expression
        {
            JsonObject jsonObject = new JsonObject();
            //1. add visit comment
            AddVisitComment<T>(jsonObject);
            //2. add type info
            AddTypeInformation(jsonObject, expression);
            return jsonObject;
        }
    }
}