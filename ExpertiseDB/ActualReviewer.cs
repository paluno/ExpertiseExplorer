
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
    
public partial class ActualReviewer
{

    public int ActualReviewerId { get; set; }

    public int ActivityId { get; set; }

    public string Reviewer { get; set; }

    public int BugId { get; set; }



    public virtual Bug Bug { get; set; }

}

}
