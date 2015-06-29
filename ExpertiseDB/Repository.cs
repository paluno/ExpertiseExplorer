
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
    
public partial class Repository
{

    public Repository()
    {

        this.Developers = new HashSet<Developer>();

        this.Artifacts = new HashSet<Artifact>();

        this.RepositoryAlgorithmRunStatus = new HashSet<RepositoryAlgorithmRunStatus>();

        this.Bugs = new HashSet<Bug>();

    }


    public int RepositoryId { get; set; }

    public string Name { get; set; }

    public string SourceURL { get; set; }

    public Nullable<System.DateTime> LastUpdate { get; set; }



    public virtual ICollection<Developer> Developers { get; set; }

    public virtual ICollection<Artifact> Artifacts { get; set; }

    public virtual ICollection<RepositoryAlgorithmRunStatus> RepositoryAlgorithmRunStatus { get; set; }

    public virtual ICollection<Bug> Bugs { get; set; }

}

}
