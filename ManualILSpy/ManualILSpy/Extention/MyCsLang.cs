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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.Decompiler.Ast.Transforms;
using ICSharpCode.ILSpy.Options;
using ICSharpCode.ILSpy.XmlDoc;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;
using ManualILSpy.Extention;
using ManualILSpy.Extention.Json;

namespace ICSharpCode.ILSpy
{
    public class MyCsLang : CSharpLanguage
    {

        public JsonValue result;
        void GenerateAstJson(AstBuilder astBuilder, ITextOutput output)
        {
            var visitor = new AstCsToJsonVisitor(output);
            astBuilder.SyntaxTree.AcceptVisitor(new InsertParenthesesVisitor { InsertParenthesesForReadability = true });
            astBuilder.SyntaxTree.AcceptVisitor(visitor);
            AstCsToJsonVisitor visit = visitor as AstCsToJsonVisitor;
            if (visit != null)
            {
                result = visit.LastValue;
            }
            else
            {
                result = null;
            } 
        }
        protected override void InnerGenerateCode(AstBuilder astBuilder, ITextOutput output)
        {
            if (output is StringBuilderTextOutput)
            {
                GenerateAstJson(astBuilder, output);
            }
            else
            {
                base.InnerGenerateCode(astBuilder, output);
            }
        }
    }

}