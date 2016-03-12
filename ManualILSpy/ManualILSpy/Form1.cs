using System;
using System.Collections.Generic;
using System.ComponentModel;

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
        string _initPathFile;
        OpenFileDialog _browseExeOrDll;
        OpenFileDialog _browseOutput;

        public Form1()
        {
            InitializeComponent();
            scan_btn.Enabled = false;
            decompile_panel.Enabled = false;
            enable_rbtn.Checked = true;

            _browseExeOrDll = new OpenFileDialog();
            _browseExeOrDll.Filter = "Execution File|*.exe|Dynamic-link library(dll) file|*.dll";
            _browseOutput = new OpenFileDialog();
            _browseOutput.Filter = "Text Files)|*.txt|Json Files|*.json";

            _initPathFile = @".\default_path.txt";
            ReadDefaultPath();

            debug_path_txt.Text = _debugPath;
            release_path_txt.Text = _releasePath;
        }

        private void ReadDefaultPath()
        {
            if (File.Exists(_initPathFile))
            {
                var text = File.ReadAllLines(_initPathFile);
                _debugPath = text[0];
                _releasePath = text[1];
            }
        }

        private void WriteNewPath()
        {
            string[] paths = new string[] { _debugPath, _releasePath };
            File.WriteAllLines(_initPathFile, paths);
        }

        private void Browse_btn_Click(object sender, EventArgs e)
        {
            _path = BrowsePath(_browseExeOrDll);
            textBox1.Text = _path;
            scan_btn.Enabled = !string.IsNullOrEmpty(textBox1.Text);
        }

        Dictionary<string, TypeDefinition> nodeTypeDef = new Dictionary<string, TypeDefinition>();
        Dictionary<string, TreeNode> nodeTreeNode = new Dictionary<string, TreeNode>();

        sealed class MyAssemblyResolver : IAssemblyResolver
        {
            readonly LoadedAssembly parent;

            public MyAssemblyResolver(LoadedAssembly parent)
            {
                this.parent = parent;
            }
            public AssemblyDefinition Resolve(AssemblyNameReference name)
            {
                var node = parent.LookupReferencedAssembly(name);
                return node != null ? node.AssemblyDefinition : null;
            }
            public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
            {
                var node = parent.LookupReferencedAssembly(name);
                return node != null ? node.AssemblyDefinition : null;
            }
            public AssemblyDefinition Resolve(string fullName)
            {
                var node = parent.LookupReferencedAssembly(fullName);
                return node != null ? node.AssemblyDefinition : null;
            }
            public AssemblyDefinition Resolve(string fullName, ReaderParameters parameters)
            {
                var node = parent.LookupReferencedAssembly(fullName);
                return node != null ? node.AssemblyDefinition : null;
            }
        }

        private void Scan_Click(object sender, EventArgs e)
        {
            nodeTypeDef.Clear();
            nodeTreeNode.Clear();

            DefaultAssemblyResolver asmResolver = new DefaultAssemblyResolver();
            ReaderParameters readPars = new ReaderParameters(ReadingMode.Deferred);
            readPars.AssemblyResolver = asmResolver;
            //temp
            asmResolver.AddSearchDirectory(Path.GetDirectoryName(_path));


            AssemblyDefinition assem = AssemblyDefinition.ReadAssembly(_path, readPars);
            var types = assem.MainModule.Types;
            var nameSpace = assem.MainModule.Types;
            TreeNode node = new TreeNode();
            foreach (TypeDefinition type in types)
            {
                if (nodeTreeNode.TryGetValue(type.Namespace, out node))
                {
                    node.Nodes.Add(type.Name);
                }
                else
                {
                    node = new TreeNode(type.Namespace);
                    node.Nodes.Add(type.Name);
                }
                nodeTreeNode[type.Namespace] = node;
                nodeTypeDef.Add(type.FullName, type);
            }
            List<string> keys = new List<string>(nodeTreeNode.Keys);
            foreach (string key in keys)
            {
                if (nodeTreeNode.TryGetValue(key, out node))
                    treeView1.Nodes.Add(node);
            }
            decompile_panel.Enabled = true;
        }

        private TreeNode GetTreeNodeByCondition(IEnumerable<TypeDefinition> types, string condition)
        {
            TypeDefinition def;
            TreeNode node = new TreeNode(condition);
            foreach (TypeDefinition type in types)
            {
                if (nodeTypeDef.TryGetValue(condition, out def))
                {
                    break;
                }
                if (condition == type.Namespace)
                {
                    treeView1.Nodes.Add(type.FullName);
                    node.Nodes.Add(type.FullName);
                    nodeTypeDef.Add(type.FullName, type);
                }
            }
            return node;
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
            string selected = FindSelectedNode(treeView1.Nodes);
            if (selected == null)
            {
                MessageBox.Show("Please select node.");
                return;
            }
            TypeDefinition type;
            string json = "null";
            if (nodeTypeDef.TryGetValue(selected, out type))
            {
                json = GetJson(type, debug);
            }
            string resultPath = debug ? _debugPath
                                      : _releasePath;

            resultPath = "d:\\WImageTest\\test_output1";

            File.WriteAllText(resultPath, json);
            MessageBox.Show("Success!!");
            System.Diagnostics.Process.Start(resultPath);
        }

        private string FindSelectedNode(TreeNodeCollection collection)
        {
            string selected = null;
            foreach (TreeNode node in collection)
            {
                selected = FindSelected(node);
                if (selected != null)
                {
                    selected = node.Text + '.' + selected;
                    break;
                }
            }
            return selected;
        }

        private string FindSelected(TreeNode node)
        {
            if (node.Nodes != null)
            {
                string selected = null;
                int count = node.Nodes.Count;
                for (int i = 0; i < count; i++)
                {
                    if (node.Nodes[i].IsSelected)
                    {
                        return node.Nodes[i].Text;
                    }
                    else if (node.Nodes[i].Nodes != null)
                    {
                        string temp = FindSelected(node.Nodes[i]);
                        if (temp != null)
                        {
                            selected = node.Text + '.' + temp;
                            return selected;
                        }
                    }
                }
            }
            return null;
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

        string _lastDir;
        private string BrowsePath(OpenFileDialog openFileDialog)
        {
            string path = null;
            DialogResult result = openFileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                openFileDialog.Multiselect = false;
                if (_lastDir != null)
                {
                    openFileDialog.InitialDirectory = _lastDir;
                }
                path = openFileDialog.FileName;
                _lastDir = path;
            }
            else if (result != DialogResult.Cancel)
            {
                MessageBox.Show("Can't Open OpenFileDialog");
            }
            return path;
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
            debug_path_txt.Text = BrowsePath(_browseOutput);
            _debugPath = debug_path_txt.Text;
            WriteNewPath();
        }

        private void release_path_btn_Click(object sender, EventArgs e)
        {
            release_path_txt.Text = BrowsePath(_browseOutput);
            _releasePath = release_path_txt.Text;
            WriteNewPath();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            scan_btn.Enabled = !string.IsNullOrEmpty(textBox1.Text);
        }
    }
}
