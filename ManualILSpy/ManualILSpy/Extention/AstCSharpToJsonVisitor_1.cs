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
                int typeIndex = GetTypeIndex(expressionType.ExpectedType.FullName);
                jsonObject.AddJsonValue("typeinfo", new JsonElement(typeIndex));
            }
            else
            {
                throw new Exception("typeinfo not found!");
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