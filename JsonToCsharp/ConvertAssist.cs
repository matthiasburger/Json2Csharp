using System.IO;
using System.Linq;

using EnvDTE;

using IronSphere.Extensions;

using Microsoft.VisualStudio.Shell;

namespace JsonToCsharp
{
    public abstract class ConvertAssist
    {
        protected JsonParser _parseDte(_DTE dte, string className)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            JsonParser parser = new JsonParser(GetText(dte))
            {
                InitialClassName = className
            };
            parser.Parse();
            return parser;
        }

        protected abstract string GetText(_DTE dte);

        protected static string _getJsonToConvert(_DTE dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Document doc = dte.ActiveDocument;
            TextDocument txt = doc?.Object() as TextDocument;

            EditPoint editPoint = txt?.StartPoint.CreateEditPoint();

            TextSelection selection = (TextSelection)dte.ActiveDocument.Selection;
            string result = selection?.Text.Length > 0
                ? selection.Text
                : editPoint?.GetText(txt.EndPoint);

            return result;
        }

        protected static TextSelection _executeInSolution(_DTE dte, string fileName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            dte.ItemOperations.NewFile(@"General\Text File", fileName, Constants.vsViewKindTextView);
            return (TextSelection)dte.ActiveDocument.Selection;
        }

        protected static TextSelection _executeSingleFile(_DTE dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return (TextSelection)dte.ActiveDocument.Selection;
        }

        protected static string _findNameSpaceInPath(string path)
        {
            string[] files = Directory.GetFiles(path, "*.cs", SearchOption.TopDirectoryOnly);
            return files.Select(_getNamespaceInFile)
                .FirstOrDefault(@namespace => !@namespace.IsNullOrWhiteSpace());
        }

        protected static string _getNamespaceInFile(string filepath)
        {
            return (
                from line in File.ReadLines(filepath)
                where line.StartsWith("namespace ")
                select ".".Join(line.Split(' ').Skip(1))
            ).FirstOrDefault();
        }
    }
}