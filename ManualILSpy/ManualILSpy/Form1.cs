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
        string _path;
        string _debugPath;
        string _releasePath;
        public Form1()
        {
            InitializeComponent();
            scan_btn.Enabled = false;
            decompile_panel.Enabled = false;
            enable_rbtn.Checked = true;

            _debugPath = @"D:\[]Documents\testAstJsonDebug.json";
            _releasePath = @"D:\[]Documents\testAstJsonRelease.json";
            debug_path_txt.Text = _debugPath;
            release_path_txt.Text = _releasePath;
        }

        private void Browse_btn_Click(object sender, EventArgs e)
        {
            _path = BrowsePath();
            textBox1.Text = _path;
            scan_btn.Enabled = true;
        }

        Dictionary<string, TypeDefinition> nodesTree = new Dictionary<string, TypeDefinition>();
        private void Scan_Click(object sender, EventArgs e)
        {
            nodesTree.Clear();
            AssemblyDefinition assem = AssemblyDefinition.ReadAssembly(_path);
            var types = assem.MainModule.Types;

            TreeNode node = new TreeNode();
            foreach (var type in types)
            {
                treeView1.Nodes.Add(type.FullName);
                nodesTree.Add(type.FullName, type);
            }
            decompile_panel.Enabled = true;
        }

        private void decompile_btn_Click(object sender, EventArgs e)
        {
            if (enable_rbtn.Checked)
            {
                Decompile(true);
            }
            else if (disable_rbtn.Checked)
            {
                Decompile(false);
            }
            else
            {
                MessageBox.Show("Please choose comment setting");
            }
        }

        private void TestReadWriteJsonBtn_Click(object sender, EventArgs e)
        {
            string inputPath = @"D:\[]Documents\testAstJsonRelease.json";
            string outputPath = @"D:\[]Documents\testWriteJson.json";

            string jsonString = File.ReadAllText(inputPath);
            JsonReader reader = new JsonReader(jsonString);
            JsonValue value = reader.Read();

            StringBuilderTextOutput output = new StringBuilderTextOutput();
            JsonWriterVisitor writer = new JsonWriterVisitor(output);
            value.AcceptVisitor(writer);

            string jsonOut = writer.ToString();
            File.WriteAllText(outputPath, jsonOut);
        }

        private void Decompile(bool debug)
        {
            string selected = GetSelectedNode();
            if (selected == null)
            {
                MessageBox.Show("Please select node.");
                return;
            }
            TypeDefinition type;
            string json = "null";
            if(nodesTree.TryGetValue(selected, out type))
            {
                json = GetJson(type, debug);
            }
            string resultPath = debug ? _debugPath
                                      : _releasePath;
            File.WriteAllText(resultPath, json);
            MessageBox.Show("Success!!");
        }

        private string GetSelectedNode()
        {
            string selected = null;
            int count = treeView1.Nodes.Count;
            for (int i = 0; i < count; i++)
            {
                if (treeView1.Nodes[i].IsSelected)
                {
                    selected = treeView1.Nodes[i].Text;
                    break;
                }
            }
            return selected;
        }

        private string GetJson(IMemberDefinition node, bool debug)
        {
            StringBuilderTextOutput output = new StringBuilderTextOutput();
            CSharpLanguage csharp = new CSharpLanguage(output);
            DecompilationOptions options = new DecompilationOptions();
            options.DecompilerSettings = LoadDecompilerSettings();

            JsonWriterVisitor visitor = new JsonWriterVisitor(output);
            visitor.Debug = debug;
            if (node is TypeDefinition)
            {
                DecomplieType(csharp, (TypeDefinition)node, output, options);
                JsonValue value = csharp.result;
                value.AcceptVisitor(visitor);
                return visitor.ToString();
            }
            else
            {
                MessageBox.Show("Not TypeDefinition");
            }
            return null;
        }

        OpenFileDialog _openFileDialog = new OpenFileDialog();
        string _lastDirectory;
        private string BrowsePath()
        {
            string path = null;
            DialogResult result = _openFileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                _openFileDialog.Multiselect = false;
                if (_lastDirectory != null)
                {
                    _openFileDialog.InitialDirectory = _lastDirectory;
                }
                path = _openFileDialog.FileName;
                _lastDirectory = path;
            }
            else if (result != DialogResult.Cancel)
            {
                MessageBox.Show("Can't Open OpenFileDialog");
            }
            return path;
        }

        string Test(string assemPath, bool debuging)
        {
            AssemblyDefinition assem = AssemblyDefinition.ReadAssembly(assemPath);
            var types = assem.MainModule.Types;

            StringBuilderTextOutput output = new StringBuilderTextOutput();
            CSharpLanguage csharp = new CSharpLanguage(output);
            DecompilationOptions options = new DecompilationOptions();
            options.DecompilerSettings = LoadDecompilerSettings();

            JsonWriterVisitor visitor = new JsonWriterVisitor(output);
            visitor.Debug = debuging;
            StringBuilder builder = new StringBuilder();
            JsonArray typeList = new JsonArray();
            foreach (var type in types)
            {
                DecomplieType(csharp, type, output, options);
                typeList.AddJsonValue(csharp.result);
            }
            typeList.AcceptVisitor(visitor);
            builder.Append(visitor.ToString());
            string strJson;
            strJson = builder.ToString();
            return strJson;
            //string path = @"D:\[]Documents\testAstJsonDebug.json";
            //File.WriteAllText(path, strJson);
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

        private void debug_path_txt_TextChanged(object sender, EventArgs e)
        {
            _debugPath = debug_path_txt.Text;
        }

        private void release_path_txt_TextChanged(object sender, EventArgs e)
        {
            _releasePath = release_path_txt.Text;
        }

        private void debug_path_btn_Click(object sender, EventArgs e)
        {
            debug_path_txt.Text = BrowsePath();
            _debugPath = debug_path_txt.Text;
        }

        private void release_path_btn_Click(object sender, EventArgs e)
        {
            release_path_txt.Text = BrowsePath();
            _releasePath = release_path_txt.Text;
        }
    }
}
