
//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


namespace ExpertiseExplorer.ExpertiseDB
{

using System;
    using System.Collections.Generic;
    
public partial class SourceRepository
{

    public SourceRepository()
    {

        this.Revisions = new HashSet<Revision>();

        this.FileRevisions = new HashSet<FileRevision>();

        this.Filenames = new HashSet<Filename>();

    }


    public int SourceRepositoryId { get; set; }

    public string Name { get; set; }

    public string URL { get; set; }



    public virtual ICollection<Revision> Revisions { get; set; }

    public virtual ICollection<FileRevision> FileRevisions { get; set; }

    public virtual ICollection<Filename> Filenames { get; set; }

}

}
