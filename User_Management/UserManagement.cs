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

namespace User_Management
{


    public static class UserManagement
    {
        //private readonly static MyDatabase db = new MyDatabase();
        private static bool[] CurrentAccessTable;

        public static string UpdateAccessTable(string username)
        {
            string role = null;
            string[,] appGroups = new string[,] {
                { "MixingApplication_Operator", "operator" },
                { "MixingApplication_Supervisor", "supervisor" },
                { "MixingApplication_Administrator", "administrator" }};
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
                        else role = "none";
                    }
                }
            }

            if (role == null) role = "none";

            MyDatabase.SendCommand_Read("access_table", whereColumns: new string[] { "role" }, whereValues: new string[] { role });
            CurrentAccessTable = MyDatabase.ReadNextBool();
            //MessageBox.Show(CurrentAccessTable[0].ToString() + CurrentAccessTable[1].ToString() + CurrentAccessTable[2].ToString() + CurrentAccessTable.Length.ToString());
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
