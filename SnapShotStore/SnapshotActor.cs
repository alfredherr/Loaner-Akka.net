using System;
using System.Collections.Generic;
using System.Text;
using Akka.Actor;
using System.IO;
using System.Threading;
using System.Runtime.Serialization;
using Akka.Routing;

namespace SnapShotStore
{
    #region Message classes
    public class SaveSnapshot : IConsistentHashable
    {
        public SaveSnapshot(string id, object state)
        {
            this.ID = id;
            this.State = state;
        }

        public object State { get; private set; }
        public string ID { get; private set; }
        public object ConsistentHashKey { get { return ID; } }
    }

    public class RestoreSnapshot
    {
        public RestoreSnapshot(string id)
        {
            this.ID = id;
        }

        public string ID { get; private set; }
    }

    public class SaveComplete { };

    public class RestoreComplete
    {
        public RestoreComplete(object obj)
        {
            this.Obj = obj;
        }

        public object Obj { get; private set; }
    }



    #endregion


    class SnapshotActor : ReceiveActor
    {
        private FileStream stream = null;
//        private BinaryFormatter formatter = new BinaryFormatter();
        int counter = 0;

        // Create the map to the items held in the snapshot store
        const int INITIAL_SIZE = 10000;
        Dictionary<string, long> objectLocation = new Dictionary<string, long>(INITIAL_SIZE);

        public SnapshotActor(long actorID, string dir, ActorSystem system)
        {
            // Open the file that is the snapshot store
            string filename = Path.Combine(dir, "snapshot-store" + actorID + ".bin");
            try
            {
                stream = File.Open(filename, FileMode.Append);
            } catch (Exception e)
            {
                Console.WriteLine($"Error opening the snapshot store file, error: {e.StackTrace}");
            }

            // Commands
            Receive<SaveSnapshot>(cmd => Save(cmd.ID, cmd.State));
            Receive<RestoreSnapshot>(cmd => Restore(cmd.ID));
        }

        public void Save(string id, object state)
        {
            Write(id, state);
            Sender.Tell(new SaveComplete());

            // Dispatch the write to the file and send a msg to the actor when it is complete
            /*
            Thread writer = new Thread(() =>
            {
                Write(state);
                sender.Tell(new SaveComplete());
            });
            writer.Start();
            */
        }

        public void Restore(string id)
        {
            // Find the id in the map to get the position within the file
            var pos = objectLocation[id];
            var sender = Sender;

            // Dispatch the write to the file and send a msg to the actor when it is complete
            Thread reader = new Thread(() =>
            {
                object obj = Read(pos);
                sender.Tell(new RestoreComplete(obj));
            });
            reader.Start();

        }







        public void Write(string id, object obj)
        {
            try {
                // Write the ID of the object to store first so on Initialize() the objects can all be identified correctly
//                formatter.Serialize(stream, id);

                // Get the current location of the file stream so we know where the object is stored on the disk
                long pos = stream.Position;

                // Writre the object to the store
//                formatter.Serialize(stream, obj);
                counter++;

                // Save the information about where the object is located in the file
                objectLocation.Add(id, pos);

                if (counter % 100 == 0)
                {
                    stream.Flush(true);
                }
            }
            catch (SerializationException e)
            {
                Console.WriteLine("Failed to serialize. Reason: " + e.Message);
                throw;
            }
        }


        public object Read(long pos)
        {
            object obj = null;
            try
            {
                // Position to the saved location for the object
                stream.Seek(pos, SeekOrigin.Begin);

                // Read the account to disk
//                obj = formatter.Deserialize(stream);
            }
            catch (SerializationException e)
            {
                Console.WriteLine("Failed to deserialize. Reason: " + e.Message);
                throw;
            }

            return obj;
        }


        private void Initiailize()
        {
            // Loop through the snapshot store file and find all the previous objects written
            // add any objects found to the map
            while (stream.Position < stream.Length)
            {
                try
                {
                    /*
                    // Read the account from disk
                    id = (string)formatter.Deserialize(stream);

                    // Get the current location of the file stream so we know where the object is stored on the disk
                    pos = stream.Position;

                    // Read the account from disk
                    obj = formatter.Deserialize(stream);

                    // Save the information about where the object is located in the file
                    objectLocation.Add(id, pos);
                    */
                }
                catch (SerializationException e)
                {
                    Console.WriteLine("Failed to deserialize. Reason: " + e.Message);
                    throw;
                }
            }
        }

    }
}

