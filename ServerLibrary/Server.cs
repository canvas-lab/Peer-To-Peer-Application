using ClientStructure;
using System.ServiceModel;

namespace ServerLib
{
    /**
     *  Server is a C# console application.
     *  It is an implementation of the interface (the Server interface).
     *  It contains a method to add a job, download a job and upload the answer of the job.
     */
    //defining the behaviours of a service by ServiceBehavior, makes the service multi-threaded by ConcurrencyMode and allow management of the thread synchronisation
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
    public class Server : ServerInterface
    {
        //private fields of LogHelper object to help log message to file
        private LogHelper logHelper = new LogHelper();

        //public constructor
        public Server() { }

        /**
         * AddAJob method takes in a JobModel parameter.
         * It adds the job to the static job list.
         */
        public void AddAJob(JobModel jobModel)
        {
            //adds the job to the static job list.
            Jobs.jobList.Add(jobModel);

            //log message to file
            logHelper.log($"[INFO] AddAJob() - adding job {jobModel.num} which is uploaded by port {jobModel.whichPortNum} to the job list.");
        }

        /**
         * DownloadAJob method returns a JobModel object.
         * It loops through the static job list and check if a job is working.
         * If the fisrt job in the job list is available, returns that job.
         */
        public JobModel DownloadAJob()
        {
            //create a new JobModel object
            JobModel jobModel = new JobModel();
            //create a false bool to indicate wheter the first job in the list is selected
            bool isSuccessful = false;

            //loop through the job static list
            for(int i = 0; i< Jobs.jobList.Count; i++)
            {
                //if the job is indicated available and no job is set yet, do set the job model object
                if (Jobs.jobList[i].isJobWorking == false && isSuccessful == false)
                {
                    //set the job to unavailable
                    Jobs.jobList[i].isJobWorking = true;
                    //set bool to true as it has taken the first job
                    isSuccessful = true;
                    //set the job model
                    jobModel = Jobs.jobList[i];

                    //log message to file
                    logHelper.log($"[INFO] DownloadAJob() - downloading job number {Jobs.jobList[i].num} which is uploaded by port {Jobs.jobList[i].whichPortNum}.");
                }
            }

            //return the job
            return jobModel;
        }

        /**
        * UploadAnswerOfJob method takes int and string parameter
        * It loops through the static job list and check if the job number is the same as the number in the parameter.
        * If the job number is the same as the number in the parameter, set the result or answer of the python code.
        */
        public void UploadAnswerOfJob(int whichJobNum, string answer)
        {
            //loop through the job static list object
            for (int i = 0; i < Jobs.jobList.Count; i++)
            {
                //if the job number is the same as the number in the parameter, set the result or answer of the python code
                if (Jobs.jobList[i].num == whichJobNum)
                {
                    //set the result or answer of the python code
                    Jobs.jobList[i].answer = answer;

                    //log message to file
                    logHelper.log($"[INFO] UploadAnswerOfJob() - set job number {Jobs.jobList[i].num}'s answer which is uploaded by port {Jobs.jobList[i].whichPortNum}.");
                }
            }
        }
    }
}
