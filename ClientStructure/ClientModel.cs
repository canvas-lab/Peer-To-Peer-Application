namespace ClientStructure
{
    /**
      * ClientModel is a class that defines the client.
      * The clients has an IP address, 
      * a port to add client to the server, and 
      * an int to tell how many job the client has compeleted or done.
      */
    public class ClientModel
    {
        //public fields
        public string ip = "localhost";
        public string port;
        public int jobDone;
    }
}
