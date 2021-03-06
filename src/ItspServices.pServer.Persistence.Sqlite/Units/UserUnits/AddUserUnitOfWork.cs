﻿using System.Data;
using System.Data.Common;
using ItspServices.pServer.Abstraction.Models;
using ItspServices.pServer.Abstraction.Units;

namespace ItspServices.pServer.Persistence.Sqlite.Units.UserUnits
{
    class AddUserUnitOfWork : SqliteUnitOfWork<User>, IAddUnitOfWork<User>
    {
        public User Entity { get; private set; }

        public AddUserUnitOfWork(DbProviderFactory dbFactory, string connectionString)
            : base(dbFactory, connectionString)
        {
            Entity = new User();
        }

        protected override void Complete(DbConnection con)
        {
            if (!UserAlreadyExists(con))
            {
                InsertUser(con);
                if (Entity.HasKeys())
                {
                    InsertPublicKeys(con);
                }
            }
        }

        private bool UserAlreadyExists(DbConnection con)
        {
            using (DbCommand query = con.CreateCommand())
            {
                query.AddParameterWithValue("username", Entity.NormalizedUserName);
                query.CommandText = "SELECT Username FROM Users " +
                                    "WHERE Users.Username=@username;";
                using (IDataReader reader = query.ExecuteReader())
                {
                    return reader.Read();
                }
            }
        }

        private void InsertUser(DbConnection con)
        {
            using (DbCommand insert = con.CreateCommand())
            {
                insert.AddParameterWithValue("username", Entity.UserName);
                insert.AddParameterWithValue("password", Entity.PasswordHash);
                insert.AddParameterWithValue("role", Entity.Role);

                insert.CommandText = "INSERT INTO Users(Username, PasswordHash, RoleID) " +
                         "SELECT N, P, ID FROM(SELECT @username AS N, @password AS P) " +
                         "JOIN Roles ON Roles.Name=@role;";
                insert.ExecuteNonQuery();
            }
        }

        private void InsertPublicKeys(DbConnection con)
        {
            using (DbCommand insert = con.CreateCommand())
            {
                int userID = FindUserId(con);
                insert.CommandText = "INSERT INTO PublicKeys ('UserID', 'PublicKeyNumber', 'KeyData', 'Active') VALUES ";
                for (int i = 0; i < Entity.PublicKeys.Count; i++)
                {
                    insert.AddParameterWithValue($"keydata{i}", Entity.PublicKeys[i].AsBase64String());
                    int active = (Entity.PublicKeys[i].Flag == Key.KeyFlag.ACTIVE) ? 1 : 0;

                    insert.CommandText += $"({userID}, -1, @keydata{i}, {active})";
                    if (i >= Entity.PublicKeys.Count - 1)
                    {
                        insert.CommandText += ';';
                    }
                    else
                    {
                        insert.CommandText += ',';
                    }
                }
                insert.ExecuteNonQuery();
            }
        }

        private int FindUserId(DbConnection con)
        {
            using (DbCommand query = con.CreateCommand())
            {
                query.AddParameterWithValue("username", Entity.UserName);
                query.CommandText += "SELECT ID FROM Users WHERE Users.UserName=@username;";
                using (IDataReader reader = query.ExecuteReader())
                {
                    reader.Read();
                    return reader.GetInt32(0);
                }
            }
        }

        public override void Dispose()
        {
            Entity = null;
        }
    }
}
