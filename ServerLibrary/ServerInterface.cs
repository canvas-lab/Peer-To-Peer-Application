using ClientStructure;
using System.ServiceModel;

namespace ServerLib
{
    //makes this a service contract as it is a service interface
    [ServiceContract]
    /**
     * ServerInterface is the public interface for the .NET server
     * It is the .NET Remoting network interface.
     */
    public interface ServerInterface
    {
        //OperationContracts is tagged as it is a service function contracts
        [OperationContract]
        void AddAJob(JobModel jobModel);

        //OperationContracts is tagged as it is a service function contracts
        [OperationContract]
        JobModel DownloadAJob();

        //OperationContracts is tagged as it is a service function contracts
        [OperationContract]
        void UploadAnswerOfJob(int whichJobNum, string answer);
    }
}
