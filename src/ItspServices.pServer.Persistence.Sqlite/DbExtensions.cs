﻿using System.Data.Common;

namespace ItspServices.pServer.Persistence.Sqlite
{
    internal static class DbExtensions
    {
        public static void AddParameterWithValue(this DbCommand cmd, string parameterName, object value)
        {
            DbParameter param = cmd.CreateParameter();
            param.ParameterName = parameterName;
            param.Value = value;
            cmd.Parameters.Add(param);
        }
    }
}
