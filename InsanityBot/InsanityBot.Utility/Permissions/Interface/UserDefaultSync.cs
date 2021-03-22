﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using InsanityBot.Utility.Permissions.Controller;
using InsanityBot.Utility.Permissions.Model;

namespace InsanityBot.Utility.Permissions.Interface
{
    public class UserDefaultSync
    {
        public const String UserPermissionFilePath = "./data";

        public static void SyncUsers()
        {
            String[] users = Directory.GetDirectories(UserPermissionFilePath);
            UInt64[] userIds = new UInt64[users.Length];

            foreach (var v in users)
            {
                String id = v.Split('\\')
                    .Last();
                userIds = userIds.Append(Convert.ToUInt64(id)).ToArray();
            }

            PermissionBackupHandler.BackupUsers(true);

            foreach (var v in userIds)
                Task.Run(() => { UserPermissionUpdater.UpdateUserPermissions(v); });
        }
    }
}
