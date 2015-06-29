namespace ExpertiseExplorer.MVC.Html
{
    using System;
    using System.Web.Mvc;

    public class TableCell : IDisposable
    {
        private readonly ViewContext viewContext;
        private readonly TableCellKind cellKind;
        private bool disposed;

        public TableCell(ViewContext viewContext, TableCellKind cellKind)
        {
            if (viewContext == null)
                throw new ArgumentNullException("viewContext");

            this.viewContext = viewContext;
            this.cellKind = cellKind;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void EndTableStandardCell()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;
                viewContext.Writer.Write(cellKind == TableCellKind.HeaderCell ? "</th>" : "</td>");
            }
        }
    }
}