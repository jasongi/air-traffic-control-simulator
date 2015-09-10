using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Runtime.Remoting.Messaging;
using System.Runtime.CompilerServices;
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
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
    class ATCMasterControllerImpl : IATCMasterController
    {
        public delegate void AirportDelegate();
        public delegate Airport GetAirportDelegate();

        /// <summary>
        /// List of airports (with references to the slave servers)
        /// </summary>
        private List<Airport> m_airports;

        /// <summary>
        /// List of airports that are yet to be allocated to a slave
        /// </summary>
        private Queue<Airport> m_airportAllocationQueue;

        /// <summary>
        /// Current time elapsed in the simulation
        /// </summary>
        private int m_SimTime;

        /// <summary>
        /// Number of airports in the database
        /// </summary>
        private int m_numAirports;

        /// <summary>
        /// Load the database into Airport/Airplane/Airroute objects
        /// </summary>
        private void Initialise()
        {
            try
            {
                //initialise objects
                m_SimTime = 0;
                int[] airportList;
                m_airports = new List<Airport>();
                m_airportAllocationQueue = new Queue<Airport>();

                //create new database object
                ATCDatabase.ATCDB initialDB = new ATCDatabase.ATCDB();

                //get number of airports
                m_numAirports = initialDB.GetNumAirports();

                //get list of airports
                airportList = initialDB.GetAirportIDList();

                //iterate through each airport and get all the data associated with the airport and instanciate those objects
                foreach (int airportID in airportList)
                {
                    //declarations
                    string airportName;
                    int[] airplaneList, airRouteList;
                    Queue<Airplane> landedList = new Queue<Airplane>();
                    Queue<AirRoute> departingRoutes = new Queue<AirRoute>();
                    List<AirRoute> incomingRoutes = new List<AirRoute>();

                    //get all airplane ids in current airport
                    airplaneList = initialDB.GetAirplaneIDsForAirport(airportID);

                    //get all the departing AirRoutes for the airport
                    airRouteList = initialDB.GetDepartingAirRouteIDsForAirport(airportID);

                    //go through each airplane, get it's info and construct the object
                    foreach (int airplaneID in airplaneList)
                    {
                        string planeType;
                        double cruisingKPH, fuelConsPerHour;
                        int initAirportID;
                        initialDB.LoadAirplane(airplaneID, out planeType, out cruisingKPH, out fuelConsPerHour, out initAirportID);
                        landedList.Enqueue(new Airplane(airplaneID, planeType, cruisingKPH, fuelConsPerHour, airportID));
                    }

                    //go through each departing AirRoute and construct the AirRoute object
                    foreach (int airRouteID in airRouteList)
                    {
                        int fromAirportID, toAirportID;
                        double distanceKM;
                        initialDB.LoadAirRoute(airRouteID, out fromAirportID, out toAirportID, out distanceKM);
                        departingRoutes.Enqueue(new AirRoute(airRouteID, fromAirportID, toAirportID, distanceKM));
                    }

                    //go through each airport, get all the departing AirRoutes and if the destination is the current airport
                    //then add it to the incomingRoutes list
                    foreach (int id in airportList)
                    {
                        int[] routeList = initialDB.GetDepartingAirRouteIDsForAirport(id);
                        foreach (int airRouteID in routeList)
                        {
                            int fromAirportID, toAirportID;
                            double distanceKM;
                            initialDB.LoadAirRoute(airRouteID, out fromAirportID, out toAirportID, out distanceKM);
                            if (toAirportID == airportID)
                            {
                                incomingRoutes.Add(new AirRoute(airRouteID, fromAirportID, toAirportID, distanceKM));
                            }
                        }

                    }

                    //finally load the airport name and then add it to the list of airports to be allocated
                    initialDB.LoadAirport(airportID, out airportName);
                    m_airportAllocationQueue.Enqueue(new Airport(airportID, airportName, landedList, departingRoutes, incomingRoutes));
                }
            }
            catch (Exception exception)
            {
                System.Console.WriteLine(exception.Message);
                Initialise();
            }
        }

        ATCMasterControllerImpl()
        {
            Initialise();
        }

        /// <summary>
        /// //Add callback to Master and get allocated airport
        /// </summary>
        /// <returns>Airport assigned to the slave</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public Airport InitialiseSlave()
        {
            try
            {
                //check if all the airports are already allocated
                if (m_numAirports > m_airports.Count)
                {
                    //add the slave to the airport list and return the airport object
                    Airport airport = m_airportAllocationQueue.Dequeue();
                    airport.slaveCallback = OperationContext.Current.GetCallbackChannel<IATCMasterControllerCallback>();
                    m_airports.Add(airport);
                    return airport;
                }
                else
                {
                    //there are no airports left to allocate
                    return null;
                }
            }
            catch (Exception exception)
            {
                System.Console.WriteLine(exception.Message);
                return null;
            }
        }

        /// <summary>
        /// Get the current state of the airports
        /// </summary>
        /// <returns>List of Airport objects in their current state</returns>
        public List<Airport> GetAirportData()
        {
            try
            {
                //declarations and initialisations
                List<Airport> airportValues = new List<Airport>();
                Queue<GetAirportDelegate> workerQueue = new Queue<GetAirportDelegate>();
                Queue<IAsyncResult> asyncQueue = new Queue<IAsyncResult>();

                //go through each slave, create a delegate and asyncronously call GetAirportData
                foreach (Airport airport in m_airports)
                {
                    GetAirportDelegate aDelegate = airport.slaveCallback.GetAirportData;
                    IAsyncResult asyncObj = aDelegate.BeginInvoke(null, null);
                    workerQueue.Enqueue(aDelegate);
                    asyncQueue.Enqueue(asyncObj);
                }

                //Call endinvoke on each slave but keep the AsyncResult so you can close the WaitHandle
                foreach (GetAirportDelegate aDelegate in workerQueue)
                {
                    airportValues.Add(aDelegate.EndInvoke(asyncQueue.Peek()));
                    //add back to the back of the queue for WaitHandling
                    asyncQueue.Enqueue(asyncQueue.Dequeue());
                }
                //Call AsyncWaitHandle.Close on each of the AsyncResults
                foreach (IAsyncResult asyncObj in asyncQueue)
                {
                    asyncObj.AsyncWaitHandle.Close();
                }
                //return the list of Airports
                return airportValues;
            }
            catch (Exception exception)
            {
                System.Console.WriteLine(exception.Message);
                try
                {
                    foreach (Airport a in m_airports)
                    {
                        a.slaveCallback.Reset();
                    }
                }
                catch (Exception e)
                {
                    System.Console.WriteLine(e.Message);
                }
                Initialise();
                return null;
            }
        }

        /// <summary>
        /// Step the entire simulation by 15 mins. Calls each of the slaves in paralell.
        /// </summary>
        /// <returns>List of the Airport objects at the end of the step</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public List<Airport> NextStep()
        {
            try
            {
                //check to see if all the airports have been allocated, if not then return null
                if (m_numAirports == m_airports.Count)
                {
                    //declarations and initialisations
                    Queue<AirportDelegate> workerQueue = new Queue<AirportDelegate>();
                    Queue<IAsyncResult> asyncQueue = new Queue<IAsyncResult>();

                    //go through each slave, create a delegate and asyncronously call NextStep
                    foreach (Airport airport in m_airports)
                    {
                        AirportDelegate aDelegate = airport.slaveCallback.NextStep;
                        IAsyncResult asyncObj = aDelegate.BeginInvoke(null, null);
                        workerQueue.Enqueue(aDelegate);
                        asyncQueue.Enqueue(asyncObj);
                    }
                    //Call eninvoke on each slave but keep the AsyncResult so you can close the WaitHandle
                    foreach (AirportDelegate aDelegate in workerQueue)
                    {
                        aDelegate.EndInvoke(asyncQueue.Peek());
                        //add back to the back of the queue for WaitHandling
                        asyncQueue.Enqueue(asyncQueue.Dequeue());
                    }
                    //Call AsyncWaitHandle.Close on each of the AsyncResults
                    foreach (IAsyncResult asyncObj in asyncQueue)
                    {
                        asyncObj.AsyncWaitHandle.Close();
                    }

                    //increment the clock
                    m_SimTime = m_SimTime + 15;

                    //now fetch the airport data from each slave 
                    return GetAirportData();
                }
                else
                {
                    return null;
                }
            }
            catch (Exception exception)
            {
                System.Console.WriteLine(exception.Message);
                try
                {
                    foreach (Airport a in m_airports)
                    {
                        a.slaveCallback.Reset();
                    }
                }
                catch (Exception e)
                {
                    System.Console.WriteLine(e.Message);
                }
                Initialise();
                return null;
            }
        }

        /// <summary>
        /// Called by a slave to pass a Airplane from the slave to another slave
        /// </summary>
        /// <param name="airplane">The Airplane to pass</param>
        /// <param name="airportID"></param>
        public void PassPlane(Airplane airplane, int airportID)
        {
            try
            {
                m_airports.Find(x => x.airportID == airportID).slaveCallback.AddPlane(airplane);
            }
            catch (Exception exception)
            {
                System.Console.WriteLine(exception.Message);
                try
                {
                    foreach (Airport a in m_airports)
                    {
                        a.slaveCallback.Reset();
                    }
                }
                catch (Exception e)
                {
                    System.Console.WriteLine(e.Message);
                }
                Initialise();
            }
        }
    }
}
