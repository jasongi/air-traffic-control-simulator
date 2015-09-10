using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace ATCMaster
{

    /// <summary>
    /// Represents an Airport in the simulation
    /// </summary>
    [DataContract]
    public class Airport
    {
        [DataMember]
        public int airportID;

        [DataMember]
        public string name;

        /// <summary>
        /// List of planes that are landed at the airport
        /// </summary>
        [DataMember]
        public Queue<Airplane> planeLandedList { get; set; }

        /// <summary>
        /// List of planes that are queued for landing at the airport
        /// </summary>
        [DataMember]
        public List<Airplane> planeQueuedList { get; set; }

        /// <summary>
        /// List of planes that have departed and are in transit to another airport
        /// </summary>
        [DataMember]
        public List<Airplane> planeDepartedList { get; set; }

        /// <summary>
        /// List of departing routes
        /// </summary>
        [DataMember]
        public Queue<AirRoute> DepartingRouteList { get; set; }

        /// <summary>
        /// List of incoming routes
        /// </summary>
        [DataMember]
        public List<AirRoute> IncomingRouteList { get; set; }

        /// <summary>
        /// Callback for the server that contains the airport, is not transferred over WCF
        /// </summary>
        [IgnoreDataMember]
        public IATCMasterControllerCallback slaveCallback { get; set; }

        public Airport(int airportID, string name, Queue<Airplane> planeLandedList, Queue<AirRoute> DepartingRouteList, List<AirRoute> IncomingRouteList)
        {
            this.airportID = airportID;
            this.name = name;
            this.planeLandedList = new Queue<Airplane>(planeLandedList);
            this.planeQueuedList = new List<Airplane>();
            this.planeDepartedList = new List<Airplane>();
            this.DepartingRouteList = new Queue<AirRoute>(DepartingRouteList);
            this.IncomingRouteList = new List<AirRoute>(IncomingRouteList);
            this.slaveCallback = null;
        }
    }
}
