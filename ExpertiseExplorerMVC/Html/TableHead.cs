namespace ExpertiseExplorerMVC.Html
{
    using System;
    using System.Web.Mvc;

    public class TableHead : IDisposable
    {
        private readonly ViewContext viewContext;
        private bool disposed;

        public TableHead(ViewContext viewContext)
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

        public void EndTableHead()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;
                viewContext.Writer.Write("</thead>");
            }
        }
    }
}