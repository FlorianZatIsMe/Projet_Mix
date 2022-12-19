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
    /// This class allows the update of the access table variable. The access table can then be used by other projects to allow or avoid accesses.
    /// <para>
    ///     Creation revision: 001
    /// </para>
    /// </summary>
    public static class UserManagement
    {
        // Access table variable: contains the values of the applicable line of the database table "access_table"
        private static bool[] CurrentAccessTable;
        // Contains the information of the database table "access_table"
        private static readonly AccessTableInfo accessTableInfo = new AccessTableInfo();
        // Allow the actions logging for debug
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
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
            // Temporary variable containing a group
            DirectoryEntry currentGroup;
            // Temporary variable containing the members of a group
            object members;
            // array containing:
            // [x, 0] windows groups names
            // [x, 1] application (and database) group names 
            string[,] appGroups = new string[,] {
                { Settings.Default.Group_operator, accessTableInfo.operatorRole },
                { Settings.Default.Group_supervisor, accessTableInfo.supervisorRole },
                { Settings.Default.Group_administrator, accessTableInfo.administratorRole }};
            // Variable to be returned
            string role = null;

            // For each group of appGroups 
            for (int i = 0; i < appGroups.GetLength(0); i++)
            {
                // currentGroup = information of the current group
                currentGroup = localMachine.Children.Find(appGroups[i, 0], "group");
                // members = members of the group currentGroup
                members = currentGroup.Invoke("members", null);

                // For each member (groupMember) of the current group
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
                        else role = accessTableInfo.noneRole;
                    }
                }
            }
            // If role wasn't updated at the end of the loop, role = guest application group name
            if (role == null) role = accessTableInfo.noneRole;

            // Update of access table variable...
            // accessTable contains the information of the database table access_table
            AccessTableInfo accessTable = new AccessTableInfo();
            // Set the value of the role column to the value of the variable role previously set
            accessTable.columns[accessTable.role].value = role;
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
            logger.Debug("GetCurrentAccessTable");
            return CurrentAccessTable;
        }
    }
}




