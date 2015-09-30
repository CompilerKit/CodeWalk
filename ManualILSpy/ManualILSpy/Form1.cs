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

        private void DecompileField(Language language, FieldDefinition field, ITextOutput output, DecompilationOptions options)
        {
            language.DecompileField(field, output, options);
        }

        private void DecomplieType(Language language, TypeDefinition type, ITextOutput output, DecompilationOptions options)
        {
            language.DecompileType(type, output, options);
        }

        private void DecompileProperty(Language language, PropertyDefinition property, ITextOutput output, DecompilationOptions options)
        {
            language.DecompileProperty(property, output, options);
        }

        private void DecompileNameSpace(Language language, string nameSpace, IEnumerable<TypeDefinition> types, ITextOutput output, DecompilationOptions options)
        {
            language.DecompileNamespace(nameSpace, types, output, options);
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

            JsonWriterVisitor visitor = new JsonWriterVisitor(output);
            visitor.Debug = true;
            StringBuilder builder = new StringBuilder();
            JsonArray typeList = new JsonArray();
            foreach (var type in types)
            {
                JsonObject typeObj = new JsonObject();
                typeObj.Comment = "EnableCommentBtn_Click";
                typeObj.AddJsonValues("namespace", new JsonElement(type.Namespace));
                if (type.Namespace == null || type.Namespace.Length == 0)
                {
                    typeObj = null;
                    continue;
                }
                
                typeObj.AddJsonValues("name", new JsonElement(type.Name));
                var fields = type.Fields;
                JsonArray fieldList = new JsonArray();
                foreach(var field in fields)
                {
                    DecompileField(csharp, field, output, options);
                    fieldList.AddJsonValue(csharp.result);
                }
                if (fieldList.Count == 0)
                {
                    fieldList = null;
                }
                typeObj.AddJsonValues("fields", fieldList);
                var methods = type.Methods;
                JsonArray methodList = new JsonArray();
                foreach (var method in methods)
                {
                    DecompileMethod(csharp, method, output, options);
                    methodList.AddJsonValue(csharp.result);
                }
                if(methodList.Count == 0)
                {
                    methodList = null;
                }
                typeObj.AddJsonValues("methods", methodList);
                typeList.AddJsonValue(typeObj);
            }
            typeList.AcceptVisitor(visitor);
            builder.Append(visitor.ToString());
            string strJson;
            strJson = builder.ToString();
            string path = @"D:\[]Documents\testAstJsonDebug.txt";
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
