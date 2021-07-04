using System.Collections.Generic;

namespace ClientStructure
{
    /**
     * Jobs is a static class (static model is the best approach) that connects to the Data Tier via .NET remoting.
     * It contains a static fields of list of jobs.
     */
    public static class Jobs
    {
        //public fields
        public static List<JobModel> jobList = new List<JobModel>();
    }
}
