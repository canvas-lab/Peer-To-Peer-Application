using ClientStructure;
using IronPython.Hosting;
using IronPython.Runtime;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Scripting.Hosting;
using Newtonsoft.Json;
using RestSharp;
using ServerLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ClientGUIWpfApp
{
    /**
     * MainWindow is the interaction logic for MainWindow.xaml.
     * It is the client gui application.
     * It contains The Networking thread, which is going to connect to the server and other clients to find and do jobs, and
     * Also, it contains Server thread, which will manage the connections from other clients.
     * 
     * Basically, in the gui user will input a Python code which will be submitted to the Server thread, via static object.
     * The gui will also display if it is working on a job in the background, and how many jobs it has completed.
     * It will also query the Networking thread for how many jobs it’s done and if it’s currently working on one.
     */
    public partial class MainWindow : Window
    {
        //private fields
        List<ClientModel> clientList;
        Thread server, networking;
        bool isWindowDone;
        int numJobClientCompleted;
        //create a client variable to track the current client on a specific port
        ClientModel currClient;
        //create a bool variable to check if its fair or if any client is unfairly advantaged
        bool isFair;
        //private fields of LogHelper object to help log message to file
        private LogHelper logHelper = new LogHelper();


        //The main window
        public MainWindow()
        {
            //initialize the components
            InitializeComponent();

            //set variables initial value
            currClient = new ClientModel();
            currClient.jobDone = 0;
            numJobClientCompleted = 0;
            isFair = true;
            isWindowDone = false;
            currClient.port = Clients.startingPort.ToString();

            //as the textbbox is used to accept python code, so let the python code text box to have lines and tabs in the gui
            pythonCodeTxt.AcceptsTab = true;
            pythonCodeTxt.AcceptsReturn = true;
            //set gui values
            numJobCompleted.Text = numJobClientCompleted.ToString();
            thisPort.Text = currClient.port;

            //start the server and networking thread on initialisation, as the GUI is basically the main for this program
            server = new Thread(new ThreadStart(startServerThread));
            server.Start();
            networking = new Thread(new ThreadStart(startNetworkingThread));
            networking.Start();
        }

        /**
         * this method is called when a main window is closed by the user in the gui.
         * It will remove the dead client and update the scoreobard.
         * Also, server and networking thread will be aborted.
         */
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            //set bool to true as window a user is exiting the gui
            isWindowDone = true;
            //calls removeDeadClient method to remove the exiting client
            removeDeadClient();
            //calls showScoreboard method to update the scoreboard
            showScoreboard();

            //write message to the console
            Console.WriteLine("A user exits. He/She has been removed. Scoreboard has been updated. Aborting that server and networking thread");
            //log message to file
            logHelper.log("[INFO] MainWindow_Closing() - A user exits. He/She has been removed. Scoreboard has been updated. Aborting that server and networking thread");

            //abort the server and networking thread
            server.Abort();
            networking.Abort();
        }

        /**
         * this method is called when the submit button is clicked by the user in the gui.
         * It use cryptographic hashes to uniquely identify every string.
         * So, if at the other end of the internet communication the hash and the string don’t match, something went wrong and you might want to try again.
         * It uses SHA256 for hashes for verification, which the SHA256 hashes are stored as a byte array.
         * Also, it will set and add the job to the static job list.
         */
        private void SubmitCodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(pythonCodeTxt.Text) == false)
            {
                //using SHA256 for hashes for verification
                SHA256 sha256Hash = SHA256.Create();
                //encode the python code text inputted by user to a byte
                byte[] dataInByte = Encoding.UTF8.GetBytes(pythonCodeTxt.Text);
                //convert to base 64 string
                string dataStr = Convert.ToBase64String(dataInByte);
                //compute the sha256Hash
                byte[] hash = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(dataStr));

                //create a new JobModel object and set its fields value
                JobModel jobModel = new JobModel();
                jobModel.code = dataStr;
                jobModel.sha256Hash = hash;
                jobModel.whichPortNum = Convert.ToInt32(currClient.port);//the port of who hosted the job
                jobModel.num = Jobs.jobList.Count + 1;//to know how many jobs

                //add the job to the static job list object
                Jobs.jobList.Add(jobModel);
                //write description to the console
                Console.WriteLine($"Trying to add a job to the static job list by port {jobModel.whichPortNum}.");
                //log message to file
                logHelper.log($"[INFO] SubmitCodeButton_Click() - Trying to add a job to the static job list by port {jobModel.whichPortNum}.");
            }
            else
            {
                //display error to user
                MessageBox.Show("Python code inputted should not be empty.");
                //write description to the console
                Console.WriteLine("Python code inputted should not be empty.");
                //log message to file
                logHelper.log($"[ERROR] SubmitCodeButton_Click() - Python code inputted should not be empty.");
            }
        }

        /**
       * removeDeadClient method represent a method to remove the client from the web application using Rest.
       * In the rest client post method, it will remove a specific client that has exited.
       */
        public void removeDeadClient()
        {
            //if the window is closed or if client close the gui, remove the dead client
            if (isWindowDone == true)
            {
                //set the base url
                string URL = "https://localhost:44317/";
                //use RestClient and set the URL
                RestClient client = new RestClient(URL);
                //set up and call the API method
                RestRequest request = new RestRequest("api/Host/RemoveAClient/" + currClient.port);
                //use IRestResponse and set the request in the client post method
                IRestResponse resp = client.Post(request);

                //check if response is succesful
                if (resp.IsSuccessful)
                {
                    //write description to console
                    Console.WriteLine("A user has exited and have been removed.");
                    //log message to file
                    logHelper.log("[INFO] removeDeadClient() - A user has exited and have been removed.");
                }
                //if response is not succesful, log the error message to file
                else
                {
                    //log error message to file
                    logHelper.log(resp.Content);
                }
            }
        }

        /**
        * showScoreboard method represent a method to get the scoreboard from the web application using Rest.
        * The returned rest request data will be used to update the scoreboard.
        */
        public void showScoreboard()
        {
            //set the base url
            string URL = "https://localhost:44317/";
            //use RestClient and set the URL
            RestClient client = new RestClient(URL);
            //set up and call the API method
            RestRequest request = new RestRequest("api/Host/GetScoreboard/");
            //use IRestResponse and set the request in the client get method
            IRestResponse resp = client.Get(request);

            //initialize score board text
            string scoreboardStr = "";
            //check if response is succesful
            if (resp.IsSuccessful)
            {
                //deserialize object using JsonConvert
                scoreboardStr = JsonConvert.DeserializeObject<string>(resp.Content);
            }
            //if response is not succesful, log the error message to file
            else
            {
                //log error message to file
                logHelper.log(resp.Content);
            }

            //set the gui values of the scoreboard
            scoreboard.Text = scoreboardStr;
        }

        /**
         * startServerThread method is like the client’s job board.
         * It host a .NET Remoting service ServerService and register a client,
         * So that it can allow other clients to ask what jobs are available, download jobs, and upload answers.
         */
        private void startServerThread()
        {
            //This is the actual host service system
            ServiceHost host;
            //create a false bool to check if the server if done being registered in the server thread
            bool isServerDone = false;

            //call getOtherClientList method to get the list of the clients
            clientList = getOtherClientList();

            //while the server is not done being created or is false, do try to regsiter a client
            while (isServerDone == false)
            {
                try
                {
                    //Bind server to the implementation of Server
                    host = new ServiceHost(typeof(Server));
                    //This represents a tcp/ip binding in the Windows network stack
                    NetTcpBinding tcp = new NetTcpBinding();
                    //set the url
                    string hostUrl = "net.tcp://" + currClient.ip + ":" + currClient.port + "/ServerService";
                    //present the publicly accessible interface to the client. It tells .net to use current client ip, port and service name of ServerService
                    host.AddServiceEndpoint(typeof(ServerInterface), tcp, hostUrl);
                    //write description to console
                    Console.WriteLine("Opening the host.");
                    //log message to file
                    logHelper.log("[INFO] startServerThread() - opening the host.");
                    //open the host
                    host.Open();

                    //register a client and get the bool result
                    isServerDone = registerAClient();
                   
                    //Executes the delegate synchronously
                    Dispatcher.Invoke(() => {
                        //set the port text in the gui
                        thisPort.Text = currClient.port;
                        //show the score board in the gui
                        showScoreboard();
                    });

                    //while the window is still open, always loops
                    while (isWindowDone==false){}
                    //close the host if only window is closed
                    host.Close();
                }
                //catch exception of already in use port
                catch (AddressAlreadyInUseException)
                {
                    //write description to console
                    Console.WriteLine("Error - the port is already in use. AddressAlreadyInUseException occured. changing the clients port.");
                    //log message to file
                    logHelper.log("[ERROR] startServerThread() - the port is already in use. AddressAlreadyInUseException occured. changing the clients port.");

                    //add the port number by 1 
                    Clients.startingPort++;
                    //set the current client port to new port number
                    currClient.port = Clients.startingPort.ToString();
                    //Bind server to the implementation of Server
                    host = new ServiceHost(typeof(Server));

                    //Executes the delegate synchronously and show the score board in the gui
                    Dispatcher.Invoke(() => { showScoreboard(); });
                }
                //catch other exception 
                catch (Exception)
                {
                    //write description to console
                    Console.WriteLine("Error - an exception occured. changing the clients port.");
                    //log message to file
                    logHelper.log("[ERROR] startServerThread() - an exception occured. changing the clients port.");

                    //add the port number by 1 
                    Clients.startingPort++;
                    //set the current client port to new port number
                    currClient.port = Clients.startingPort.ToString();
                    //Bind server to the implementation of Server
                    host = new ServiceHost(typeof(Server));
                }
            }
        }

        /**
        * getOtherClientList method represent a method to get the client list from the web application using Rest.
        * It will retrives the rest client get method and return it as the client list.
        */
        public List<ClientModel> getOtherClientList()
        {
            //set the base url
            string URL = "https://localhost:44317/";
            //use RestClient and set the URL
            RestClient client = new RestClient(URL);
            //set up and call the API method
            RestRequest request = new RestRequest("api/Host/GetOtherClientList");
            //use IRestResponse and set the request in the client get method
            IRestResponse resp = client.Get(request);

            //check if response is succesful
            if (resp.IsSuccessful)
            {
                //deserialize object using JsonConvert
                clientList = JsonConvert.DeserializeObject<List<ClientModel>>(resp.Content);
            }
            //if response is not succesful, log the error message to file
            else
            {
                //log error message to file
                logHelper.log(resp.Content);
            }

            //return the client list
            return clientList;
        }

        /**
         * startNetworkingThread method do two things in a loop.
         * It look for new clients and
         * Check each client for jobs, and do them if it can.
         * Also, upon successfully downloading a job, the Networking thread,
         * Use Iron Python to execute the code and post the answer back to the client that hosted the job.
         */
        private void startNetworkingThread()
        {
            //initialize the Server
            ServerInterface server;
            //initialize the channel factory of Server
            ChannelFactory<ServerInterface> serverFactory;
            //This represents a tcp/ip binding in the Windows network stack
            NetTcpBinding tcp = new NetTcpBinding();

            //inifinitly runs the loop for the networking thread
            while (true)
            {
                //create a false bool to check if the client list is not null
                bool isListOk=false;

                //call getOtherClientList method to get the client list 
                clientList = getOtherClientList();
                //if client list is not null, set bool to true
                if(clientList != null)
                {
                    //set bool to true
                    isListOk = true;
                } 

                try
                {
                    //creates a SHA256 hash for hashes for verification
                    SHA256 sha256Hash = SHA256.Create();

                    //if the swarm in fair and client list is not null
                    if (isFair == true && isListOk == true)
                    {
                        //set fair bool to false as its currently an unfair swarm
                        isFair = false;
                        //loop each of the client list
                        for (int i = 0; i < clientList.Count; i++)
                        {
                            //if it is not the current client port, do create the server factory channel
                            if (currClient.port != clientList[i].port)
                            {
                                //present the publicly accessible interface to the client. It tells .net to use current client ip, port and service name of ServerService
                                serverFactory = new ChannelFactory<ServerInterface>(tcp, "net.tcp://" + clientList[i].ip + ":" + clientList[i].port + "/ServerService");
                                //create the channel
                                server = serverFactory.CreateChannel();

                                //download and get an available job
                                JobModel jobModel = server.DownloadAJob();
                                
                                //check if the python code inputted is not null
                                if (String.IsNullOrEmpty(jobModel.code) == false)
                                {
                                    //write descpriotion to the console
                                    Console.WriteLine("Client on port " + clientList[i].port + " is connected.");
                                    //log message to file
                                    logHelper.log("[INFO] startNetworkingThread() - Client on port " + clientList[i].port + " is connected.");

                                    //decode code back to a string
                                    byte[] encodedBytes = Convert.FromBase64String(jobModel.code);
                                    string codeStr = Encoding.UTF8.GetString(encodedBytes);
                                    //compute the hash
                                    byte[] hash = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(jobModel.code));

                                    //check if computed hash is sequence equal to the job hash
                                    if (hash.SequenceEqual(jobModel.sha256Hash))
                                    {
                                        //write descpriotion to the console
                                        Console.WriteLine("The computed hash is sequence equal to the job hash");
                                        //set the job code to the codeStr
                                        jobModel.code = codeStr;

                                        //Executes the delegate synchronously
                                        Dispatcher.Invoke(() =>
                                        {
                                            //calls doSetPythonCodeRes method to get the python code result with IronPython and set the gui values
                                            jobModel = doSetPythonCodeRes(jobModel);
                                        });

                                        //upload the answer of the specific job
                                        server.UploadAnswerOfJob(jobModel.num, jobModel.answer);

                                        //Executes the delegate synchronously
                                        Dispatcher.Invoke(() =>
                                        {
                                            //let the thread sleep for 0.5 second 
                                            Thread.Sleep(500);
                                            //call setGuiVal method to set the gui values
                                            setGuiVal(jobModel);
                                        });
                                    }
                                }
                            }
                        }
                    }
                    //if the swarm in unfair and client list is not null
                    else if (isFair == false && isListOk == true)
                    {
                        //set fair bool to true as its currently a fair swarm
                        isFair = true;
                        //loop the client list
                        for (int i = clientList.Count - 1; i >= 0; i--)
                        {
                            //if it is not the current client port, do create the server factory channel
                            if (currClient.port != clientList[i].port)
                            {
                                //present the publicly accessible interface to the client. It tells .net to use current client ip, port and service name of ServerService
                                serverFactory = new ChannelFactory<ServerInterface>(tcp, "net.tcp://" + clientList[i].ip + ":" + clientList[i].port + "/ServerService");
                                //create the channel
                                server = serverFactory.CreateChannel();

                                //download and get an available job
                                JobModel jobModel = server.DownloadAJob();

                                //check if the python code inputted is not null
                                if (String.IsNullOrEmpty(jobModel.code) == false)
                                {
                                    //write descpriotion to the console
                                    Console.WriteLine("Client on port " + clientList[i].port + " is connected.");
                                    //log message to file
                                    logHelper.log("[INFO] startNetworkingThread() - Client on port " + clientList[i].port + " is connected.");

                                    //decode code back to a string
                                    byte[] encodedBytes = Convert.FromBase64String(jobModel.code);
                                    string codeStr = Encoding.UTF8.GetString(encodedBytes);
                                    //compute the hash
                                    byte[] hash = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(jobModel.code));

                                    //check if computed hash is sequence equal to the job hash
                                    if (hash.SequenceEqual(jobModel.sha256Hash))
                                    {
                                        //write descpriotion to the console
                                        Console.WriteLine("The computed hash is sequence equal to the job hash");
                                        //set the job code to the codeStr
                                        jobModel.code = codeStr;

                                        //Executes the delegate synchronously
                                        Dispatcher.Invoke(() =>
                                        {
                                            //calls doSetPythonCodeRes method to get the python code result with IronPython and set the gui values
                                            jobModel = doSetPythonCodeRes(jobModel);
                                        });

                                        //upload the answer of the specific job
                                        server.UploadAnswerOfJob(jobModel.num, jobModel.answer);

                                        //Executes the delegate synchronously
                                        Dispatcher.Invoke(() =>
                                        {
                                            //let the thread sleep for 0.5 second 
                                            Thread.Sleep(500);
                                            //call setGuiVal method to set the gui values
                                            setGuiVal(jobModel);
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
                //if there is a fault, catch the exception of fault exception and show error message to user
                catch (FaultException)
                {
                    //write description to console
                    Console.WriteLine("Error - a fault exception occured.");
                    //log message to file
                    logHelper.log("[ERROR] startServerThread() - a fault exception occured.");

                    //calls removeDeadClient method to remove the client that has exited
                    removeDeadClient();
                    //calls getOtherClientList to get the client list 
                    clientList = getOtherClientList();
                }
                //if the endpoint is not found, catch the exception of endpoint not found and show error message to user
                catch (EndpointNotFoundException)
                {
                    //write description to console
                    Console.WriteLine("Error - an endpoint not found exception occured.");
                    //log message to file
                    logHelper.log("[ERROR] startServerThread() - an endpoint not found exception occured.");

                    //calls removeDeadClient method to remove the client that has exited
                    removeDeadClient();
                    //calls getOtherClientList to get the client list 
                    clientList = getOtherClientList();
                }
                //if the task is cancelled, catch the exception of task cancelled and show error message to user
                catch (TaskCanceledException)
                {
                    //write description to console
                    Console.WriteLine("Error - a task cancelled exception occured.");
                    //log message to file
                    logHelper.log("[ERROR] startServerThread() - a task cancelled exception occured.");

                    //calls removeDeadClient method to remove the client that has exited
                    removeDeadClient();
                    //calls getOtherClientList to get the client list 
                    clientList = getOtherClientList();
                }
                //if there is a communication error, catch the exception communication error and show error message to user
                catch (CommunicationException)
                {
                    //write description to console
                    Console.WriteLine("Error - a communication exception occured.");
                    //log message to file
                    logHelper.log("[ERROR] startServerThread() - a communication exception occured.");

                    //calls removeDeadClient method to remove the client that has exited
                    removeDeadClient();
                    //calls getOtherClientList to get the client list 
                    clientList = getOtherClientList();
                }
                //if there is an error, catch the exception and show error message to user
                catch (Exception)
                {
                    //write description to console
                    Console.WriteLine("Error - an exception occured.");
                    //log message to file
                    logHelper.log("[ERROR] startServerThread() - an exception occured.");

                    //calls removeDeadClient method to remove the client that has exited
                    removeDeadClient();
                    //calls getOtherClientList to get the client list 
                    clientList = getOtherClientList();
                }

                //if the window is still open, show the updated score board
                if (isWindowDone == false)
                {
                    //Executes the delegate synchronously and show the score board in the gui
                    Dispatcher.Invoke(() => { showScoreboard(); });
                }
            }
        }

        /**
         * registerAClient method returns a bool indicating that it has finish registring a client.
         * It will call the api method and set the request in the client post method.
         */
        public bool registerAClient()
        {
            //set the base url
            string URL = "https://localhost:44317/";
            //use RestClient and set the URL
            RestClient client = new RestClient(URL);
            //set up and call the API method
            RestRequest request2 = new RestRequest("api/Host/RegisterAClient/");
            //add json body to the request
            request2.AddJsonBody(currClient);
            //use IRestResponse and set the request in the client post method
            IRestResponse resp = client.Post(request2);

            //initialize bool
            bool isSuccess = false;
            //check if response is succesful
            if (resp.IsSuccessful)
            {
                //set bool to true as it has succesfully register a client
                isSuccess = true;
                //write description to console
                Console.WriteLine("Client on port " + currClient.port + " is registered, running server thread.");
                //log message to file
                logHelper.log("[INFO] registerAClient() - Client on port " + currClient.port + " is registered, running server thread.");
            }
            //if response is not succesful, log the error message to file
            else
            {
                //log error message to file
                logHelper.log(resp.Content);
            }

            //return bool
            return isSuccess;
        }

        /**
         * doSetPythonCodeRes method takes a JobModel parameter and is used for the networking thread.
         * It set the gui values such as the isJobWorkingInBackground, pythonCodeAnsTxt and the scoreboard text.
         * It uses IronPython to get the job answer from the python code inputted.
         * Then, it will update the score board and returns a JobModel object.
         */
        public JobModel doSetPythonCodeRes(JobModel jobModel)
        {
            try
            {
                //write description to console
                Console.WriteLine("With IronPython, try to execute the python code.");
                //log message to file
                logHelper.log("[INFO] doSetPythonCodeRes() - With IronPython, try to execute the python code.");
                //set the gui value for working in background to yes
                isJobWorkingInBackground.Text = "Yes";

                //run the code from inside C# with IronPython. create the engine
                ScriptEngine engine = Python.CreateEngine();
                //create the scope
                ScriptScope scope = engine.CreateScope();
                //execute the code
                engine.Execute(jobModel.code, scope);
                //use a C# dynamic type. get the variable of main
                dynamic codeFunction = scope.GetVariable("main");
                //get the result of the code
                var result = codeFunction();

                //set the gui value for the python code result
                pythonCodeAnsTxt.Text = result.ToString();
                //set the job answer
                jobModel.answer = pythonCodeAnsTxt.Text.ToString();
                //add the number of job client has completed bby 1
                numJobClientCompleted++;

                //call setScoreboard method to update the score board in the gui
                setScoreboard();
            }
            //if the code has a null refernece or returns nothing, catch the exception of null reference and show error message to user
            catch (NullReferenceException)
            {
                //show error message to user
                MessageBox.Show("Error - The code inputted returns nothing, a null reference execption occured.");
                Console.WriteLine("Error - The code inputted returns nothing, a null reference execption occured.");
                //log message to file
                logHelper.log("[Error] doSetPythonCodeRes() - The code inputted returns nothing, a null reference execption occured.");
            }
            //if the code has error that occurs when dynamic bind is processed and is not a valid python code, catch the exception of unbound name and show error message to user
            catch (RuntimeBinderException)
            {
                //show error message to user
                MessageBox.Show("Error - The code inputted is invalid, a runtime binder exception occured.");
                Console.WriteLine("Error - The code inputted is invalid, a runtime binder exception occured.");
                //log message to file
                logHelper.log("[Error] doSetPythonCodeRes() - The code inputted is invalid, a runtime binder exception occured.");
            }
            //if the code has unbound name and is not a valid python code, catch the exception of unbound name and show error message to user
            catch (UnboundNameException)
            {
                //calls removeDeadClient method to remove the client that has exited
                removeDeadClient();
                //calls getOtherClientList to get the client list 
                clientList = getOtherClientList();

                //show error message to user
                MessageBox.Show("Error - The code inputted is invalid, an unbound name execption occured.");
                Console.WriteLine("Error - The code inputted is invalid, an unbound name execption occured.");
                //log message to file
                logHelper.log("[Error] doSetPythonCodeRes() - The code inputted is invalid, an unbound name execption occured.");
            }
            //if the code has a syntax error and is not a valid python code, catch the exception of syntax error and show error message to user
            catch (SyntaxErrorException)
            {
                //show error message to user
                MessageBox.Show("Error - The code inputted is invalid, a syntax error occured.");
                Console.WriteLine("Error - The code inputted is invalid, a syntax error occured.");
                //log message to file
                logHelper.log("[Error] doSetPythonCodeRes() - The code inputted is invalid, a syntax error occured.");
            }
            //if the code has an error, catch the exception and show error message to user
            catch (Exception)
            {
                //show error message to user
                MessageBox.Show("Error - The code inputted is invalid, an exception occured.");
                Console.WriteLine("Error - The code inputted is invalid, an exception occured.");
                //log message to file
                logHelper.log("[Error] doSetPythonCodeRes() - The code inputted is invalid, an exception occured.");
            }

            //return the job
            return jobModel;
        }

        /**
        * setGuiVal method is used to set the gui values.
        * Values set are isJobWorkingInBackground, numJobCompleted and the scoreboard.
        * It will update those gui values.
        */
        public void setGuiVal(JobModel jobModel)
        {
            //set the gui value for working in background to no
            isJobWorkingInBackground.Text = "No";
            //set the gui value of the number of job client has completed
            numJobCompleted.Text = numJobClientCompleted.ToString();
            //call setScoreboard method to update the score board in the gui
            showScoreboard();

            //write description to console
            Console.WriteLine("Retrieving the Python code answer: " + jobModel.answer);
        }

        /**
        * setScoreboard method represent a method to set the scoreboard from the web application using Rest.
        * In the rest client post method, it will add the specific client job that has been completed or done.
        */
        public void setScoreboard()
        {
            //set the base url
            string URL = "https://localhost:44317/";
            //use RestClient and set the URL
            RestClient client = new RestClient(URL);
            //set up and call the API method
            RestRequest request = new RestRequest("api/Host/SetScoreboard/");
            //add json body to the request
            request.AddJsonBody(currClient);
            //use IRestResponse and set the request in the client post method
            IRestResponse resp = client.Post(request);

            //check if response is succesful
            if (resp.IsSuccessful)
            {
                //write description to console
                Console.WriteLine("The scoreboard is updated");
                //log message to file
                logHelper.log("[INFO] setScoreboard() - The scoreboard is updated");
            }
            //if response is not succesful, log the error message to file
            else
            {
                //log error message to file
                logHelper.log(resp.Content);
            }
        }
    }
}
