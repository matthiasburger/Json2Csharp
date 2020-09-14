using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnvDTE;

using IronSphere.Extensions;

using JetBrains.Annotations;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;

using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using Task = System.Threading.Tasks.Task;

namespace JsonToCsharp
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class ConvertCommand : ConvertAssist
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("bfbe045e-459f-4e5d-893f-3a5e84aef6b7");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConvertCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private ConvertCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static ConvertCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IAsyncServiceProvider ServiceProvider => package;

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in ConvertCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new ConvertCommand(package, commandService);
        }


        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!(ServiceProvider.GetServiceAsync(typeof(DTE)).Result is DTE dte))
            {
                _showMessageBoxNoDte();
                return;
            }

            Document doc = dte.ActiveDocument;
            if (doc is null)
            {
                _showMessageBoxNoDocument();
                return;
            }

            string className = doc?.Name?.Split('.').FirstOrDefault() ?? "temp_class";
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
            
            string @namespace = _findNameSpaceInPath(doc.Path);

            string netCode = new NetCodeWriter(parser.Classes)
            {
                Namespace = @namespace
            }.GetCode();

            string fileName = $"{parser.Classes.FirstOrDefault(x => x.Name == className)?.GetClassName() ?? "TempClass"}.cs";
            bool hasSolution = !dte.Solution.FullName.IsNullOrWhiteSpace();

            TextSelection selection = hasSolution
                ? _executeInSolution(dte, fileName)
                : _executeSingleFile(dte);

            selection.SelectAll();
            selection.Delete();
            selection.Insert(netCode);

            if (!hasSolution)
                return;

            dte.ActiveDocument.Save(Path.Combine(doc.Path, fileName));
            doc.ProjectItem.ProjectItems.AddFromFile(Path.Combine(doc.Path, fileName));
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

        protected override string GetText(_DTE dte) => _getJsonToConvert(dte);
    }
}

