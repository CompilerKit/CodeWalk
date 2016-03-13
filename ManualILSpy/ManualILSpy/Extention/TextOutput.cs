//MIT, 2016, Brezza27, EngineKit
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;

namespace ManualILSpy.Extention
{
    public class ManualTextOutput : ITextOutput
    {
        int indent = 0;
        public TextLocation Location
        {
            get
            {
                return new TextLocation(0, 0);
            }
        }

        public void AddDebugSymbols(MethodDebugSymbols methodDebugSymbols)
        {
            //throw new NotImplementedException();
        }

        public void Indent()
        {
            indent++;
        }

        public void MarkFoldEnd()
        {
            throw new NotImplementedException();
        }

        public void MarkFoldStart(string collapsedText = "...", bool defaultCollapsed = false)
        {
            throw new NotImplementedException();
        }

        public void Unindent()
        {
            indent--;
        }

        public void Write(string text)
        {
            Console.Write(text);
        }

        public void Write(char ch)
        {
            Console.Write(ch);
        }

        public void WriteDefinition(string text, object definition, bool isLocal = true)
        {
            Console.Write(text);
        }

        public void WriteLine()
        {
            Console.WriteLine();
        }

        public void WriteReference(string text, object reference, bool isLocal = false)
        {
            Console.Write(text);
        }
    }

    public class StringBuilderTextOutput : ITextOutput
    {
        int indent;
        StringBuilder output;
        public StringBuilderTextOutput()
        {
            output = new StringBuilder();
        }
        public TextLocation Location
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void AddDebugSymbols(MethodDebugSymbols methodDebugSymbols)
        {
            throw new NotImplementedException();
        }

        public void Indent()
        {
            indent++;
        }

        public void MarkFoldEnd()
        {
            throw new NotImplementedException();
        }

        public void MarkFoldStart(string collapsedText = "...", bool defaultCollapsed = false)
        {
            throw new NotImplementedException();
        }

        public void Unindent()
        {
            indent--;
        }

        public void Write(string text)
        {
            output.Append(text);
        }

        public void Write(char ch)
        {
            output.Append(ch);
        }

        public void WriteDefinition(string text, object definition, bool isLocal = true)
        {
            throw new NotImplementedException();
        }

        public void WriteLine()
        {
            output.Append("\r\n");
        }

