﻿

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
using System.Data.Entity;
using System.Data.Entity.Infrastructure;


public partial class ExpertiseDBEntities : DbContext
{
    public ExpertiseDBEntities()
        : base("name=ExpertiseDBEntities")
    {

    }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
        throw new UnintentionalCodeFirstException();
    }


    public DbSet<SourceRepository> SourceRepositorys { get; set; }

    public DbSet<Revision> Revisions { get; set; }

    public DbSet<FileRevision> FileRevisions { get; set; }

    public DbSet<Repository> Repositorys { get; set; }

    public DbSet<Developer> Developers { get; set; }

    public DbSet<Algorithm> Algorithms { get; set; }

    public DbSet<Artifact> Artifacts { get; set; }

    public DbSet<ArtifactType> ArtifactTypes { get; set; }

    public DbSet<DeveloperExpertise> DeveloperExpertises { get; set; }

    public DbSet<DeveloperExpertiseValue> DeveloperExpertiseValues { get; set; }

    public DbSet<ActualReviewer> ActualReviewers { get; set; }

    public DbSet<ComputedReviewer> ComputedReviewers { get; set; }

    public DbSet<Filename> Filenames { get; set; }

    public DbSet<Bug> Bugs { get; set; }

    public DbSet<RepositoryAlgorithmRunStatus> RepositoryAlgorithmRunStatus { get; set; }

}

}

