using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Mono.Cecil;
using ICSharpCode.ILSpy;
using ICSharpCode.Decompiler;
using ManualILSpy.Extention;
using ManualILSpy.Extention.Json;

namespace ManualILSpy
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void DecompileMethod(Language language, MethodDefinition method, ITextOutput output, DecompilationOptions options)
        {
            language.DecompileMethod(method, output, options);
        }

        private void Decomplie()
        {

        }

        public DecompilerSettings LoadDecompilerSettings()
        {
            //XElement e = settings["DecompilerSettings"];
            DecompilerSettings s = new DecompilerSettings();
            s.AnonymousMethods = true;//(bool?)e.Attribute("anonymousMethods") ?? s.AnonymousMethods;
            s.YieldReturn = true; //(bool?)e.Attribute("yieldReturn") ?? s.YieldReturn;
            s.AsyncAwait = true; //(bool?)e.Attribute("asyncAwait") ?? s.AsyncAwait;
            s.QueryExpressions = true; //(bool?)e.Attribute("queryExpressions") ?? s.QueryExpressions;
            s.ExpressionTrees = true; //(bool?)e.Attribute("expressionTrees") ?? s.ExpressionTrees;
            s.UseDebugSymbols = true; //(bool?)e.Attribute("useDebugSymbols") ?? s.UseDebugSymbols;
            s.ShowXmlDocumentation = true;//(bool?)e.Attribute("xmlDoc") ?? s.ShowXmlDocumentation;
            s.FoldBraces = false;//(bool?)e.Attribute("foldBraces") ?? s.FoldBraces;
            return s;
        }

        private void EnableCommentBtn_Click(object sender, EventArgs e)
        {
            AssemblyDefinition assem = AssemblyDefinition.ReadAssembly(@"D:\[]New Project\TestILSpy\TestILSpy\bin\Debug\TestILSpy.exe");
            var types = assem.MainModule.Types;

            StringBuilderTextOutput output = new StringBuilderTextOutput();
            CSharpLanguage csharp = new CSharpLanguage(output);
            DecompilationOptions options = new DecompilationOptions();
            options.DecompilerSettings = LoadDecompilerSettings();

            JsonArray methodList = new JsonArray();
            JsonWriterVisitor visitor = new JsonWriterVisitor(output);
            visitor.Debug = true;
            StringBuilder builder = new StringBuilder();

            foreach (var type in types)
            {
                var methods = type.Methods;
                foreach (var method in methods)
                {
                    DecompileMethod(csharp, method, output, options);
                    methodList.AddJsonValue(csharp.result);
                }
            }
            methodList.AcceptVisitor(visitor);
            builder.Append(visitor.ToString());
            string strJson;
            strJson = builder.ToString();
            //strJson = csharp.writer.ToString();
            string path = @"D:\[]Documents\testAstJsonRelease.txt";
            if (visitor.Debug)
            {
                path = @"D:\[]Documents\testAstJsonDebug.txt";
            }
            File.WriteAllText(path, strJson);
        }

        private void DisableCommentBtn_Click(object sender, EventArgs e)
        {
            AssemblyDefinition assem = AssemblyDefinition.ReadAssembly(@"D:\[]New Project\TestILSpy\TestILSpy\bin\Debug\TestILSpy.exe");
            var types = assem.MainModule.Types;

            StringBuilderTextOutput output = new StringBuilderTextOutput();
            CSharpLanguage csharp = new CSharpLanguage(output);
            DecompilationOptions options = new DecompilationOptions();
            options.DecompilerSettings = LoadDecompilerSettings();

            JsonArray methodList = new JsonArray();
            JsonWriterVisitor visitor = new JsonWriterVisitor(output);
            visitor.Debug = false;
            StringBuilder builder = new StringBuilder();

            foreach (var type in types)
            {
                var methods = type.Methods;
                foreach (var method in methods)
                {
                    DecompileMethod(csharp, method, output, options);
                    methodList.AddJsonValue(csharp.result);
                }
            }
            methodList.AcceptVisitor(visitor);
            builder.Append(visitor.ToString());
            string strJson;
            strJson = builder.ToString();
            //strJson = csharp.writer.ToString();
            string path = @"D:\[]Documents\testAstJsonRelease.txt";
            if (visitor.Debug)
            {
                path = @"D:\[]Documents\testAstJsonDebug.txt";
            }
            File.WriteAllText(path, strJson);
        }

        private void TestReadWriteJsonBtn_Click(object sender, EventArgs e)
        {
            string inputPath = @"D:\[]Documents\testAstJsonRelease.txt";
            string outputPath = @"D:\[]Documents\testWriteJson.txt";

            string jsonString = File.ReadAllText(inputPath);
            JsonReader reader = new JsonReader(jsonString);
            JsonValue value = reader.Read();

            StringBuilderTextOutput output = new StringBuilderTextOutput();
            JsonWriterVisitor writer = new JsonWriterVisitor(output);
            value.AcceptVisitor(writer);

            string jsonOut = writer.ToString();
            File.WriteAllText(outputPath, jsonOut);
        }
    }
}
