using EnvDTE;
using EnvDTE80;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace FuncAnalyser
{
    public partial class ToolWindow1Control : UserControl
    {
        string[] KeyWords = {"alignas", "alignof", "and", "and_eq", "asm", "auto", "bitand", "bitor", "bool", "break", "case", "catch", "char",
            "char16_t", "char32_t", "class", "compl", "const", "constexpr", "const_cast", "continue", "decltype", "default", "delete", "do", "double",
            "dynamic_cast", "else", "enum", "explicit", "export", "extern", "false", "float", "for", "friend", "goto", "if", "inline", "int", "long",
            "mutable", "namespace", "new", "noexcept", "not", "not_eq", "nullptr", "operator", "or", "or_eq", "private", "protected", "public",
            "register", "reinterpret_cast", "return", "short", "signed", "sizeof", "static", "static_assert", "static_cast", "struct", "switch", "template",
            "this", "thread_local", "throw", "true", "try", "typedef", "typeid", "typename", "union", "unsigned", "using", "virtual", "void", "volatile",
            "wchar_t", "while", "xor", "xor_eq" };

        public ToolWindow1Control()
        {
            this.InitializeComponent();
        }

        private int NumOfObj(CodeElements elts)
        {
            int numOfObj = 0;
            CodeElement elt = null;
            for (int i = 1; i <= elts.Count; i++)
            {
                elt = elts.Item(i);
                if (elt.Kind == vsCMElement.vsCMElementFunction)
                    numOfObj++;
            }
            return numOfObj;
        }
        private void AddRow(int count)
        {
            for (int i = 0; i < count; ++i)
                InfoTable.Items.Add(new { Func = "0", Lines = "0", LinesWithoutComm = "0", KeyWords = "0" });
        }
        private void RemoveRow(int count)
        {
            for (int i = 0; i < count; ++i)
                InfoTable.Items.RemoveAt(0);
        }
        private int NumOfLines(CodeFunction elt)
        {
            return elt.GetEndPoint(vsCMPart.vsCMPartBodyWithDelimiter).Line - elt.GetStartPoint(vsCMPart.vsCMPartHeader).Line + 1;
        }
        int Strings(string input)
        {
            int res = 0;
            for (int i = 0; i < input.Length; ++i)
            {
                if (input[i] == '\n')
                    res++;
            }
            return res;
        }
        int SingleCommentTample(ref string func)
        {
            string new_string = "";
            int str = 0;
            Match match = Regex.Match(func, @"//(.*\\\r\n)*.*\n");
            if (match.Success)
            {
                str = Strings(match.Value);
                for (int i = 0; i < str; ++i)
                    new_string += " lav.ax\n";
                Regex reg = new Regex(@"//(.*\\\r\n)*.*\n");
                func = reg.Replace(func, new_string, 1);
            }
            return str;
        }
        int MultiplyCommentTample(ref string func)
        {
            string new_string = "";
            int str = 0;
            Match match = Regex.Match(func, @"/\*(.*?\n)*?.*?\*/");
            if (match.Success)
            {
                str = Strings(match.Value);
                for (int i = 0; i < str; ++i)
                    new_string += " lav.ax\n";
                new_string += " lav.ax ";
                Regex reg = new Regex(@"/\*(.*?\n)*?.*?\*/");
                func = reg.Replace(func, new_string, 1);
            }
            return str + 1;
        }
        void SingleQuotTample(ref string func)
        {
            string new_string = "";
            int str = 0;
            Match match = Regex.Match(func, @"('.*?')|('.*?\n)");
            if (match.Success)
            {
                if (match.Value[match.Value.Length - 1] != '\n')
                    new_string += " lavax ";
                else
                    new_string += " lavax\n";
                Regex reg = new Regex(@"('.*?')|('.*?\n)");
                func = reg.Replace(func, new_string, 1);
                return;
            }

        }
        void MultipleQuotTample(ref string func)
        {
            string new_string = "";
            int str = 0;
            Match match = Regex.Match(func, @"(""(.*?\\\r\n)*?.*?"")|(""(.*?\\\r\n)+?.*?\n)|(""(.*?\\\r\n)*?.*?\n)");
            if (match.Success)
            {
                str = Strings(match.Value);
                for (int i = 0; i < str; ++i)
                    new_string += "lavax\n";
                if (match.Value[match.Value.Length - 1] != '\n')
                    new_string += " lavax ";
                Regex reg = new Regex(@"(""(.*?\\\r\n)*?.*?"")|(""(.*?\\\r\n)+?.*?\n)|(""(.*?\\\r\n)*?.*?\n)");
                func = reg.Replace(func, new_string, 1);
                return;
            }

        }
        private int NumOfCodeLines(TextPoint begin, TextPoint end, ref string func)
        {
            int delta_str = 0;
            func = begin.CreateEditPoint().GetLines(begin.Line, end.Line + 1);
            func += '\n';
            Regex reg = new Regex(@"\\""");
            func = reg.Replace(func, "");
            reg = new Regex(@"\\'");
            func = reg.Replace(func, "");
            for (int i = 0; i < func.Length - 2; ++i)
            {
                if (func[i] == '/' && func[i + 1] == '/')
                    delta_str += SingleCommentTample(ref func);
                else if (func[i] == '/' && func[i + 1] == '*')
                    delta_str += MultiplyCommentTample(ref func);
                else if (func[i] == '"')
                    MultipleQuotTample(ref func);
                else if (func[i] == '\'')
                    SingleQuotTample(ref func);
            }
            bool free_str = true;
            int duplicate = 0;
            for (int i = 0; i < func.Length; ++i)
            {
                if (func[i] == '\n')
                {
                    if (free_str)
                        delta_str++;
                    if (duplicate > 1)
                        delta_str -= (duplicate - 1);
                    free_str = true;
                    duplicate = 0;
                    continue;
                }
                if (func[i] != '\t' && func[i] != '\r' && func[i] != ' ')
                    free_str = false;
                if (i > func.Length - 7)
                    continue;
                if (func[i] == 'l' && func[i + 1] == 'a' && func[i + 2] == 'v' && func[i + 3] == '.' && func[i + 4] == 'a' && func[i + 5] == 'x')
                    duplicate++;
            }
            //return func;
            return end.Line - begin.Line + 1 - delta_str;
        }
        private int NumOfKeyWords(string func)
        {
            if (func == null)
                return 0;
            int res = 0;
            for (int i = 0; i < KeyWords.Length; ++i)
            {
                string pattern = "";
                pattern += KeyWords[i] + "\\W";
                res += Regex.Matches(func, @pattern).Count;
                pattern = null;
                pattern = "(_";
                pattern += KeyWords[i] + "\\W" + ")|(\\w" + KeyWords[i] + "\\W)";
                res -= Regex.Matches(func, @pattern).Count;

            }
            return res;
        }
        private void Update(object sender, RoutedEventArgs e)
        {
            int lines, lines_without_comm, key_words, numOfObj;
            string func = null;
            DTE2 dte = FuncAnalyserPackage.GetGlobalService(typeof(DTE)) as DTE2;
            FileCodeModel fileCM = dte.ActiveDocument.ProjectItem.FileCodeModel;
            CodeElements elts = fileCM.CodeElements;
            numOfObj = NumOfObj(elts);
            if (InfoTable.Items.Count < numOfObj)
                AddRow(numOfObj - InfoTable.Items.Count);
            else if (InfoTable.Items.Count > numOfObj)
                RemoveRow(InfoTable.Items.Count - numOfObj);
            for (int i = 1, j = 0; i <= elts.Count; ++i)
            {
                CodeElement elt_1 = elts.Item(i);
                if (elt_1.Kind == vsCMElement.vsCMElementFunction)
                {
                    func = null;
                    var elt = elts.Item(i) as CodeFunction;
                    string name = elt.FullName;
                    lines = NumOfLines(elt);
                    lines_without_comm = NumOfCodeLines(elt.GetStartPoint(vsCMPart.vsCMPartHeader), elt.GetEndPoint(vsCMPart.vsCMPartBodyWithDelimiter), ref func);
                    key_words = NumOfKeyWords(func);
                    InfoTable.Items[j] = new { Func = name, Lines = lines.ToString(), LinesWithoutComm = lines_without_comm.ToString(), KeyWords = key_words.ToString() };
                    j++;
                }
            }
        }   

        private void Resize(object sender, RoutedEventArgs e)
        {
            double current_width = FuncAnalyser.ActualWidth / 4;
            double current_height = FuncAnalyser.ActualHeight - 30.0;

            for (int i = 0; i < 4; ++i)
                InfoTable.Columns[i].Width = current_width;

            if (current_height > 0.0)
            {
                InfoTable.Height = current_height;
            }

            HelloBottom.Width = 0.8 * current_width * 4;
            UpdateBottom.Width = 0.15 * current_width * 4;
        }

        private void Hello(object sender, RoutedEventArgs e)
        {
            string message = "I welcome you to my expansion! I hope it will be useful for you! If you have any questions or suggestions, you can write to my mail example@gmail.com";
            string title = "Hello my dear!";
            MessageBox.Show(message, title);
        }
    }
}