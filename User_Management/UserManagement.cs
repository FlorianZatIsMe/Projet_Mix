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
    ///  This is the summary
    /// </summary>
    /// <remarks>This is a remark</remarks>
    public static class UserManagement
    {
        /// <summary>
        ///  This is a variable
        /// </summary>
        /// <value>variable description</value>
        private static bool[] CurrentAccessTable;
        private static AccessTableInfo accessTableInfo = new AccessTableInfo();

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        ///  This is a method
        /// </summary>
        /// <param name="username">Description of username parameter</param>
        public static string UpdateAccessTable(string username)
        {
            logger.Debug("UpdateAccessTable");

            string role = null;
            string[,] appGroups = new string[,] {
                { Settings.Default.Group_operator, accessTableInfo.operatorRole },
                { Settings.Default.Group_supervisor, accessTableInfo.supervisorRole },
                { Settings.Default.Group_administrator, accessTableInfo.administratorRole }};
            object members;
            DirectoryEntry currentGroup;
            DirectoryEntry localMachine = new DirectoryEntry("WinNT://" + Environment.MachineName + ",Computer");

            for (int i = 0; i < appGroups.GetLength(0); i++)
            {
                currentGroup = localMachine.Children.Find(appGroups[i, 0], "group");
                members = currentGroup.Invoke("members", null);
                foreach (object groupMember in (IEnumerable)members)
                {
                    DirectoryEntry member = new DirectoryEntry(groupMember);
                    if (member.Name.ToLower() == username.ToLower())
                    {
                        if (role == null) role = appGroups[i, 1];
                        else role = accessTableInfo.noneRole;
                    }
                }
            }

            if (role == null) role = accessTableInfo.noneRole;

            accessTableInfo = new AccessTableInfo();
            accessTableInfo.columns[accessTableInfo.role].value = role;

            Task<object> t = MyDatabase.TaskEnQueue(() => { return MyDatabase.GetOneBoolRow(accessTableInfo); });
            CurrentAccessTable = (bool[])t.Result;
            CurrentAccessTable = (bool[])null;

            return role;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static bool[] GetCurrentAccessTable()
        {
            logger.Debug("GetCurrentAccessTable");
            return CurrentAccessTable;
        }
    }
}




