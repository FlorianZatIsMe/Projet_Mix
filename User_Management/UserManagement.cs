using Database;
using System;
using System.Collections;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using User_Management.Properties;

namespace User_Management
{
    /// <summary>
    /// Structure of the information from other projects required to initialize the main class
    /// </summary>
    public struct IniInfo
    {
        ///<value>Main window of the application. Is used to attach the MessageBoxes to it</value>
        public Window Window;
    }

    /// <summary>
    /// This class allows the update of the access table variable. The access table can then be used by other projects to allow or avoid accesses.
    /// <para>Creation revision: 001</para>
    /// </summary>
    public static class UserManagement
    {
        // Access table variable: contains the values of the applicable line of the database table "access_table"
        private static bool[] CurrentAccessTable;
        // Contains the information of the database table "access_table"
        private static readonly AccessTableInfo accessTableInfo = new AccessTableInfo();
        // Allow the actions logging for debug
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private static IniInfo info;

        /// <summary>
        /// Method called by other project which initializes the variable <see cref="info"/>
        /// </summary>
        /// <param name="info_arg"><see cref="IniInfo"/> variable which contains the applicable settings from the calling project</param>
        public static void Initialize(IniInfo info_arg)
        {
            logger.Debug("Initialize"); // Log a debug message
            info = info_arg;            // Set the info variable from the parameter
        }

        // Method to show MessageBoxes in front of the main window
        private static void ShowMessageBox(string message)
        {
            // If the class was updated (the main window variable was set) then the MessageBox is shown in front of the main window
            if (info.Window != null)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() => // Invoke the action MessageBox
                {
                    MessageBox.Show(info.Window, message);                  // Show the MessageBox in front of the main window
                }));
            }
            // else (if the main window variable wasn't set) then
            else
            {
                MessageBox.Show(message);   // Show the default MessageBox
            }
        }
        /// <summary>
        /// Update the access table variable
        /// </summary>
        /// <param name="username">Name a windows account</param>
        /// <returns>The name of role of the windows account username</returns>
        public static string UpdateAccessTable(string username)
        {
            // Debug log
            logger.Debug("UpdateAccessTable");

            // Represents the active directory
            DirectoryEntry localMachine = new DirectoryEntry("WinNT://" + Environment.MachineName + ",Computer");
            // Temporary group variable
            DirectoryEntry currentGroup;
            // Temporary members variable which contains the members of the temporary group
            object members;
            // array containing:
            // [x, 0] windows groups names
            // [x, 1] application (and database) group names 
            string[,] appGroups = new string[,] {
                { Settings.Default.Group_operator, accessTableInfo.OperatorRole },
                { Settings.Default.Group_supervisor, accessTableInfo.SupervisorRole },
                { Settings.Default.Group_administrator, accessTableInfo.AdministratorRole }};
            // Variable to be returned
            string role = null;

            // For each group in the variable appGroups 
            for (int i = 0; i < appGroups.GetLength(0); i++)
            {
                // Temporary group = information of the current group
                currentGroup = localMachine.Children.Find(appGroups[i, 0], "group");
                // Temporary members = members of the current Temporary group
                members = currentGroup.Invoke("members", null);

                // For each member (groupMember) of the current Temporary group
                foreach (object groupMember in (IEnumerable)members)
                {
                    // member = current member based on groupMember
                    DirectoryEntry member = new DirectoryEntry(groupMember);
                    // If the name of the current member = username (parameter of the function) then update role...
                    if (member.Name.ToLower() == username.ToLower())
                    {
                        // If role wasn't updated then role = applicable application group name
                        if (role == null) role = appGroups[i, 1];
                        // If role was already updated then role = guest application group name
                        else role = accessTableInfo.NoneRole;
                    }
                }
            }
            // If role wasn't updated at the end of the loop, role = guest application group name
            if (role == null) role = accessTableInfo.NoneRole;

            // Update of access table variable...
            // accessTable contains the information of the database table access_table
            AccessTableInfo accessTable = new AccessTableInfo();
            // Set the value of the role column to the value of the variable role previously set
            accessTable.Columns[accessTable.Role].Value = role;
            // Start database task: get the row of the database table access_table for the applicable role (returns bool array)
            Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneBoolRow(accessTable); });
            // Storage of the database result in the variable CurrentAccessTable
            CurrentAccessTable = (bool[])t.Result;

            // Return the application group name
            return role;
        }
        /// <returns>The access table variable</returns>
        public static bool[] GetCurrentAccessTable()
        {
            logger.Debug("GetCurrentAccessTable");  //  Log a debug message
            return CurrentAccessTable;              // Return the access table variable
        }
    }
}




