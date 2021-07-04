﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using ClientStructure;

namespace TheWebServerWebApp.Controllers
{
    /**
     * HostController is a ASP.NET Web API controller class.
     * This class helps to host a list of client machines.
     * This class contains a rest service http method to register, remove and get a client, and also get and set the scoreboard.
     */
    public class HostController : ApiController
    {
        //private fields of LogHelper object to help log message to file
        private LogHelper logHelper = new LogHelper();

        /**
         * RegisterAClient rest service creates or register a client.
         * It adds the client to the static object of client list.
         */
        [Route("api/Host/RegisterAClient/")]
        [HttpPost]
        public void RegisterAClient([FromBody] ClientModel clientModel)
        {
            try
            {
                //adds the client to the static object of client list
                Clients.clientList.Add(clientModel);
                //log message to file
                logHelper.log($"[INFO] RegisterAClient() - registring client of port {clientModel.port}.");
            }
            //catch an exception and log error message to file and throw a http response exception
            catch (Exception)
            {
                //create an error response
                HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);
                httpResponseMessage.Content = new StringContent("[ERROR] RegisterAClient() - an exception occured whilst adding a client to the list.");
                //log message to file
                logHelper.log("[ERROR] RegisterAClient() - an exception occured whilst adding a client to the list.");
                //throw a http response exception
                throw new HttpResponseException(httpResponseMessage);
            }
        }

        /**
         * RemoveAClient rest service removes a client.
         * It removes the client from the static object of client list.
         */
        [Route("api/Host/RemoveAClient/{port}")]
        [HttpPost]
        public void RemoveAClient(string port)
        {
            try
            {
                //loops through the client list
                for (int i = 0; i < Clients.clientList.Count; i++)
                {
                    //if the client port is equal to the port inputted, remove the client on that port
                    if (Clients.clientList[i].port == port)
                    {
                        //log message to file
                        logHelper.log($"[INFO] RemoveAClient() - removing a client of port {Clients.clientList[i].port}.");
                        //removes the client from the static object of client list.
                        Clients.clientList.RemoveAt(i);
                    }
                }
            }
            //catch an exception and log error message to file and throw a http response exception
            catch (Exception)
            {
                //create an error response
                HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);
                httpResponseMessage.Content = new StringContent("[ERROR] RemoveAClient() - an exception occured whilst removing a client from the list.");
                //log message to file
                logHelper.log("[ERROR] RemoveAClient() - an exception occured whilst removing a client from the list.");
                //throw a http response exception
                throw new HttpResponseException(httpResponseMessage);
            }
        }

        /**
        * GetOtherClientList rest service gets the list of the client.
        * It returns the client list.
        */
        [Route("api/Host/GetOtherClientList")]
        [HttpGet]
        public List<ClientModel> GetOtherClientList()
        {
            //create a new client list
            List<ClientModel> clients = null;

            try 
            {
                //get the client list
                clients = Clients.clientList;
            }
            //catch an exception and log error message to file and throw a http response exception
            catch (Exception)
            {
                //create an error response
                HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);
                httpResponseMessage.Content = new StringContent("[ERROR] GetOtherClientList() - an exception occured whilst getting the client list.");
                //log message to file
                logHelper.log("[ERROR] GetOtherClientList() - an exception occured whilst getting the client list.");
                //throw a http response exception
                throw new HttpResponseException(httpResponseMessage);
            }

            //return the list of client
            return clients;
        }

        /**
        * SetScoreboard rest service sets the scoreboard by adding the job done number of a specific client.
        * The scoreboard will lets client see at a glance who is connected to the swarm and how many jobs they have completed.
        */
        [Route("api/Host/SetScoreboard/")]
        [HttpPost]
        public void SetScoreboard([FromBody] ClientModel clientModel)
        {
            try
            {
                //loops through the client list
                for(int i=0; i< Clients.clientList.Count; i++)
                {
                    //if the client port equals to the inputted client port, do add the job done number by 1
                    if (Clients.clientList[i].port == clientModel.port)
                    {
                        //add the job done number by 1
                        Clients.clientList[i].jobDone++;
                        //log message to file
                        logHelper.log($"[INFO] SetScoreboard() - add the job number completed of a client in the list. Client on port {Clients.clientList[i].port} has completed {Clients.clientList[i].jobDone} jobs.");
                    }
                }
            }
            //catch an exception and log error message to file and throw a http response exception
            catch (Exception)
            {
                //create an error response
                HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);
                httpResponseMessage.Content = new StringContent("[ERROR] SetScoreboard() - an exception occured whilst trying to add the job number completed of a client in the list.");
                //log message to file
                logHelper.log("[ERROR] SetScoreboard() - an exception occured whilst trying to add the job number completed of a client in the list.");
                //throw a http response exception
                throw new HttpResponseException(httpResponseMessage);
            }
        }

        /**
        * GetScoreboard rest service gets the score board.
        * It returns the scoreboard as a string.
        * The scoreboard will lets client see at a glance who is connected to the swarm and how many jobs they have completed.
        */
        [Route("api/Host/GetScoreboard/")]
        [HttpGet]
        public string GetScoreboard()
        {
            //create a string res for the return result
            string res = "";

            try
            {
                //check if the client list is not null
                if (Clients.clientList != null)
                {
                    //loop through the client list
                    for (int i = 0; i < Clients.clientList.Count; i++)
                    {
                        //set the string res with the clients port and the number of job done
                        res = res + "Port " + Clients.clientList[i].port + ": " + Clients.clientList[i].jobDone + " jobs.\n";
                    }
                }
            }
            //catch an exception and log error message to file and throw a http response exception
            catch (Exception)
            {
                //create an error response
                HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);
                httpResponseMessage.Content = new StringContent("[ERROR] GetScoreboard() - an exception occured whilst trying to get the content of the scoreboard text.");
                //log message to file
                logHelper.log("[ERROR] GetScoreboard() - an exception occured whilst trying to get the content of the scoreboard text.");
                //throw a http response exception
                throw new HttpResponseException(httpResponseMessage);
            }

            //return the string res
            return res;
        }
    }
}