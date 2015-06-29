using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpertiseExplorer.ExpertiseDB.Extensions
{
    public class DeveloperWithExpertise
    {
            public int DeveloperId { get; set;}
            public double Expertise { get; set;}

            public DeveloperWithExpertise(int id, double expertise)
            {
                this.DeveloperId = id;
                this.Expertise = expertise;
            }
    }
}
