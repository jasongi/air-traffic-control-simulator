using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ServiceModel;
using System.Runtime.Remoting.Messaging;
using ATCMaster;
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
namespace ATCPresentationGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IATCMasterControllerCallback
    {
        /// <summary>
        /// Master server object
        /// </summary>
        private IATCMasterController m_ATCMaster;

        /// <summary>
        /// List of Airports. Not kept up to date, but used to get the slaveCallbacks from an AirportID
        /// </summary>
        private List<Airport> m_airports;

        /// <summary>
        /// address of the Master server
        /// </summary>
        private string address = "localhost:50002/ATCMaster";
        public delegate List<Airport> NextStepDelegate();

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                m_airports = new List<Airport>();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
                Application.Current.Shutdown();
            }

        }
        /// <summary>
        /// Display connection prompt and connect to master
        /// </summary>
        private void ConnectToMaster()
        {
            try
            {
                //show dialog box to input server address
                var dialog = new Connect(address);
                if (dialog.ShowDialog() != true)
                {
                    dialog.Close();
                }
                else
                {
                    address = dialog.ResponseText;
                    if (address == "")
                    {
                        address = "localhost:50002/ATCMaster";
                    }
                    //connect to the server
                    DuplexChannelFactory<IATCMasterController> IATCMasterFactory;
                    NetTcpBinding tcpBinding = new NetTcpBinding();
                    tcpBinding.MaxReceivedMessageSize = System.Int32.MaxValue;
                    tcpBinding.ReaderQuotas.MaxArrayLength = System.Int32.MaxValue;
                    string sURL = "net.tcp://" + address;
                    IATCMasterFactory = new DuplexChannelFactory<IATCMasterController>(this, tcpBinding, sURL);
                    m_ATCMaster = IATCMasterFactory.CreateChannel();

                    //get the current state of the airports
                    m_airports = m_ATCMaster.GetAirportData();

                    //add airports to the airport listbox
                    listBoxAirports.Items.Clear();
                    foreach (Airport airport in m_airports)
                    {
                        ListBoxItem lbItem = new ListBoxItem();
                        lbItem.Content = airport.name;
                        listBoxAirports.Items.Add(lbItem);
                    }

                    //select the first airport
                    listBoxAirports.SelectedIndex = 0;

                    //refresh the lists
                    this.Refresh();
                }
            } //Exceptions: displays the message then prompts again for a server
            catch (EndpointNotFoundException exception)
            {
                MessageBox.Show("Unable to connect!\n\n" + exception.Message);
                ConnectToMaster();
            }
            catch (NullReferenceException exception)
            {
                MessageBox.Show("Server is not in a state to accept clients - check to see if all your slaves are connected!\n\n" + exception.Message);
                ConnectToMaster();
            }
            catch (TimeoutException exception)
            {
                MessageBox.Show("Server timed out. Try to fix the problem then reconnect.\n\n" + exception.Message);
                ConnectToMaster();
            }
            catch (CommunicationException exception)
            {
                MessageBox.Show("Communication Exception - try restarting the Master/Slaves\n\n" + exception.Message);
                ConnectToMaster();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
                Application.Current.Shutdown();
            }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //display connection prompt and connect to Master
            ConnectToMaster();
        }

        /// <summary>
        /// Calls NextStep on the master server asynchronously when you click Next Step
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonNextStep_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                NextStepAsync();
            }
            catch (NullReferenceException exception)
            {
                MessageBox.Show("Error connecting to server - maybe you haven't conencted yet?\n\n" + exception.Message);
            }
            catch (TimeoutException exception)
            {
                MessageBox.Show("Server timed out. Try to fix the problem then reconnect.\n\n" + exception.Message);
                ConnectToMaster();
            }
            catch (CommunicationException exception)
            {
                MessageBox.Show("Communication Exception - try restarting the Master/Slaves\n\n" + exception.Message);
                ConnectToMaster();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
                Application.Current.Shutdown();
            }
        }

        /// <summary>
        /// Asynchronously calls the Master's NextStep
        /// </summary>
        public void NextStepAsync()
        {
            NextStepDelegate nsDelegate;
            AsyncCallback callbackDel;
            nsDelegate = m_ATCMaster.NextStep;
            callbackDel = this.OnNextStepComplete;
            nsDelegate.BeginInvoke(callbackDel, null);
        }

        /// <summary>
        /// Callback for when the Master has completed moving to the next step
        /// </summary>
        /// <param name="asyncResult"></param>
        public void OnNextStepComplete(IAsyncResult asyncResult)
        {
            try
            {
                //declarations 
                NextStepDelegate del;
                List<Airport> result;

                AsyncResult asyncObj = (AsyncResult)asyncResult;

                //make sure you only call EndInvoke once
                if (asyncObj.EndInvokeCalled == false)
                {
                    del = (NextStepDelegate)asyncObj.AsyncDelegate;

                    //retrieve the airport list
                    result = del.EndInvoke(asyncObj);

                    //update the GUI
                    m_airports = result;
                    this.Refresh();
                }
                asyncObj.AsyncWaitHandle.Close();
            } //if there is a null reference then try to reconnect, otherwise any other exception then shutdown
            catch (NullReferenceException exception)
            {
                MessageBox.Show("Null Reference Exception - Something must be wrong with the server\n\n" + exception.Message);
                ConnectToMaster();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
                Application.Current.Shutdown();
            }
        }

        /// <summary>
        /// update the listsViews with the latest airplane info
        /// </summary>
        private void Refresh()
        {
            //load the currently selected airport
            Airport airport = m_airports.Find(x => x.name.Equals((string)((ListBoxItem)listBoxAirports.Items[listBoxAirports.SelectedIndex]).Content));

            //clear all element from list
            listViewOutbound.Items.Clear();
            listViewInbound.Items.Clear();

            //add all the incoming planes to the inbound ListView
            foreach (Airplane airplane in airport.planeQueuedList)
            {
                listViewInbound.Items.Add(new { ID = airplane.airplaneID.ToString(), State = airplane.state.ToString(), Type = airplane.type.ToString(), Fuel = airplane.fuel.ToString(), Speed = airplane.cruisingKPH.ToString() + " KPH", AirRouteID = airplane.currentAirRouteID, DistanceT = airplane.distanceAlongRoute, DistanceR = airport.IncomingRouteList.Find(x => x.airRouteID == airplane.currentAirRouteID).distanceKM - airplane.distanceAlongRoute, AirportName = "", TimeLanded = airplane.timeLanded.ToString()});
            }

            //add all the departed (outgoing) planes to the outbound ListView
            foreach (Airplane airplane in airport.planeDepartedList)
            {
                listViewOutbound.Items.Add(new { ID = airplane.airplaneID.ToString(), State = airplane.state.ToString(), Type = airplane.type.ToString(), Fuel = airplane.fuel.ToString(), Speed = airplane.cruisingKPH.ToString() + " KPH", AirRouteID = airplane.currentAirRouteID, DistanceT = airplane.distanceAlongRoute, DistanceR = airport.DepartingRouteList.ToList().Find(x => x.airRouteID == airplane.currentAirRouteID).distanceKM - airplane.distanceAlongRoute, AirportName = "", TimeLanded = airplane.timeLanded.ToString() });
            }

            //add all the landed planes to the outbound ListView
            foreach (Airplane airplane in airport.planeLandedList)
            {
                listViewOutbound.Items.Add(new { ID = airplane.airplaneID.ToString(), State = airplane.state.ToString(), Type = airplane.type.ToString(), Fuel = airplane.fuel.ToString(), Speed = 0, AirRouteID = "", DistanceT = "-1", DistanceR = "-1", AirportName = m_airports.Find(x => x.airportID == airplane.currentAirportID).name, TimeLanded = airplane.timeLanded.ToString()});
            }
        }

        /// <summary>
        /// Do not call: Not implemented
        /// </summary>
        public void Reset()
        {
            throw new NotImplementedException("You called a function designed for a slave in the GUI");
        }

        /// <summary>
        /// Do not call: Not implemented
        /// </summary>
        /// <param name="plane"></param>
        public void AddPlane(Airplane plane)
        {
            throw new NotImplementedException("You called a function designed for a slave in the GUI");
        }

        /// <summary>
        /// Do not call: Not implemented
        /// </summary>
        /// <returns></returns>
        public Airport GetAirportData()
        {
            throw new NotImplementedException("You called a function designed for a slave in the GUI");
        }

        /// <summary>
        /// Do not call: Not implemented
        /// </summary>
        void IATCMasterControllerCallback.NextStep()
        {
            throw new NotImplementedException("You called a function designed for a slave in the GUI");
        }

        /// <summary>
        /// updates the ListViews when the selected airport changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listBoxAirports_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                this.Refresh();
            }
            catch (NullReferenceException exception)
            {
                MessageBox.Show("Null Reference Exception - Something must be wrong with the server\n\n" + exception.Message);
                ConnectToMaster();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
                Application.Current.Shutdown();
            }
        }

        /// <summary>
        /// Launches the connect to server dialog
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonServerConnect_Click(object sender, RoutedEventArgs e)
        {
            ConnectToMaster();
        }
    }
}
