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
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using Message;

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

        /// <summary>
        /// Update the access table variable
        /// </summary>
        /// <param name="username">Name a windows account</param>
        /// <param name="password">Password of the windows account</param>
        /// <returns>The name of role of the windows account username</returns>
        public static string UpdateAccessTable(string username = null, string password = null)
        {
            // Debug log
            logger.Debug("UpdateAccessTable");

            // array containing:
            // [x, 0] windows groups names
            // [x, 1] application (and database) group names 
            string[,] appGroups = new string[,] {
                { Settings.Default.Group_operator, AccessTableInfo.OperatorRole },
                { Settings.Default.Group_supervisor, AccessTableInfo.SupervisorRole },
                { Settings.Default.Group_administrator, AccessTableInfo.AdministratorRole }};
            // Variable to be returned
            string role = null;
            PrincipalContext ctx;
            try
            {
                if (username == null)
                {
                    ctx = new PrincipalContext(ContextType.Domain, "integra-ls.com");

                }
                else if (password != null)
                {
                    ctx = new PrincipalContext(ContextType.Domain, "integra-ls.com", username, password);
                }
                else
                {
                    logger.Fatal("Faute du programmeur, virez le tout de suite");
                    MyMessageBox.Show("Faute du programmeur, virez le tout de suite");
                    goto End;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                MessageBox.Show(ex.Message);
                goto End;
            }

            UserPrincipal user;
            if (username == null)
            {
                user = UserPrincipal.FindByIdentity(ctx, UserPrincipal.Current.UserPrincipalName);
            }
            else
            {
                user = UserPrincipal.FindByIdentity(ctx, username);
            }

            // find a user

            if (user != null)
            {
                // For each group in the variable appGroups 
                for (int i = 0; i < appGroups.GetLength(0); i++)
                {
                    GroupPrincipal group = GroupPrincipal.FindByIdentity(ctx, appGroups[i, 0]);

                    // check if user is member of that group
                    if (user.IsMemberOf(group))
                    {
                        // If role wasn't updated then role = applicable application group name
                        if (role == null) { role = appGroups[i, 1]; logger.Debug(appGroups[i, 1]); }
                        // If role was already updated then role = guest application group name
                        else { role = AccessTableInfo.NoneRole; logger.Debug(appGroups[i, 1]); }
                    }
                }
            }

        End:
            // If role wasn't updated at the end of the loop, role = guest application group name
            if (role == null) role = AccessTableInfo.NoneRole;
            SetAccess(role);
            // Return the application group name
            return role;
            /*


            MessageBox.Show("Madame");

            // Represents the active directory
            DirectoryEntry localMachine = new DirectoryEntry("WinNT://" + Environment.MachineName + ",Computer");
            // Temporary group variable
            DirectoryEntry currentGroup;
            // Temporary members variable which contains the members of the temporary group
            object members;

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
                        if (role == null) { role = appGroups[i, 1]; logger.Debug(appGroups[i, 1]); }
                        // If role was already updated then role = guest application group name
                        else {role = AccessTableInfo.NoneRole; logger.Debug(appGroups[i, 1]); }
                    }
                }
            }
            // If role wasn't updated at the end of the loop, role = guest application group name
            if (role == null) role = AccessTableInfo.NoneRole;

            SetAccess(role);
            /*
            // Update of access table variable...
            // accessTable contains the information of the database table access_table
            AccessTableInfo accessTable = new AccessTableInfo();
            // Set the value of the role column to the value of the variable role previously set
            accessTable.Columns[accessTable.Role].Value = role;
            // Start database task: get the row of the database table access_table for the applicable role (returns bool array)
            Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneBoolRow(accessTable); });
            // Storage of the database result in the variable CurrentAccessTable
            CurrentAccessTable = (bool[])t.Result;
            */
            // Return the application group name
       /*     return role;*/
        }

        /// <summary>
        /// Update the current access table with the guest rights
        /// </summary>
        /// <returns>True if the table was updated, false otherwirse</returns>
        public static void SetNoneAccess()
        {
            SetAccess(AccessTableInfo.NoneRole);
        }

        private static void SetAccess(string role)
        {
            logger.Debug("SetAccess " + role);
            // Update of access table variable...
            // accessTable contains the information of the database table access_table
            AccessTableInfo accessTable = new AccessTableInfo();
            // Set the value of the role column to the none role
            //accessTable.Columns[accessTable.Role].Value = AccessTableInfo.NoneRole;

            object[] values = new object[accessTable.Ids.Count()];
            values[accessTable.Role] = role;

            // Start database task: get the row of the database table access_table for the applicable role (returns bool array)
            //Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneBoolRow(accessTable); });

            // Start database task: get the row of the database table access_table for the applicable role (returns bool array)
            Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneRow_new(accessTable, values: values); });
            // Storage of the database result in the variable CurrentAccessTable
            //CurrentAccessTable = (bool[])t.Result;

            CurrentAccessTable = new bool[accessTable.Ids.Count()];
            object[] row = (object[])t.Result;

            // If the row from the access table wasn't found, we return and give backup access to the backup if the role is administrator
            if (row == null)
            {
                if (role == AccessTableInfo.AdministratorRole)
                {
                    CurrentAccessTable[AccessTableInfo.Backup] = true;
                }

                return;
            }

            for (int i = 0; i < accessTable.Ids.Count(); i++)
            {
                //logger.Trace(i.ToString() + " - " + row[i].ToString());
                try
                {
                    CurrentAccessTable[i] = Convert.ToBoolean(row[i]);
                }
                catch (Exception)
                {
                    CurrentAccessTable[i] = false;
                }
            }
        }

        /// <returns>The access table variable</returns>
        public static bool[] GetCurrentAccessTable()
        {
            logger.Debug("GetCurrentAccessTable");  //  Log a debug message
            return CurrentAccessTable;              // Return the access table variable
        }
    }
}




