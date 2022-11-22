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


    public static class UserManagement
    {
        //private readonly static MyDatabase db = new MyDatabase();
        private static bool[] CurrentAccessTable;
        private static AccessTableInfo accessTableInfo = new AccessTableInfo();

        public static string UpdateAccessTable(string username)
        {
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
                    //MessageBox.Show(member.Name);
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
            MyDatabase.SendCommand_Read(accessTableInfo);
            CurrentAccessTable = MyDatabase.ReadNextBool();
            return role;



            /*
            WindowsIdentity windowsIdentity = new WindowsIdentity("florian.polomack@integralife.com");
            WindowsPrincipal principal = new WindowsPrincipal(windowsIdentity);
            MessageBox.Show(principal.IsInRole(@"BUILTIN\Users").ToString());
            //*/

            /*
            //PrincipalContext ctx = new PrincipalContext(ContextType.Domain, userName: "x", password: "*", name: "integra-ls.com");
            PrincipalContext ctx_local = new PrincipalContext(ContextType.Machine, userName: "Julien", password: "Integra2021*", name: "DCHLOCPRD077");
            PrincipalContext ctx = new PrincipalContext(ContextType.Domain, userName: "florian.polomack", password: "7Up1n5t5t&", name: "integra-ls.com");
            UserPrincipal user = UserPrincipal.FindByIdentity(ctx, "florian.polomack");
            GroupPrincipal groupPrincipal = GroupPrincipal.FindByIdentity(ctx_local, "MixingApplication_Supervisor");// @"BUILTIN\Users");
            MessageBox.Show(user.IsMemberOf(groupPrincipal).ToString());
            //*/

        }
        public static bool[] GetCurrentAccessTable()
        {
            return CurrentAccessTable;
        }
    }
}
