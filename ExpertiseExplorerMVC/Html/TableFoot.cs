namespace ExpertiseExplorer.MVC.Html
{
    using System;
    using System.Web.Mvc;

    public class TableFoot : IDisposable
    {
        private readonly ViewContext viewContext;
        private bool disposed;

        public TableFoot(ViewContext viewContext)
        {
            if (viewContext == null)
                throw new ArgumentNullException("viewContext");

            this.viewContext = viewContext;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void EndTableBody()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;
                viewContext.Writer.Write("</tfoot>");
            }
        }
    }
}