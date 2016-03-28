using System;
using System.Collections.Generic;
using System.ComponentModel;

using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using Mono.Cecil;
using ICSharpCode.ILSpy;
using ICSharpCode.Decompiler;
using ManualILSpy.Extention;
using ManualILSpy.Extention.Json;
using System.Threading.Tasks;

namespace ManualILSpy
{
    public partial class Form1 : Form
    {
        string _browsePath;
        private string _saveOutputPath = "d:\\WImageTest\\test_output\\";
        OpenFileDialog _browseExeOrDll;
        OpenFileDialog _browseOutput;

        public Form1()
        {
            InitializeComponent();
            scan_btn.Enabled = false;
            decompile_panel.Enabled = false;
            enable_rbtn.Checked = true;

            browsePathTb.ReadOnly = true;
            browsePathTb.BackColor = System.Drawing.SystemColors.Window;

            _browseExeOrDll = new OpenFileDialog();
            _browseExeOrDll.Filter = "Execution File|*.exe|Dynamic-link library(dll) file|*.dll";
            _browseOutput = new OpenFileDialog();
            _browseOutput.Filter = "Text Files)|*.txt|Json Files|*.json";

            browse_btn.TabIndex = 1;
            scan_btn.TabIndex = 2;
            decompile_btn.TabIndex = 3;
            decompile_all_btn.TabIndex = 4;

            listView1.View = View.Details;
            listView1.GridLines = true;
            listView1.FullRowSelect = true;

            listView1.Columns.Add("Fullname",200);
            listView1.Columns.Add("Status",100);

            listView1.MouseDoubleClick += ListViewMouseDoubleClick;
            errorLogBtn.Enabled = false;

            EnableControlDecompileBtn(false);
        }

        private void ListViewMouseDoubleClick(object sender, MouseEventArgs e)
        {
            var items = listView1.SelectedItems;
            ListViewItem item = items[0];
            if (item != null)
            {
                string fileTarget = item.Text + ".txt";
                try
                {
                    System.Diagnostics.Process.Start(_saveOutputPath + "\\" + fileTarget);
                }
                catch (Exception expection)
                {
                    MessageBox.Show(expection.Message);
                }
                
            }
        }

        private void Browse_btn_Click(object sender, EventArgs e)
        {
            _browsePath = BrowsePath(_browseExeOrDll);
            browsePathTb.Text = _browsePath;
            scan_btn.Enabled = !string.IsNullOrEmpty(browsePathTb.Text);
        }

        Dictionary<string, TypeDefinition> nodeTypeDefs = new Dictionary<string, TypeDefinition>();
        Dictionary<string, TreeNode> nodeTreeNodes = new Dictionary<string, TreeNode>();

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
            nodeTypeDefs.Clear();
            nodeTreeNodes.Clear();
            treeView1.Nodes.Clear();
            
            DefaultAssemblyResolver asmResolver = new DefaultAssemblyResolver();
            ReaderParameters readPars = new ReaderParameters(ReadingMode.Deferred);
            readPars.AssemblyResolver = asmResolver;
            //temp
            asmResolver.AddSearchDirectory(Path.GetDirectoryName(_browsePath));


