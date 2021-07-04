namespace ClientStructure
{
    /**
      * JobModel is a class that defines the job. It has a fields of:
      * a python code from the gui user input, 
      * a result or answer of the code which will be set in the gui result code textbox,
      * a current port who upload the job,
      * a job number,
      * a boolean to indicate wheter a job is working, and
      * a sha256Hash.
      */
    public class JobModel
    {
        //public fields
        public string code;
        public string answer;
        public int whichPortNum;
        public int num;
        public bool isJobWorking;
        public byte[] sha256Hash;
    }
}
