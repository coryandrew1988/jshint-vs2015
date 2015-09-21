//------------------------------------------------------------------------------
// <copyright file="JSHintPackage.cs" company="Microsoft">
//     Copyright (c) Microsoft.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace JSHint
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [Guid(JSHintPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class JSHintPackage : Package
    {
        /// <summary>
        /// JSHintPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "32ff2929-1b02-4f73-9ca0-ebf40be1542f";

        /// <summary>
        /// Initializes a new instance of the <see cref="JSHintPackage"/> class.
        /// </summary>
        public JSHintPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

        #region Package Members

        private DocumentWatcher docWatcher;
        private Tester tester;
        private Logger logger;

        protected override void Initialize()
        {
            base.Initialize();

            tester = new Tester();
            logger = new Logger(this);

            docWatcher = new DocumentWatcher(new DocumentWatcher.HandlerSet
            {
                AfterSave = (docName) => {
                    var r = tester.Test(docName);
                    if (r.IsFail)
                    foreach (var h in r.Hints)
                    {
                        logger.Log(h);
                    }
                }
            }, this);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                docWatcher.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}
