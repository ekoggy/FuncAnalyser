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

        private void AddRow(string func, int lines, int lineWithoutComm, int keyWords)
        {
            InfoTable.Items.Add(new { Func = func, Lines = lines, LinesWithoutComm = lineWithoutComm, KeyWords = keyWords });
        }
        private void ClearTable(int count)
        {
            for (int i = 0; i < count; ++i)
                InfoTable.Items.RemoveAt(0);
        }
        private int GetLinesAmount(CodeFunction elt)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            return elt.GetEndPoint(vsCMPart.vsCMPartBodyWithDelimiter).Line - elt.GetStartPoint(vsCMPart.vsCMPartHeader).Line + 1;
        }
        int GetNumStrings(string input)
        {
            int res = 0;
            for (int i = 0; i < input.Length; ++i)
            {
                if (input[i] == '\n')
                    res++;
            }
            return res;
        }
        int GetSingleCommNum(ref string func)
        {
            string new_string = "";
            int str = 0;
            Match match = Regex.Match(func, @"//(.*\\\r\n)*.*\n");
            if (match.Success)
            {
                str = GetNumStrings(match.Value);
                for (int i = 0; i < str; ++i)
                    new_string += " lav.ax\n";
                Regex reg = new Regex(@"//(.*\\\r\n)*.*\n");
                func = reg.Replace(func, new_string, 1);
            }
            return str;
        }
        int GetMultiCommNum(ref string func)
        {
            string new_string = "";
            int str = 0;
            Match match = Regex.Match(func, @"/\*(.*?\n)*?.*?\*/");
            if (match.Success)
            {
                str = GetNumStrings(match.Value);
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
            int str = 0;
            string new_string = "";
            Match match = Regex.Match(func, @"(""(.*?\\\r\n)*?.*?"")|(""(.*?\\\r\n)+?.*?\n)|(""(.*?\\\r\n)*?.*?\n)");
            if (match.Success)
            {
                str = GetNumStrings(match.Value);
                for (int i = 0; i < str; ++i)
                    new_string += "lavax\n";
                if (match.Value[match.Value.Length - 1] != '\n')
                    new_string += " lavax ";
                Regex reg = new Regex(@"(""(.*?\\\r\n)*?.*?"")|(""(.*?\\\r\n)+?.*?\n)|(""(.*?\\\r\n)*?.*?\n)");
                func = reg.Replace(func, new_string, 1);
                return;
            }

        }
        private int GetTrueStringsAmount(TextPoint begin, TextPoint end, ref string func)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
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
                    delta_str += GetSingleCommNum(ref func);
                else if (func[i] == '/' && func[i + 1] == '*')
                    delta_str += GetMultiCommNum(ref func);
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
            return end.Line - begin.Line + 1 - delta_str;
        }
        private int GetKeywordsNum(string func)
        {
            if (func == null)
                return 0;
            int res = 0;
            for (int i = 0; i < KeyWords.Length; ++i)
            {
                string pattern = "";
                pattern += KeyWords[i] + "\\W"; // поиск всех ключевых слов в тексте
                res += Regex.Matches(func, @pattern).Count; //запоминание их количества
                pattern = "(_"; //
                pattern += KeyWords[i] + "\\W" + ")|(\\w" + KeyWords[i] + "\\W)";
                res -= Regex.Matches(func, @pattern).Count; // вычитаем

            }
            return res;
        }

        private bool isFunc(CodeElement elt)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            return elt.Kind == vsCMElement.vsCMElementFunction ? true : false;
        }
        private void Update(object sender, RoutedEventArgs e)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            //DTE - средства разработки с помощью которых можно работать с окошком или приложением
            //Тут просто получаем 
            DTE2 dte = FuncAnalyserPackage.GetGlobalService(typeof(DTE)) as DTE2;

            //Интерфейс работы с кодом (в нём лежат куча данных и сеттеров)
            FileCodeModel fileCM = dte.ActiveDocument.ProjectItem.FileCodeModel;

            //Получение количества элементов (функций)
            CodeElements elts = fileCM.CodeElements;

            //Зачищаем таблица
            ClearTable(InfoTable.Items.Count);

            //Добавляем и заполняем навые строки
            for (int i = 1; i <= elts.Count; ++i)
                if (isFunc(elts.Item(i)))
                {
                    string func = null;
                    string name = elts.Item(i).FullName;
                    int lines = GetLinesAmount(elts.Item(i) as CodeFunction);
                    int linesWithoutComm = GetTrueStringsAmount(elts.Item(i).GetStartPoint(vsCMPart.vsCMPartHeader),
                                                        elts.Item(i).GetEndPoint(vsCMPart.vsCMPartBodyWithDelimiter),
                                                        ref func);

                    int key_words = GetKeywordsNum(func);

                    AddRow(name, lines, linesWithoutComm, key_words);
                }
        }

        private void Resize(object sender, RoutedEventArgs e)
        {
            //ширина столбцов
            double current_width = FuncAnalyser.ActualWidth / 4;
            //высота с доп параметром, который нужен для отображения кнопок внизу
            //(их ширина как раз 30)
            double current_height = FuncAnalyser.ActualHeight - 30.0;

            //замена ширины стобцов на новую
            for (int i = 0; i < 4; ++i)
                InfoTable.Columns[i].Width = current_width;

            //замена высоты на новую
            if (current_height > 0.0)
            {
                InfoTable.Height = current_height;
            }

            //изменение ширины кнопок (берутся мои коэйы и ширина строки = сумме ширин столбцов)
            HelloBottom.Width = 0.8 * current_width * 4;
            UpdateBottom.Width = 0.15 * current_width * 4;
        }

        private void Hello(object sender, RoutedEventArgs e)
        {
            string message = "I welcome you to my expansion! " +
                             "I hope it will be useful for you! " +
                             "If you have any questions or suggestions," +
                             " you can write to my mail example@gmail.com";
            string title = "Hello my dear!";
            MessageBox.Show(message, title);
        }
    }
}