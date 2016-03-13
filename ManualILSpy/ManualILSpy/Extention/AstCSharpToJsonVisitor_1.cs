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
        /// <summary>
        /// get type info and push on stack
        /// </summary>
        public void PushWithTypeInfo(JsonObject jsonObject, Expression expression)
        {
            AddTypeInformation(jsonObject, expression);
            Push(jsonObject);
        }
    }
}