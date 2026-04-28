using Nienow.DataBaseConn;
using System;
using System.Text;

namespace Nienow.Shinrikai.Dictionary.DB
{
    public class DBDictionary : IDisposable
    {
        #region Constructors

        public DBDictionary(DBConnectionConfig dbConnectionConfig)
        {
            this._db = new ShinrikaiDB(dbConnectionConfig);
        }

        #endregion

        #region Private Fields

        private readonly ShinrikaiDB _db;

        #endregion

        #region Public Methods

        public IDisposable Connect()
        {
            this._db.Connect();
            return this;
        }

        internal byte[] GetAudio(int audioId)
        {
            var sql = new StringBuilder();
            sql.AppendLine("SELECT VCB_AUDIO_1 FROM VOCABULARY WHERE VCB_ID = " + audioId);

            this._db.DB.CommandText = sql.ToString();

            var audio = this._db.DB.ExecuteScalar() as byte[];

            return audio;
        }

        public byte[] GetImage(int imageId)
        {
            var sql = new StringBuilder();
            sql.AppendLine("SELECT VCB_IMAGE_1 FROM VOCABULARY WHERE VCB_ID = " + imageId);

            this._db.DB.CommandText = sql.ToString();

            var image = this._db.DB.ExecuteScalar() as byte[];

            return image;
        }

        #endregion

        #region IDisposable Methods

        public void Dispose()
        {
            this._db.Disconnect();
        }

        #endregion
    }
}
