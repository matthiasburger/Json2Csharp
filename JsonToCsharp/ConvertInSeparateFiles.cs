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
    internal class ConvertInSeparateFiles : ConvertAssist
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("4878d2cb-d27b-4365-a6f9-be4118d4dea9");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConvertInSeparateFiles"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private ConvertInSeparateFiles(AsyncPackage package, OleMenuCommandService commandService)
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
        public static ConvertInSeparateFiles Instance
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
            Instance = new ConvertInSeparateFiles(package, commandService);
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

            for (int i = 0; i < parser.Classes.Count; i++)
            {
                Clazz clazz = parser.Classes[i];

                string netCode = new NetCodeWriter(new[] { clazz })
                {
                    Namespace = @namespace
                }.GetCode();

                string fileName = $"{clazz?.GetClassName() ?? $"TempClass_{i}"}.cs";
                bool hasSolution = !dte.Solution.FullName.IsNullOrWhiteSpace();

                TextSelection selection = hasSolution
                    ? _executeInSolution(dte, fileName)
                    : _executeSingleFile(dte);

                selection.SelectAll();
                selection.Delete();
                selection.Insert(netCode);

                if (!hasSolution || (doc.ProjectItem?.ProjectItems is null))
                    return;

                try
                {
                    dte.ActiveDocument.Save(Path.Combine(doc.Path, fileName));
                    doc.ProjectItem.ProjectItems.AddFromFile(Path.Combine(doc.Path, fileName));
                }
                catch (Exception)
                {
                    // ignore. converted file doesn't belong to the solution
                }
            }            
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