            AssemblyDefinition assem = AssemblyDefinition.ReadAssembly(_browsePath, readPars);
            var types = assem.MainModule.Types;
            var nameSpace = assem.MainModule.Types;
            TreeNode node = new TreeNode();
            foreach (TypeDefinition type in types)
            {
                if (nodeTreeNodes.TryGetValue(type.Namespace, out node))
                {
                    node.Nodes.Add(type.Name);
                }
                else
                {
                    node = new TreeNode(type.Namespace);
                    node.Nodes.Add(type.Name);
                }
                nodeTreeNodes[type.Namespace] = node;
                nodeTypeDefs.Add(type.FullName, type);
            }
            List<string> keys = new List<string>(nodeTreeNodes.Keys);
            foreach (string key in keys)
            {
                if (nodeTreeNodes.TryGetValue(key, out node))
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
                if (nodeTypeDefs.TryGetValue(condition, out def))
                {
                    break;
                }
                if (condition == type.Namespace)
                {
                    treeView1.Nodes.Add(type.FullName);
                    node.Nodes.Add(type.FullName);
                    nodeTypeDefs.Add(type.FullName, type);
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
            if (nodeTypeDefs.TryGetValue(selected, out type))
            {
                json = GetJson(type, debug);
            }
            string dllFileName = Path.GetFileName(_browsePath);
            if (type != null)
            {
                string resultPath = "d:\\WImageTest\\test_output\\" + dllFileName + "\\" + (debug ? "Debug\\" : "Release\\") + type.FullName + ".txt";
                File.WriteAllText(resultPath, json);
                MessageBox.Show(type.FullName + " Success!!");
            }
            else
            {
                MessageBox.Show("Not Writable Json");
            }
            
        }

        private void DecompileAll(bool debug)
        {
            EnableDecompileBtn(false);
            EnableControlDecompileBtn(true);
            var allTypes = nodeTypeDefs.Values.ToArray();
            string dllFileName = Path.GetFileName(_browsePath);
            string json;

            string resultPath = "d:\\WImageTest\\test_output\\" + dllFileName + "\\" + (debug ? "Debug\\" : "Release\\");
            Directory.CreateDirectory(resultPath);
            StringBuilder unwritable = new StringBuilder();
            _saveOutputPath = resultPath;

            string[] arr = new string[2];
            ListViewItem item;

            int typeCount = allTypes.Count();
            lbCountAll.Text = dllFileName + " have " + typeCount + " types.";

            int successCount = 0;
            int errorCount = 0;
            foreach (TypeDefinition type in allTypes)
            {
                if (type != null)
                {
                    arr[0] = type.FullName;
                    try
                    {
                        string nameSpace = type.Namespace;
                        json = GetJson(type, debug);
                        File.WriteAllText(resultPath + type.FullName + ".txt", json);
                        arr[1] = "Success!!";
                        successCount++;
                    }
                    catch(Exception e)
                    {
                        unwritable.Append(type.FullName + " - [ " + e.Message + " ]\n");
                        arr[1] = e.Message;
                        errorCount++;
                    }

                    item = new ListViewItem(arr);
                    listView1.Items.Add(item);
                    lbSuccessCounter.Text = "Success " + successCount + " / " + typeCount + " types";
                    lbErrorCounter.Text = "Error " + errorCount + " / " + typeCount + " types";
                }
                Application.DoEvents();
                if (isStop)
                    break;
            }
            File.WriteAllText(resultPath+"..\\ErrorLog.txt", unwritable.ToString());
            MessageBox.Show("Decompile Success " + successCount + " files.\nDecompile Error " + errorCount + " files.");
            errorLogBtn.Enabled = true;
            EnableDecompileBtn(true);
            EnableControlDecompileBtn(false);
            isPause = false;
            isStop = false;
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

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            scan_btn.Enabled = !string.IsNullOrEmpty(browsePathTb.Text);
        }

        private void decompile_all_btn_Click(object sender, EventArgs e)
        {
            if (enable_rbtn.Checked)
            {
                DecompileAll(true);
            }
            else if (disable_rbtn.Checked)
            {
                DecompileAll(false);
            }
            else
            {
                MessageBox.Show("Please choose comment setting");
            }
        }

        private void errorLogBtn_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(_saveOutputPath+ "..\\ErrorLog.txt");
        }

        bool isPause = false;
        private void pauseBtn_Click(object sender, EventArgs e)
        {
            if (isPause)
            {
                isPause = false;
                pauseBtn.Text = "Pause";
            }
            else
            {
                isPause = true;
                pauseBtn.Text = "Continue";
            }
        }

        private void EnableDecompileBtn(bool enable)
        {
            browse_btn.Enabled = enable;
            scan_btn.Enabled = enable;
            decompile_panel.Enabled = enable;
            testReadWriteJsonBtn.Enabled = enable;
        }

        bool isStop = false;
        private void stopBtn_Click(object sender, EventArgs e)
        {
            if (!isStop)
            {
                isStop = true;
                EnableControlDecompileBtn(false);
                EnableDecompileBtn(true);
            }
        }

        private void EnableControlDecompileBtn(bool enable)
        {
            pauseBtn.Enabled = false;
            stopBtn.Enabled = enable;
        }
    }
}
