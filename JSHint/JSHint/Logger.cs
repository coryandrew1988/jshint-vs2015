namespace JSHint
{
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.TextManager.Interop;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    class Logger
    {
        private IServiceProvider sp;
        private ErrorListProvider elp;
        private Dictionary<string, List<ErrorTask>> tasksDictionary;

        public Logger(IServiceProvider serviceProvider)
        {
            sp = serviceProvider;
            tasksDictionary = new Dictionary<string, List<ErrorTask>>();

            elp = new ErrorListProvider(sp);
            elp.ProviderName = "Factory Guide Errors";
            elp.ProviderGuid = new Guid("5A10E43F-8D1D-4026-98C0-E6B502058901");
        }

        public void ClearDocument(string docName)
        {
            List<ErrorTask> tasks;
            if (!tasksDictionary.TryGetValue(docName, out tasks))
            {
                return;
            }

            foreach (var t in tasks)
            {
                elp.Tasks.Remove(t);
            }
        }

        public void Log(Hint hint)
        {
            if (!tasksDictionary.ContainsKey(hint.Filename))
            {
                tasksDictionary.Add(hint.Filename, new List<ErrorTask>());
            }
            var tasks = tasksDictionary[hint.Filename];

            ErrorTask task = new ErrorTask
            {
                Document = hint.Filename,
                Line = hint.LineNumber - 1,
                Column = hint.ColumnNumber - 1,
                Text = hint.Message,
                ErrorCategory = TaskErrorCategory.Message,
                Category = TaskCategory.User
            };
            task.Navigate += NavigateText;

            elp.Tasks.Add(task);
            tasks.Add(task);
        }

        private void NavigateText(object sender, EventArgs arguments)
        {
            // With regards to advice seen at https://social.msdn.microsoft.com/Forums/vstudio/en-US/a1d37fdf-09e0-41f9-a045-52a8109b8943/how-to-add-parsing-errors-to-errorlist-window?forum=vsx

            var task = sender as Microsoft.VisualStudio.Shell.Task;

            if (task == null) { throw new ArgumentException("sender"); }

            // If the name of the file connected to the task is empty there is nowhere to navigate to  
            if (String.IsNullOrEmpty(task.Document)) { return; }

            IVsUIShellOpenDocument openDoc = this.sp.GetService(typeof(IVsUIShellOpenDocument)) as IVsUIShellOpenDocument;

            if (openDoc == null) { return; }

            IVsWindowFrame frame;
            Microsoft.VisualStudio.OLE.Interop.IServiceProvider sp;
            IVsUIHierarchy hier;
            uint itemid;
            Guid logicalView = VSConstants.LOGVIEWID_Code;

            if (ErrorHandler.Failed(openDoc.OpenDocumentViaProject(
                task.Document, ref logicalView, out sp, out hier, out itemid, out frame))
                || frame == null
            )
            { return; }

            object docData;
            frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocData, out docData);

            // Get the VsTextBuffer  
            var buffer = docData as VsTextBuffer;
            if (buffer == null)
            {
                IVsTextBufferProvider bufferProvider = docData as IVsTextBufferProvider;
                if (bufferProvider != null)
                {
                    IVsTextLines lines;
                    ErrorHandler.ThrowOnFailure(bufferProvider.GetTextBuffer(out lines));
                    buffer = lines as VsTextBuffer;
                    Debug.Assert(buffer != null, "IVsTextLines does not implement IVsTextBuffer");

                    if (buffer == null) { return; }
                }
            }

            // Finally, perform the navigation.  
            IVsTextManager mgr = this.sp.GetService(typeof(VsTextManagerClass)) as IVsTextManager;
            if (mgr == null) { return; }

            mgr.NavigateToLineAndColumn(buffer, ref logicalView, task.Line, task.Column, task.Line, task.Column);
        }
    }
}
