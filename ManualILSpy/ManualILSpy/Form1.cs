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
        const string DEFAULT_SAVEPATH = "d:\\WImageTest\\test_output\\";
        private string _lastSaveOutputPath = DEFAULT_SAVEPATH;
        OpenFileDialog _browseExeOrDll;
        OpenFileDialog _browseOutput;
        const string _errorLogFile = "ErrorLog.txt";
        const string _errorListFile = "ErrorListFile.txt";
        char[] _specialChar = new char[] { '<', '>' };
        const string JsonExtention = ".json";
        const string CSharpExtention = ".cs";

        delegate void DecompileTypeAny(bool debug);

        public Form1()
        {
            InitializeComponent();
            SetUpComponent();
        }

        string _lastBrowseDir;
        private string BrowsePath(OpenFileDialog openFileDialog)
        {
            string path = null;
            DialogResult result = openFileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                openFileDialog.Multiselect = false;
                if (_lastBrowseDir != null)
                {
                    openFileDialog.InitialDirectory = _lastBrowseDir;
                }
                path = openFileDialog.FileName;
                _lastBrowseDir = path;
            }
            else if (result != DialogResult.Cancel)
            {
                MessageBox.Show("Can't Open OpenFileDialog");
            }
            return path;
        }
        static void EnsureFolder(string rootFolder, string fullFolder)
        {
            //ensure folder and subfodlers existing
            if (!fullFolder.StartsWith(rootFolder))
            {
                throw new NotSupportedException();
            }
            //---------------------------------------
            int rootFolderLen = rootFolder.Length;
            if (!Directory.Exists(rootFolder))
            {
                throw new NotSupportedException("root folder must exist");
            }
            //---------------------------------------
            string[] subFolders = fullFolder.Substring(rootFolderLen).Split(new string[] { "\\" }, StringSplitOptions.RemoveEmptyEntries);
            int j = subFolders.Length;
            string curFolder = rootFolder;
            for (int i = 0; i < j; ++i)
            {
                string subfolder = curFolder += "\\" + subFolders[i];
                if (!Directory.Exists(subfolder))
                {
                    Directory.CreateDirectory(subfolder);
                }
                curFolder = subfolder;
            }
        }



        #region Control Conponents
        private void SetUpComponent()
        {
            scan_btn.Enabled = false;

            EnableDecompileWidget(false);
            jsonOutRBtn.Checked = true;
            enable_rbtn.Checked = true;

            EnableControlDecompileWidget(false);

            browsePathTb.ReadOnly = true;
            browsePathTb.BackColor = System.Drawing.SystemColors.Window;

            _browseExeOrDll = new OpenFileDialog();
            _browseExeOrDll.Filter = ".NET Assembly file| *.exe; *.dll";
            _browseOutput = new OpenFileDialog();
            _browseOutput.Filter = "Text Files)|*.txt|Json Files|*.json";

            browse_btn.TabIndex = 1;
            scan_btn.TabIndex = 2;
            decompile_btn.TabIndex = 3;
            decompile_all_btn.TabIndex = 4;

            typesListView.View = View.Details;
            typesListView.GridLines = true;
            typesListView.FullRowSelect = true;

            typesListView.Columns.Add("Fullname", 200);
            typesListView.Columns.Add("Status", 100);

            typesListView.MouseDoubleClick += typesListView_DoubleClick;
            errorLogBtn.Enabled = false;
        }

        private void EnableDecompileWidget(bool enable)
        {
            outOptGroup.Enabled = enable;
            decompileOptGroup.Enabled = enable;
        }

        private void EnableControlDecompileWidget(bool enable)
        {
            pauseBtn.Enabled = false;
            stopBtn.Enabled = enable;
        }
        #endregion

        #region Decompile
        enum OutputOptions
        {
            Json,
            CSharp,
            Both
        }
        private OutputOptions CheckOutputOptions()
        {
            if (jsonOutRBtn.Checked)
            {
                return OutputOptions.Json;
            }
            else if (csharpOutRBtn.Checked)
            {
                return OutputOptions.CSharp;
            }
            else//both
            {
                return OutputOptions.Both;
            }
        }

        private void DecompileSelectedType(bool debug)
        {
            string selected = FindSelectedNode(treeView1.Nodes);
            if (selected == null)
            {
                MessageBox.Show("Please select node.");
                return;
            }

            TypeDefinition type;
            string result = "null";
            OutputOptions opt = CheckOutputOptions();
            string dllFileName = Path.GetFileName(_browsePath);
            if (nodeTypeDefs.TryGetValue(selected, out type))
            {
                if (type != null)
                {
                    try
                    {
                        string resultPath = DEFAULT_SAVEPATH + dllFileName + "\\" + (debug ? "Debug\\" : "Release\\");
                        Directory.CreateDirectory(resultPath + @"\Json");
                        Directory.CreateDirectory(resultPath + @"\CSharp");
                        _lastSaveOutputPath = resultPath;
                        string filename = type.FullName.Trim(_specialChar);
                        switch (opt)
                        {
                            case OutputOptions.Json:
                                result = GetJson(type, debug);
                                File.WriteAllText(resultPath + @"\Json\" + filename + JsonExtention, result, Encoding.UTF8);
                                typesListView.Items.Add(new ListViewItem(new string[] { filename + JsonExtention, "Success!!" }));
                                break;
                            case OutputOptions.CSharp:
                                result = GetCSharpCode(type, debug);
                                File.WriteAllText(resultPath + @"\CSharp\" + filename + CSharpExtention, result, Encoding.UTF8);
                                typesListView.Items.Add(new ListViewItem(new string[] { filename + CSharpExtention, "Success!!" }));
                                break;
                            case OutputOptions.Both:
                                result = GetJson(type, debug);
                                File.WriteAllText(resultPath + @"\Json\" + filename + JsonExtention, result, Encoding.UTF8);
                                typesListView.Items.Add(new ListViewItem(new string[] { filename + JsonExtention, "Success!!" }));
                                result = GetCSharpCode(type, debug);
                                File.WriteAllText(resultPath + @"\CSharp\" + filename + CSharpExtention, result, Encoding.UTF8);
                                typesListView.Items.Add(new ListViewItem(new string[] { filename + CSharpExtention, "Success!!" }));
                                break;
                            default:
                                break;
                        }

                        MessageBox.Show(type.FullName + " Decompile Success!!");
                    }
                    catch (DirectoryNotFoundException e)
                    {
                        MessageBox.Show("Error : " + e.Message);
                        File.WriteAllText(_lastSaveOutputPath + "..\\" + _errorLogFile, type.FullName + " - [ " + e.Message + " ]");
                        File.WriteAllText(_lastSaveOutputPath + "..\\" + _errorListFile, type.FullName);
                    }
                    //catch(NotSupportedException n)
                    //{
                    //    MessageBox.Show("Error : " + n.Message);
                    //    File.WriteAllText(_lastSaveOutputPath + "..\\" + _errorLogFile, type.FullName + " - [ " + n.Message + " ]");
                    //    File.WriteAllText(_lastSaveOutputPath + "..\\" + _errorListFile, type.FullName);
                    //}

                }
                else
                {
                    MessageBox.Show("Not Writable");
                }
            }
            else
            {
                MessageBox.Show("Not Writable");
            }
        }

        private void DecompileAll(bool debug)
        {
            errorLogBtn.Enabled = false;
            browse_btn.Enabled = false;
            scan_btn.Enabled = false;
            EnableDecompileWidget(false);
            EnableControlDecompileWidget(true);

            var allTypes = nodeTypeDefs.Values.ToArray();
            string dllFileName = Path.GetFileName(_browsePath);
            string result;

            string resultPath = DEFAULT_SAVEPATH + dllFileName + "\\" + (debug ? "Debug\\" : "Release\\");

            EnsureFolder(DEFAULT_SAVEPATH, resultPath + @"\Json");
            EnsureFolder(DEFAULT_SAVEPATH, resultPath + @"\CSharp");

            StringBuilder unwritable = new StringBuilder();
            _lastSaveOutputPath = resultPath;

            string[] arr = new string[2];
            ListViewItem item;

            int typeCount = nodeTypeDefs.Count;

            int successCount = 0;
            int errorCount = 0;

            OutputOptions opt = CheckOutputOptions();
            List<string> errorList = new List<string>();
            string filename;
            foreach (TypeDefinition type in allTypes)
            {
                if (type != null)
                {
                    arr[0] = filename = type.FullName.Trim(_specialChar);
                    try
                    {
                        arr[1] = "Success!!";
                        switch (opt)
                        {
                            case OutputOptions.Json:
                                result = GetJson(type, debug);
                                arr[0] = filename + JsonExtention;
                                File.WriteAllText(resultPath + @"\Json\" + arr[0], result, Encoding.UTF8);
                                break;
                            case OutputOptions.CSharp:
                                result = GetCSharpCode(type, debug);
                                arr[0] = filename + CSharpExtention;
                                File.WriteAllText(resultPath + @"\CSharp\" + arr[0], result, Encoding.UTF8);
                                break;
                            case OutputOptions.Both:
                                result = GetJson(type, debug);
                                arr[0] = filename + JsonExtention;
                                File.WriteAllText(resultPath + @"\Json" + arr[0], result, Encoding.UTF8);
                                typesListView.Items.Add(new ListViewItem(arr));

                                result = GetCSharpCode(type, debug);
                                arr[0] = filename + CSharpExtention;
                                File.WriteAllText(resultPath + @"\CSharp" + arr[0], result, Encoding.UTF8);
                                break;
                            default:
                                break;
                        }
                        typesListView.Items.Add(new ListViewItem(arr));
                        successCount++;
                    }
                    catch (ManualILSpy.Extention.FirstTimeUseException e)
                    {
                        unwritable.Append(type.FullName + " - [ " + e.Message + " ]\n");
                        arr[1] = e.Message;
                        errorCount++;
                        //isStop = true;
                        item = new ListViewItem(arr);
                        typesListView.Items.Add(item);
                        errorList.Add(type.FullName);
                    }
                    catch (Exception e)
                    {
                        unwritable.Append(type.FullName + " - [ " + e.Message + " ]\n");
                        arr[1] = e.Message;
                        errorCount++;
                        item = new ListViewItem(arr);
                        typesListView.Items.Add(item);
                        errorList.Add(type.FullName);
                    }
                    lbSuccessCounter.Text = "Success " + successCount + " / " + typeCount + " types";
                    lbErrorCounter.Text = "Error " + errorCount + " / " + typeCount + " types";
                }
                Application.DoEvents();
                if (isStop)
                    break;
            }
            File.WriteAllText(_lastSaveOutputPath + "..\\" + _errorLogFile, unwritable.ToString());
            File.WriteAllLines(_lastSaveOutputPath + "..\\" + _errorListFile, errorList.ToArray());
            MessageBox.Show("Decompile Success " + successCount + " files.\nDecompile Error " + errorCount + " files.");
            errorLogBtn.Enabled = true;

            browse_btn.Enabled = true;
            scan_btn.Enabled = true;
            EnableDecompileWidget(true);
            EnableControlDecompileWidget(false);
            isPause = false;
            isStop = false;
        }

        private void DecompileAllErrorTypes(bool debug)
        {
            try
            {
                string[] errorList = File.ReadAllLines(_lastSaveOutputPath + "..\\" + _errorListFile);
                TypeDefinition typeDef;
                string jsonOutput = "";
                string[] items = new string[2];
                string fileName;
                foreach (string typeName in errorList)
                {
                    if (nodeTypeDefs.TryGetValue(typeName, out typeDef))
                    {
                        fileName = typeDef.FullName.Trim(_specialChar);
                        try
                        {
                            jsonOutput = GetJson(typeDef, debug);
                            File.WriteAllText(_lastSaveOutputPath + fileName + JsonExtention, jsonOutput, Encoding.UTF8);
                            items[0] = fileName;
                            items[1] = "Success!!";
                            typesListView.Items.Add(new ListViewItem(items));
                        }
                        catch (NotSupportedException e)
                        {
                            items[0] = fileName;
                            items[1] = e.Message;
                            typesListView.Items.Add(new ListViewItem(items));
                        }
                        catch (Exception e)
                        {
                            items[0] = fileName;
                            items[1] = e.Message;
                            typesListView.Items.Add(new ListViewItem(items));
                        }
                    }
                }
                //MessageBox.Show("End Process");
            }
            catch (FileNotFoundException e)
            {
                MessageBox.Show("Please Decompile First.\n" + "Error : " + e.Message);
                return;
            }
            MessageBox.Show("End Process");
            //catch(Exception e)
            //{
            //    MessageBox.Show("Error : " + e.Message);
            //}

        }

        private void DecompileAny(DecompileTypeAny decompileMethod)
        {
            if (enable_rbtn.Checked)
            {
                decompileMethod(true);
            }
            else if (disable_rbtn.Checked)
            {
                decompileMethod(false);
            }
            else
            {
                MessageBox.Show("Please choose comment setting");
            }
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
        #endregion

        #region Node and AssemblyResolver
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

        Dictionary<string, TypeDefinition> nodeTypeDefs = new Dictionary<string, TypeDefinition>();
        Dictionary<string, TreeNode> nodeTreeNodes = new Dictionary<string, TreeNode>();

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

        private string GetJson(IMemberDefinition node, bool debug)
        {
            StringBuilderTextOutput output = new StringBuilderTextOutput();

            MyCsLang csharp = new MyCsLang();
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

        private string GetCSharpCode(IMemberDefinition member, bool debug)
        {
            PlainTextOutput output = new PlainTextOutput();
            CSharpLanguage csharp = new CSharpLanguage();
            DecompilationOptions options = new DecompilationOptions();
            options.DecompilerSettings = LoadDecompilerSettings();

            if (member is TypeDefinition)
            {
                DecomplieType(csharp, (TypeDefinition)member, output, options);
                return output.ToString();
            }
            else
            {
                MessageBox.Show("Not TypeDefinition");
            }
            return null;
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
        #endregion

        #region Widget Events
        private void browsePathTb_TextChanged(object sender, EventArgs e)
        {
            scan_btn.Enabled = !string.IsNullOrEmpty(browsePathTb.Text);
        }

        private void browse_btn_Click(object sender, EventArgs e)
        {
            _browsePath = BrowsePath(_browseExeOrDll);
            browsePathTb.Text = _browsePath;
            scan_btn.Enabled = !string.IsNullOrEmpty(browsePathTb.Text);
            //auto scan 

            scan_Click(this, EventArgs.Empty);
        }
        private void scan_Click(object sender, EventArgs e)
        {
            if (_browsePath == null)
            {
                return;
            }

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
            EnableDecompileWidget(true);
            lbCountAll.Text = Path.GetFileName(_browsePath) + " have " + nodeTypeDefs.Count + " types";
        }

        private void decompileSelected_btn_Click(object sender, EventArgs e)
        {
            DecompileAny(DecompileSelectedType);
        }

        private void decompileAll_btn_Click(object sender, EventArgs e)
        {
            DecompileAny(DecompileAll);
        }

        private void decompileError_Click(object sender, EventArgs e)
        {
            _lastSaveOutputPath = DEFAULT_SAVEPATH + "mscorlib.dll\\Debug\\";
            DecompileAny(DecompileAllErrorTypes);
        }

        private void testReadWriteJsonBtn_Click(object sender, EventArgs e)
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

        private void errorLogBtn_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(_lastSaveOutputPath + "..\\ErrorLog.txt");
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

        bool isStop = false;
        private void stopBtn_Click(object sender, EventArgs e)
        {
            if (!isStop)
            {
                isStop = true;
                EnableControlDecompileWidget(false);
                EnableDecompileWidget(true);
            }
        }

        private void typesListView_DoubleClick(object sender, MouseEventArgs e)
        {
            var items = typesListView.SelectedItems;
            ListViewItem item = items[0];
            if (item != null)
            {
                string fileTarget = item.Text.Trim(_specialChar);
                try
                {
                    if (fileTarget[fileTarget.Length - 1] == 'n')//from .json
                    {
                        System.Diagnostics.Process.Start(_lastSaveOutputPath + @"\Json\" + fileTarget);
                    }
                    else if (fileTarget[fileTarget.Length - 1] == 's')//from .cs
                    {
                        System.Diagnostics.Process.Start(_lastSaveOutputPath + @"\CSharp\" + fileTarget);
                    }
                }
                catch (Exception expection)
                {
                    MessageBox.Show(expection.Message);
                }

            }
        }

        private void clearBtn_Click(object sender, EventArgs e)
        {
            typesListView.Clear();
            typesListView.Columns.Add("Fullname", 200);
            typesListView.Columns.Add("Status", 100);

        }
        #endregion
    }
}
