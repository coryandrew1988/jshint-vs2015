namespace JSHint
{
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell.Interop;
    using System;

    // https://msdn.microsoft.com/en-us/library/vstudio/bb166557%28v=vs.140%29.aspx

    public class DocumentWatcher : IVsRunningDocTableEvents, IDisposable
    {
        public class HandlerSet
        {
            public Action<string> AfterSave { get; set; }
        }

        private class DocumentInfo
        {
            public uint flags, rLocks, wLocks, id;
            public string doc;
            public IntPtr data;
            public IVsHierarchy hierarchy;
        }

        private uint rdtCookie;
        private IVsRunningDocumentTable dt;
        private HandlerSet hs;

        public DocumentWatcher(HandlerSet handlerSet, IServiceProvider serviceProvider)
        {
            hs = handlerSet;
            dt = (IVsRunningDocumentTable)serviceProvider.GetService(typeof(SVsRunningDocumentTable));

            dt.AdviseRunningDocTableEvents(this, out rdtCookie);
        }

        private DocumentInfo GetDocInfo(uint docCookie)
        {
            uint flags, rLocks, wLocks, id;
            string doc;
            IntPtr data;
            IVsHierarchy hierarchy;
            dt.GetDocumentInfo(docCookie, out flags, out rLocks, out wLocks, out doc, out hierarchy, out id, out data);
            return new DocumentInfo
            {
                flags = flags,
                rLocks = rLocks,
                wLocks = wLocks,
                doc = doc,
                hierarchy = hierarchy,
                id = id,
                data = data
            };
        }

        #region Events

        public int OnAfterAttributeChange(uint docCookie, uint grfAttribs)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterSave(uint docCookie)
        {
            var di = GetDocInfo(docCookie);
            hs.AfterSave(di.doc);
            return VSConstants.S_OK;
        }

        public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        #endregion

        #region IDisposable Support

        private bool isDisposed = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    dt.UnadviseRunningDocTableEvents(rdtCookie);
                }

                isDisposed = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }

        #endregion
    }
}
