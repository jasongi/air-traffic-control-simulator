using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace ATCMaster
{
    public enum PlaneState { Landed, InTransit, Entering, Circling, Crashed };
    [DataContract]
    public class Airplane
    {
        [DataMember]
        public int airplaneID;

        /// <summary>
        /// Name of the model of the plane
        /// </summary>
        [DataMember]
        public string type;

        /// <summary>
        /// State of the plane (Landed, InTransit, Entering, Circling, Crashed)
        /// </summary>
        [DataMember]
        public PlaneState state;

        /// <summary>
        /// Average speed of the plane when flying (in KPH)
        /// </summary>
        [DataMember]
        public double cruisingKPH;

        /// <summary>
        /// Amount of fuel a plane consumes per hour
        /// </summary>
        [DataMember]
        public double fuelConsPerHour;

        /// <summary>
        /// Amount of fuel currently in the plane's fuel tank
        /// </summary>
        [DataMember]
        public double fuel;

        /// <summary>
        /// ID of the route the plane is currently on
        /// </summary>
        [DataMember]
        public int currentAirRouteID;

        /// <summary>
        /// Route the plane is currently on, not transfered over WCF
        /// </summary>
        [IgnoreDataMember]
        public AirRoute currentAirRoute;

        /// <summary>
        /// Distance (in KM) that the plane is along the route
        /// </summary>
        [DataMember]
        public double distanceAlongRoute;

        /// <summary>
        /// ID of the airport the plane is landed at (-1 if currently flying)
        /// </summary>
        [DataMember]
        public int currentAirportID;

        /// <summary>
        /// Airport the plane is landedn at, not transfered over WCF
        /// </summary>
        [IgnoreDataMember]
        public Airport currentAirport;

        /// <summary>
        /// Time an airplane has been landed at an airport, -1 if currently flying
        /// </summary>
        [DataMember]
        public int timeLanded;

        /// <summary>
        /// Simulation clock, used to check if airplane is at the same time the airport is at
        /// </summary>
        [DataMember]
        public int simTime;

        public Airplane(int airplaneID, string type, double cruisingKPH, double fuelConsPerHour, int currentAirportID)
        {
            this.airplaneID = airplaneID;
            this.type = type;
            this.state = PlaneState.Landed;
            this.cruisingKPH = cruisingKPH;
            this.fuelConsPerHour = fuelConsPerHour;
            this.currentAirRouteID = -1;
            this.distanceAlongRoute = 0;
            this.currentAirportID = currentAirportID;
            this.timeLanded = 0;
            this.fuel = 0;
            this.currentAirport = null;
            this.currentAirRoute = null;
            this.simTime = 0;
        }

        [OnDeserialized]
        public void OnDeserialization(StreamingContext context)
        {
            this.currentAirport = null;
            this.currentAirRoute = null;
        }
    }
}
