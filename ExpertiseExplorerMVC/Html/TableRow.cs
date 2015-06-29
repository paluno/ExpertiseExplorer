namespace ExpertiseExplorer.MVC.Html
{
    using System;
    using System.Web.Mvc;

    public class TableRow : IDisposable
    {
        private readonly ViewContext viewContext;
        private bool disposed;

        public TableRow(ViewContext viewContext)
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

        public void EndListItem()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;
                viewContext.Writer.Write("</tr>");
            }
        }
    }
}