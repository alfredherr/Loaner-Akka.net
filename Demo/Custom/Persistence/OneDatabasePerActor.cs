using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using MessagePack;
//using Microsoft.Data.Sqlite;

//Serialiation 
//    https://github.com/neuecc/MessagePack-CSharp
namespace Demo.Custom.Persistence
{
    class OneDatabasePerActor
    {
        public SQLiteConnection sqlite_connection { get; }

        public OneDatabasePerActor(string actorDatabaseName)
        {
            sqlite_connection= new SQLiteConnection( $"Data Source={actorDatabaseName}.db;Version=3;" );
            InitSnapshots();
            InitJournal();
        }

        public void Snapshot(string persistenceId, string className, object item)
        {
            var binaryObject = MessagePackSerializer.Serialize(item);
            sqlite_connection.Open();
            SQLiteCommand sqlite_cmd = sqlite_connection.CreateCommand();
            sqlite_cmd.CommandText = $"INSERT INTO snapshot (PersistenceID, ClassName,Contents) VALUES ('{persistenceId}', '{className}','{binaryObject}');";
            sqlite_cmd.ExecuteNonQuery();
            sqlite_connection.Close();
        }
        public void SaveEvent(string persistenceId, string className, object item)
        {
            var binaryObject = MessagePackSerializer.Serialize(item);
            sqlite_connection.Open();
            SQLiteCommand sqlite_cmd = sqlite_connection.CreateCommand();
            sqlite_cmd.CommandText = $"INSERT INTO joural (PersistenceID, ClassName,Contents) VALUES ('{persistenceId}', '{className}','{binaryObject}');";
            sqlite_cmd.ExecuteNonQuery();
            sqlite_connection.Close();
        }
        private void InitSnapshots()
        {
            sqlite_connection.Open();
            SQLiteCommand sqliteCmd = sqlite_connection.CreateCommand();
            sqliteCmd.CommandText = @"CREATE TABLE IF NOT EXISTS [snapshot] (          
                                       [SequenceID]   INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                                       [PersistenceID] NVARCHAR(2048) NOT NULL,
                                       [ClassName] NVARCHAR(2048) NOT NULL,
                                       [Contents]  BLOB NOT NULL)";
            sqliteCmd.ExecuteNonQuery();
            sqlite_connection.Close();
        }
        private void InitJournal()
        {
            sqlite_connection.Open();

            SQLiteCommand sqliteCmd = sqlite_connection.CreateCommand();

            sqliteCmd.CommandText = @"CREATE TABLE IF NOT EXISTS [joural] (  
                                       [SequenceID]   INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                                       [PersistenceID] NVARCHAR(2048) NOT NULL,
                                       [ClassName] NVARCHAR(2048) NOT NULL,
                                       [Contents]  BLOB NOT NULL)";

            sqliteCmd.ExecuteNonQuery();
            sqlite_connection.Close();
        }

        

    }

}
