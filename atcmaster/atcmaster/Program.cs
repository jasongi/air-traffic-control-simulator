using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
/*  
    Copyright (C) 2015 Jason Giancono (jasongiancono@gmail.com)

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
*/
namespace ATCMaster
{
    class Program
    {
        static void Main(string[] args)
        {
            //prompt for binding address
            System.Console.WriteLine("Input Service Endpoint Address (leave blank for localhost:50002/ATCMaster)");
            string address = System.Console.ReadLine();
            if (address == "")
            {
                address = "localhost:50002/ATCMaster";
            }
            StartServer(address);
        }

        /// <summary>
        /// Start the server
        /// </summary>
        /// <param name="address">the address that the server binds to</param>
        static void StartServer(string address)
        {
            ServiceHost host = null;
            try
            {
                NetTcpBinding tcpBinding = new NetTcpBinding();
                //set the maximum message size
                tcpBinding.MaxReceivedMessageSize = System.Int32.MaxValue;
                tcpBinding.ReaderQuotas.MaxArrayLength = System.Int32.MaxValue;
                //start the service
                host = new ServiceHost(typeof(ATCMasterControllerImpl));
                host.AddServiceEndpoint(typeof(IATCMasterController), tcpBinding, "net.tcp://" + address);
                host.Open();
                System.Console.WriteLine("Press Enter to exit");
                //keep server running until enter is pressed
                System.Console.ReadLine();
                //exit
                host.Close();
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e.Message);
                if (host != null)
                    host.Close();
                StartServer(address);
            }
        }
    }
}
