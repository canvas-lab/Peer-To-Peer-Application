using System.Collections.Generic;
 
namespace ClientStructure
{
    /**
      * Client is a static class (static model is the best approach) that connects to the Data Tier via .NET remoting.
      * It contains a static fields of list of clients and a starting port of a client.
      */
    public static class Clients
    {
        //public fields
        public static List<ClientModel> clientList = new List<ClientModel>();
        public static int startingPort = 8000;
    }
}
