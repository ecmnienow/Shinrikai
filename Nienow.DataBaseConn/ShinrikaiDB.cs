using System.Configuration;
using System.Data;
using FirebirdSql.Data.FirebirdClient;
using System;
using System.Data.Common;

namespace Nienow.DataBaseConn
{
    public class ShinrikaiDB : IDisposable
    {
        #region Constructors

        public ShinrikaiDB()
        {
            DbConnection conn = new FbConnection(this._connString);
            this._conn = conn;
        }

        #endregion

        #region Attributes and Properties

        private DbCommand _db = null;
        public DbCommand DB
        {
            get
            {
                if (this._db == null)
                    this._db = this._conn.CreateCommand();

                return this._db;
            }
        }

        #endregion

        #region Private Fields

        private DbConnection _conn = null;
        private DbDataReader _dataReader = null;

        private string _connString = ConfigurationManager.ConnectionStrings["ShinrikaiDB"].ConnectionString;
//        private string _connString =
//                @"character set=UTF8;
//                  initial catalog=C:\Users\Everton\Desenvolvimento\DATA_BASES\SHINRIKAI.FDB;
//                  data source=localhost;
//                  port=3050;
//                  dialect=3;
//                  user id=SYSDBA;
//                  password=masterkey";

        #endregion

        public void Connect()
        {
            this._conn.Open();
        }

        public void Disconnect()
        {
            //this._dataReader.Close();
            //this._dataReader = null;
            this._conn.Close();
            //this._conn = null;
        }

        public DbCommand CreateCommand(DbTransaction trans)
        {
            var cmm = this._conn.CreateCommand();
            cmm.Transaction = trans;

            return cmm;
        }

        public DbTransaction NewTransaction()
        {
            return _conn.BeginTransaction(IsolationLevel.Unspecified) as FbTransaction;
        }

        public DbParameter NewParameter(string name, DbType dbType)
        {
            return this.NewParameter(this.DB, name, dbType);
        }

        public DbParameter NewParameter(DbCommand cmm, string name, DbType dbType)
        {
            var par = cmm.CreateParameter();
            par.ParameterName = name;
            par.DbType = dbType;

            cmm.Parameters.Add(par);

            return par;
        }

        #region IDisposable Methods

        public void Dispose()
        {
            this.Disconnect();
        }

        #endregion
    }
}
