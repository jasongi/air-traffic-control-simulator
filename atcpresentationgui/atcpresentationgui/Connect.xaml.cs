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
using System.Windows.Shapes;
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
    /// Interaction logic for Connect.xaml
    /// </summary>
    public partial class Connect : Window
    {
        /// <summary>
        /// Allow MainWindow to get the text put in the server address field
        /// </summary>
        public string ResponseText
        {
            get { return textBoxServer.Text; }
            set { textBoxServer.Text = value; }
        }
        public Connect(string address)
        {
            InitializeComponent();
            textBoxServer.Text = address;
        }

        /// <summary>
        /// Center the dialog to the center of the app
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Application curApp = Application.Current;
            Window mainWindow = curApp.MainWindow;
            this.Left = mainWindow.Left + (mainWindow.Width - this.ActualWidth) / 2;
            this.Top = mainWindow.Top + (mainWindow.Height - this.ActualHeight) / 2;

        }

        /// <summary>
        /// return to the MainWindow
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonConnect_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        /// <summary>
        /// Return to the main window if you press enter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBoxServer_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                DialogResult = true;
            }
        }

    }
}
