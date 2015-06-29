namespace ExpertiseExplorer.MVC.Html
{
    using System;
    using System.Web.Mvc;

    public class Table : IDisposable
    {
        private readonly ViewContext viewContext;
        private bool disposed;

        public Table(ViewContext viewContext)
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

        public void EndTable()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;
                viewContext.Writer.Write("</table>");
            }
        }
    }
}