namespace JSHint
{
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell.Interop;
    using System;
    using System.Collections.Generic;

    // https://msdn.microsoft.com/en-us/library/vstudio/bb166557%28v=vs.140%29.aspx

    public class DocumentWatcher : IVsRunningDocTableEvents, IDisposable
    {
        public class HandlerSet
        {
            public Action<string> OnOpen { get; set; }
            public Action<string> OnClose { get; set; }
            public Action<string> OnSave { get; set; }
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

        #region DocumentInfo

        private class DocState
        {
            public uint flags, rLocks, wLocks, id;
            public string doc;
            public IntPtr data;
            public IVsHierarchy hierarchy;
        }

        private Dictionary<uint, DocState> docInfoMap = new Dictionary<uint, DocState>();

        private void OpenDoc(uint docCookie)
        {
            uint flags, rLocks, wLocks, id;
            string doc;
            IntPtr data;
            IVsHierarchy hierarchy;
            dt.GetDocumentInfo(docCookie, out flags, out rLocks, out wLocks, out doc, out hierarchy, out id, out data);

            docInfoMap[docCookie] = new DocState
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

        private void CloseDoc(uint docCookie)
        {
            docInfoMap.Remove(docCookie);
        }

        private DocState GetDocState(uint docCookie)
        {
            DocState result;
            if (docInfoMap.TryGetValue(docCookie, out result))
            {
                return result;
            }

            return null;
        }

        private bool IsDocOpen(uint docCookie)
        {
            return GetDocState(docCookie) != null;
        }

        #endregion

        #region Events

        public int OnAfterSave(uint docCookie)
        {
            var di = GetDocState(docCookie);
            if (di != null)
            {
                hs.OnSave(di.doc);
            }

            return VSConstants.S_OK;
        }

        // This approach seems to work, but it's really unfortunate that I couldn't find a better way to observe simple document open/close events.

        public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            // Despite the suggestive "First" in the name, this event often fires multiple times.
            // There's probably some good reason for that, but the documentation did not make this obvious.
            if (!IsDocOpen(docCookie))
            {
                OpenDoc(docCookie);
                var di = GetDocState(docCookie);
                hs.OnOpen(di.doc);
            }

            return VSConstants.S_OK;
        }

        public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            // Locks seem to both hit 0 if and only if the file is closed.
            if (dwReadLocksRemaining == 0 && dwEditLocksRemaining == 0 && IsDocOpen(docCookie))
            {
                var di = GetDocState(docCookie);
                CloseDoc(docCookie);
                hs.OnClose(di.doc);
            }

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

        public int OnAfterAttributeChange(uint docCookie, uint grfAttribs)
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