        public void WriteReference(string text, object reference, bool isLocal = false)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return output.ToString();
        }
    }

    public class UTF8TextWriter : TextWriter
    {
        public override Encoding Encoding
        {
            get
            {
                return Encoding.UTF8;
            }
        }
    }

    public class JsonTokenWriter
    {
        readonly ITextOutput output;
        //readonly DecompilerContext context;
        readonly Stack<AstNode> nodeStack = new Stack<AstNode>();
        //bool firstUsingDeclaration;
        //bool lastUsingDeclaration;

        //TextLocation? lastEndOfLine;
        int counter;
        bool lastIsNewLine;
        public JsonTokenWriter(ITextOutput output)
        {
            this.output = output;
            counter = 0;
            lastIsNewLine = true;
        }

        //public override void EndNode(AstNode node)
        //{
        //    throw new NotImplementedException();
        //}

        //public override void Indent()
        //{
        //    throw new NotImplementedException();
        //}

        public void NewLine()
        {
            if (lastIsNewLine)
                return;
            output.WriteLine();
            lastIsNewLine = true;
        }

        public void Space()
        {
            output.Write(new String('\t', counter));
        }

        public void OpenArrayBrace()
        {
            if (counter > 0)
            {
                WriteLine();
            }
            output.Write('[');
            counter++;
            lastIsNewLine = false;
            WriteLine();
        }

        public void CloseArrayBrace()
        {
            counter--;
            lastIsNewLine = false;
            WriteLine();
            output.Write(']');
        }

        public void OpenObjectBrace()
        {
            if (counter > 0)
            {
                WriteLine();
            }
            output.Write('{');
            counter++;
            lastIsNewLine = false;
            WriteLine();
        }

        public void CloseObjectBrace()
        {
            lastIsNewLine = false;
            counter--;
            WriteLine();
            output.Write('}');
        }

        public void Comma()
        {
            lastIsNewLine = false;
            output.Write(", ");
            WriteLine();
        }

        public void WriteComment(string comment)
        {
            if (comment.Length == 0 || comment == null)
            {
                return;
            }
            WriteLine();
            lastIsNewLine = false;
            output.Write("/*" + comment + "*/");
            WriteLine();
        }

        public void WriteKey(string key)
        {
            lastIsNewLine = false;
            output.Write('"' + key + '"');
            output.Write(" : ");
        }

        public void WriteLine()
        {
            if (lastIsNewLine)
                return;
            output.WriteLine();
            Space();
            lastIsNewLine = true;
        }

        public void WritePairValue(string key, string value)
        {
            lastIsNewLine = false;
            output.Write('"' + key + '"');
            output.Write(" : ");
            //output.Write(value);
            output.Write(value);
        }

        public void WriteValue(string value)
        {
            lastIsNewLine = false;
            output.Write(value);
        }

        public override string ToString()
        {
            return output.ToString();
        }

        //public override void StartNode(AstNode node)
        //{
        //    throw new NotImplementedException();
        //}

        //public override void Unindent()
        //{
        //    throw new NotImplementedException();
        //}

        //public override void WriteComment(CommentType commentType, string content)
        //{
        //    throw new NotImplementedException();
        //}

        //public override void WriteIdentifier(Identifier identifier)
        //{
        //    throw new NotImplementedException();
        //}

        //public override void WriteKeyword(Role role, string keyword)
        //{
        //    output.Write(keyword);
        //}

        //public override void WritePreProcessorDirective(PreProcessorDirectiveType type, string argument)
        //{
        //    throw new NotImplementedException();
        //}

        //public override void WritePrimitiveType(string type)
        //{
        //    throw new NotImplementedException();
        //}

        //public override void WritePrimitiveValue(object value, string literalValue = null)
        //{
        //    throw new NotImplementedException();
        //}

        //public override void WriteToken(Role role, string token)
        //{
        //    throw new NotImplementedException();
        //}
    }

    public class AstJsonWriter : TokenWriter
    {
        ITextOutput output;
        public AstJsonWriter(ITextOutput output)
        {
            this.output = output;
        }

        #region Start and End Node

        public override void EndNode(AstNode node)
        {
            throw new NotImplementedException();
        }

        public override void StartNode(AstNode node)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Indent

        public override void Indent()
        {
            throw new NotImplementedException();
        }

        public override void Unindent()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Line and Space

        public override void NewLine()
        {
            output.WriteLine();
        }

        public override void Space()
        {
            output.Write(' ');
        }

        #endregion

        #region Write Anything

        public override void WriteComment(CommentType commentType, string content)
        {
            throw new NotImplementedException();
        }

        public override void WriteIdentifier(Identifier identifier)
        {
            output.Write('"' + identifier.Name + '"');
        }

        public override void WriteKeyword(Role role, string keyword)
        {

            output.Write('"' + keyword + '"');
        }

        public override void WritePreProcessorDirective(PreProcessorDirectiveType type, string argument)
        {
            throw new NotImplementedException();
        }

        public override void WritePrimitiveType(string type)
        {
            output.Write(type);
        }

        public override void WritePrimitiveValue(object value, string literalValue = null)
        {
            throw new NotImplementedException();
        }

        public override void WriteToken(Role role, string token)
        {
            output.Write(token);
        }
        #endregion

    }

    public static class MyDebugWriter
    {
        public static bool Debuging { get; set; }

        public static void Log(string tag, string text)
        {
            Console.Write(tag + " : " + text);
        }

        public static string LogReturn(string tag, string text)
        {
            string textReturn = "";
            if (Debuging)
            {
                textReturn = tag + " : " + text;
            }
            return textReturn;
        }
    }
}