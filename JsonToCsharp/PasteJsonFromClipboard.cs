using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using EnvDTE;

using IronSphere.Extensions;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using Constants = Microsoft.VisualStudio.Shell.Interop.Constants;
using Task = System.Threading.Tasks.Task;

namespace JsonToCsharp
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class PasteJsonFromClipboard : ConvertAssist
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 256;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("168fa7de-1724-4ff0-adda-7ffb40d155aa");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="PasteJsonFromClipboard"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private PasteJsonFromClipboard(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static PasteJsonFromClipboard Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in PasteJsonFromClipboard's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new PasteJsonFromClipboard(package, commandService);
        }

        [STAThread]
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!(ServiceProvider.GetServiceAsync(typeof(DTE)).Result is DTE dte))
            {
                _showMessageBoxNoDte();
                return;
            }

            Document doc = dte.ActiveDocument;
            TextDocument txt = doc?.Object() as TextDocument;
            bool useCurrentDocument = txt?.StartPoint.CreateEditPoint().GetText(txt.EndPoint).Trim().Length == 0;
            
            string className = (useCurrentDocument ? doc?.Name?.Split('.').FirstOrDefault() : null) ?? "temp_class";
            JsonParser parser;

            try
            { 
                parser = _parseDte(dte, className);
            }
            catch (Exception exception)
            {
                _showMessageBoxParsing(exception.ToString());
                return;
            }
            
            string netCode = new NetCodeWriter(parser.Classes).GetCode();

            TextSelection selection = useCurrentDocument 
                ? _executeSingleFile(dte) 
                : _executeInSolution(dte, "TempClass.cs");

            selection.SelectAll();
            selection.Delete();
            selection.Insert(netCode);
        }

        private void _showMessageBoxNoDte()
        {
            VsShellUtilities.ShowMessageBox(
                package,
                "DTE is null - please create an issue, including your json and repro-steps to https://github.com/matthiasburger/Json2Csharp-Issues/issues",
                "Json to C# Plugin - Exception",
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        private void _showMessageBoxParsing(string exception)
        {
            StringBuilder stringBuilder = new StringBuilder(exception)
                .Append("----------------------------")
                .Append(Environment.NewLine)
                .Append("Please write an issue, including your json and repro-steps to https://github.com/matthiasburger/Json2Csharp-Issues/issues");

            VsShellUtilities.ShowMessageBox(
                package,
                stringBuilder.ToString(),
                "Json to C# Plugin - Exception",
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        private void _showMessageBoxNoDocument()
        {
            VsShellUtilities.ShowMessageBox(
                package,
                "This function requires an active document. Please open a valid Json-File.",
                "Json to C# Plugin - Exception",
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        [STAThread]
        private void Execute_old(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (Clipboard.ContainsText(TextDataFormat.Text))
            {
                string result = Clipboard.GetText(TextDataFormat.Text);

                DTE dte = ServiceProvider.GetServiceAsync(typeof(DTE)).Result as DTE;

                Document doc = dte?.ActiveDocument;
                TextDocument txt = doc?.Object() as TextDocument;

                bool useCurrentDocument = txt?.StartPoint.CreateEditPoint().GetText(txt.EndPoint).Trim().Length == 0;

                try
                {
                    string className = (useCurrentDocument ? doc?.Name?.Split('.').FirstOrDefault() : null) ?? "initial_class";

                    JsonParser parser = new JsonParser(result)
                    {
                        InitialClassName = className
                    };
                    parser.Parse();

                    string netCode = new NetCodeWriter(parser.Classes).GetCode();

                    if (!useCurrentDocument)
                    {
                        string fileName = parser.Classes.FirstOrDefault(x => x.Name == className)?.GetClassName() ?? "TempClass";
                        dte.ItemOperations.NewFile(@"General\Text File", $"{fileName}.cs", EnvDTE.Constants.vsViewKindTextView);
                    }

                    TextSelection txtSel = (TextSelection)dte.ActiveDocument.Selection;

                    txtSel.SelectAll();
                    txtSel.Delete();
                    txtSel.Insert(netCode);
                }
                catch (Exception ex)
                {
                    StringBuilder stringBuilder = new StringBuilder(ex.ToString())
                            .Append("----------------------------")
                            .Append(Environment.NewLine)
                            .Append("Please write an issue, including your json and repro-steps to https://github.com/matthiasburger/Json2Csharp-Issues/issues");

                    VsShellUtilities.ShowMessageBox(
                        package,
                        stringBuilder.ToString(),
                        "Json to C# Plugin",
                        OLEMSGICON.OLEMSGICON_INFO,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                }
            }
        }

        protected override string GetText(_DTE dte) => 
            Clipboard.ContainsText(TextDataFormat.Text) ? Clipboard.GetText(TextDataFormat.Text) : null;
    }
}
