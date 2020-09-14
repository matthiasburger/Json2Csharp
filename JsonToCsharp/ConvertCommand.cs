using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EnvDTE;

using IronSphere.Extensions;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;

using Task = System.Threading.Tasks.Task;

namespace JsonToCsharp
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class ConvertCommand
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

            DTE dte = ServiceProvider.GetServiceAsync(typeof(DTE)).Result as DTE;

            Document doc = dte?.ActiveDocument;
            TextDocument txt = doc?.Object() as TextDocument;

            EditPoint editPoint = txt?.StartPoint.CreateEditPoint();

            string result;
            TextSelection selection = (TextSelection)dte?.ActiveDocument.Selection;
            if (selection?.Text.Length > 0)
            {
                result = selection.Text;
            }
            else
            {
                result = editPoint?.GetText(txt.EndPoint);
                if (result is null)
                    return;
            }

            try
            {
                string className = doc?.Name?.Split('.').FirstOrDefault() ?? "initial_class";

                JsonParser parser = new JsonParser(result)
                {
                    InitialClassName = className
                };
                parser.Parse();

                NetCodeWriter codeWriter = new NetCodeWriter(parser.Classes);

                string netCode = codeWriter.GetCode();

                if (dte.Solution.FullName.IsNullOrWhiteSpace())
                {
                    TextSelection txtSel = (TextSelection)dte.ActiveDocument.Selection;

                    txtSel.SelectAll();
                    txtSel.Delete();

                    editPoint.Insert(netCode);
                }
                else
                {
                    string fileName = parser.Classes.FirstOrDefault(x => x.Name == className)?.GetClassName() ?? "TempClass";
                    dte.ItemOperations.NewFile(@"General\Text File", fileName + ".cs", Constants.vsViewKindTextView);
                    TextSelection txtSel = (TextSelection)dte.ActiveDocument.Selection;

                    txtSel.SelectAll();
                    txtSel.Delete();
                    txtSel.Insert(netCode);
                }
            }
            catch (Exception ex)
            {
                editPoint.EndOfDocument();
                editPoint.Insert(Environment.NewLine);
                editPoint.Insert(Environment.NewLine);
                editPoint.Insert("----------------------------");
                editPoint.Insert(Environment.NewLine);
                editPoint.Insert(ex.ToString());
            }
        }
    }
}

