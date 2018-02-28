using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akka.Dispatch;
using Akka.Event;
using Akka.Persistence;
using Akka.Persistence.Snapshot;
using Akka.Serialization;
using SnapShotStore;

namespace Loaner.SnapShotStore3
{
    /* STUFF TO DO
     * 1. Check that the sequence of event is correct. Snapshot offer does not seem to work
     * 2. Figure out how to store a snaphot with a seqnumber and time
     * 3. Change Initialize so it can read a snapshot with a seq# and time back in
     * 
     * 
     * 
     */

    internal class FileSnapshotStore3 : SnapshotStore
    {
        // Constants for the offsets when reading and writing SFE's
        private const int SIZE_OF_PERSISTENCE_ID_LENGTH = 4;
        private const int SIZE_OF_SEQ_NUM = 8;
        private const int SIZE_OF_DATE_TIME = 8;
        private const int SIZE_OF_SNAPSHOT_LENGTH = 4;
        private const int SIZE_OF_SNAPSHOT_POSITION = 8;
        private const int SIZE_OF_DELETED = 1;

        // Default constants in case the coniguration item is missing
        private const int NUM_ACTORS = 4;
        private const int MAX_SNAPHOT_SIZE = 40000; // Maximum size for an item to be saved as a snapshot
        private const int NUM_READ_THREADS = 3;

        // Create the map to the items held in the snapshot store
        private const int INITIAL_SIZE = 1000000;
        private readonly string _defaultSerializer;
        private readonly string _dir;

        private readonly ILoggingAdapter _log = Context.GetLogger();
        private readonly int _maxLoadAttempts;

        private readonly int _maxSnapshotSize = MAX_SNAPHOT_SIZE;
        private readonly FileStream _readSMEStream;

        // Structure to hold the read streams, one per read thread
        private readonly FileStream[] _readStreams = new FileStream[NUM_READ_THREADS];
        private readonly Serialization _serialization;

        // Locks to prevent thread collision
        private readonly object _smeLock = new object();
        private readonly MessageDispatcher _streamDispatcher;
        private readonly FileStream _writeSMEStream;

        private readonly FileStream _writeStream;

        private readonly ConcurrentDictionary<string, SnapshotMapEntry> SnapshotMap =
            new ConcurrentDictionary<string, SnapshotMapEntry>(NUM_READ_THREADS + 1, INITIAL_SIZE);

        private int _currentStreamId;

        private long _load;

        // Counters for debug
        private long _loadasync;
        private int _readSME;
        private long _save;
        private long _saveasync;
        private int _smeMaxLength = 0;
        private bool FlushingFiles;

        private Timer FlushTimer;

        public FileSnapshotStore3()
        {
            try
            {
                // Get the configuration
                var config = Context.System.Settings.Config.GetConfig("akka.persistence.snapshot-store.jonfile");
                _maxLoadAttempts = config.GetInt("max-load-attempts");

                _streamDispatcher = Context.System.Dispatchers.Lookup(config.GetString("plugin-dispatcher"));
                _dir = config.GetString("dir");
                if (config.GetInt("max-snapshot-size") > 0) _maxSnapshotSize = config.GetInt("max-snapshot-size");
                _log.Info("Max Snapshot Size in bytes = {0}", _maxSnapshotSize);

                _defaultSerializer = config.GetString("serializer");
                _serialization = Context.System.Serialization;

                // Log the configuration parameters
                // TODO remove or use this, depending if we can figure out how to make a router group out of this actor
                _log.Info("This actor name= {0}", Context.Self.Path);

                // Open the file that is the snapshot store
                var filename = Path.Combine(_dir, "file-snapshot-store.bin");
                var filenameSME = Path.Combine(_dir, "file-snapshot-map.bin");
                _log.Info("Opening the snapshot store for this instance, filename = {0}", filename);
                _log.Info("Opening the snapshot map for this instance, filename = {0}", filenameSME);

                // Open the various streams to the two files used to store the information about the snapshots
                _writeStream = File.Open(filename, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);

                // Open a group of streams, one per read thread
                for (var i = 0; i < NUM_READ_THREADS; i++)
                    _readStreams[i] = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                // Open the file so the file pointer can be moved in case an error is detected. 
                //Position to end of file because normally that is where items will be added
                _writeSMEStream = File.Open(filenameSME, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
                _writeSMEStream.Seek(_writeSMEStream.Length, SeekOrigin.Begin);

                _readSMEStream = File.Open(filenameSME, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
            catch (Exception e)
            {
                _log.Error("Error opening the snapshot store file, error: {0}", e.StackTrace);
                throw e;
            }
        }

        protected override Task DeleteAsync(SnapshotMetadata metadata)
        {
            _log.Debug("DeleteAsync() - metadata: {0}, metadata.Timestamp {1:yyyy-MMM-dd-HH-mm-ss ffff}", metadata,
                metadata.Timestamp);
            return RunWithStreamDispatcher(() =>
            {
                Delete(metadata);
                return new object();
            });
        }


        protected override Task DeleteAsync(string persistenceId, SnapshotSelectionCriteria criteria)
        {
            _log.Debug("DeleteAsync() -persistenceId: {0}", persistenceId);

            // Create an empty SnapshotMetadata
            var metadata = new SnapshotMetadata(persistenceId, -1);

            return RunWithStreamDispatcher(() =>
            {
                Delete(metadata);
                return new object();
            });
        }

        /// <summary>
        ///     Deletes a snapshot from the store
        /// </summary>
        /// <param name="metadata">TBD</param>
        protected virtual void Delete(SnapshotMetadata metadata)
        {
            _log.Debug("Delete() - metadata: {0}, metadata.Timestamp {1:yyyy-MMM-dd-HH-mm-ss ffff}", metadata,
                metadata.Timestamp);
        }


        /// <summary>
        ///     Finds the requested snapshot in the file and returns it asynchronously. If no snapshot is found it returns null
        ///     without waiting
        /// </summary>
        protected override Task<SelectedSnapshot> LoadAsync(string persistenceId, SnapshotSelectionCriteria criteria)
        {
            try
            {
                _loadasync++;
                if (_loadasync % 10000 == 0) _log.Info("LoadAsync() - count of calls={0}", _loadasync);

                // Create an empty SnapshotMetadata
                var metadata = new SnapshotMetadata(persistenceId, -1);

                // Pick a read stream to use
                var streamId = getReadStream();

                return RunWithStreamDispatcher(() => Load(streamId, metadata));
            }
            catch (Exception e)
            {
                _log.Error("ERROR in LoadAsync(). Message={0}\nStacktrace={1}", e.Message, e.StackTrace);
                return null;
            }
        }


        /// <summary>
        ///     Generates a streamId which is used to obtain the stream from the set of
        ///     read streams opened on the file
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private int getReadStream()
        {
            // TODO fix this stuff. There is a better way to do this.
            if (_currentStreamId == NUM_READ_THREADS - 1)
            {
                _currentStreamId = 0;
                return _currentStreamId;
            }

            return ++_currentStreamId;
        }

        /// <summary>
        ///     Stores the snapshot in the file asdynchronously
        /// </summary>
        protected override Task SaveAsync(SnapshotMetadata metadata, object snapshot)
        {
            try
            {
                _saveasync++;
                if (_saveasync % 10000 == 0) _log.Info("SaveAsync() - count of calls={0}", _saveasync);

                return RunWithStreamDispatcher(() =>
                {
                    Save(metadata, snapshot);
                    return new object();
                });
            }
            catch (Exception e)
            {
                _log.Error("ERROR in LoadAsync(). Message={0}\nStacktrace={1}", e.Message, e.StackTrace);
                return null;
            }
        }


        /// <summary>
        ///     Saves the snapshot to the end of the file.
        /// </summary>
        /// <param name="metadata">TBD</param>
        /// <param name="snapshot">TBD</param>
        //        [MethodImpl(MethodImplOptions.Synchronized)]
        protected virtual void Save(SnapshotMetadata metadata, object snapshot)
        {
            _save++;
            if (_save % 10000 == 0) _log.Info("Save() - count of calls={0}", _saveasync);

            lock (_smeLock)
            {
                try
                {
                    // Serialize the object that describes the snapshot
                    var serializer = _serialization.FindSerializerFor(snapshot, _defaultSerializer);
                    var bytes = serializer.ToBinary(snapshot);

                    // Get the current location of the file stream so we know where the object is stored on the disk
                    var pos = _writeStream.Position;

                    // Write the Snapshot to disk
                    _writeStream.Write(bytes, 0, bytes.Length);

                    // Save the information about the snapshot and where it is located in the file to the map
                    // Create a snapshot map entry to describe the snapshot
                    var sme = new SnapshotMapEntry(metadata, pos, bytes.Length, false);

                    //                _log.Debug("Save() - persitenceId={0}\tposition={1}\tlength={2}", metadata.PersistenceId, pos, bytes.Length);

                    // Write the SME to disk
                    WriteSME(_writeSMEStream, sme);

                    // Save the SME in the map
                    if (!SnapshotMap.TryGetValue(metadata.PersistenceId, out var currentValue))
                        SnapshotMap.TryAdd(sme.Metadata.PersistenceId, sme);
                    else
                        SnapshotMap.TryUpdate(sme.Metadata.PersistenceId, sme, currentValue);
                }
                catch (SerializationException e)
                {
                    _log.Error("Failed to serialize. Reason: {0}\n{1}", e.Message, e.StackTrace);
                    throw e;
                }
                catch (Exception e)
                {
                    _log.Error("ERROR in Save. Message={0}\n StackTrace={1}", e.Message, e.StackTrace);
                    throw e;
                }
            }
        }


        /// <summary>
        ///     Finds the requested snapshot in the file and returns it.
        /// </summary>
//        [MethodImpl(MethodImplOptions.Synchronized)]
        private SelectedSnapshot Load(int streamId, SnapshotMetadata metadata)
        {
            _load++;
            if (_load % 10000 == 0) _log.Info("Load() - count of calls={0}", _load);


            // Get the snapshot map entry to locate where in the file the snapshot is stored
            if (!SnapshotMap.TryGetValue(metadata.PersistenceId, out var sme)) return null;

//            _log.Debug("Load() - persistenceId={0}\t pos={1}\t length={2}", metadata.PersistenceId, sme.Position, sme.Length);

            // Find the id in the map to get the position within the file
            SelectedSnapshot snapshot = null;
            Monitor.Enter(_readStreams[streamId]);
            try
            {
                // Position to the saved location for the object
                _readStreams[streamId].Seek(sme.Position, SeekOrigin.Begin);

                // Get the snapshot file entry from the file
                var buffer = new byte[sme.Length];
                _readStreams[streamId].Read(buffer, 0, sme.Length);
                var type = typeof(object);
                var serializer = _serialization.FindSerializerForType(type, _defaultSerializer);

                // Create the snapshot to return 
                snapshot = new SelectedSnapshot(sme.Metadata, serializer.FromBinary(buffer, type));
                //                _log.Debug("Snapshot found for id: {0}", metadata.PersistenceId);
            }
            catch (SerializationException e)
            {
                _log.Error("Failed to deserialize. Reason: {0} at {1}", e.Message, e.StackTrace);
                throw e;
            }
            catch (Exception e)
            {
                _log.Error("Serious error while loading snapshot from store. msg={0}\n Postion:{1}", e.Message,
                    e.StackTrace);
                throw e;
            }
            finally
            {
                Monitor.Exit(_readStreams[streamId]);
            }

            return snapshot;
        }


        private Task<T> RunWithStreamDispatcher<T>(Func<T> fn)
        {
            var promise = new TaskCompletionSource<T>();

            _streamDispatcher.Schedule(() =>
            {
                try
                {
                    var result = fn();
                    promise.SetResult(result);
                }
                catch (Exception e)
                {
                    promise.SetException(e);
                }
            });

            return promise.Task;
        }


        private void FlushFiles(object info)
        {
            lock (_smeLock)
            {
                //            _log.Debug("FlushFiles() - flushing the files");
                if (FlushingFiles) return;

                try
                {
                    // Prevent any other timer from performing the flush, incase the flush takes longer than the timer period
                    FlushingFiles = true;

                    // Flush the files to disk to ensure the information is saved. A normal flush only flushes to the OS buffers
                    _writeStream.Flush(true);
                    _writeSMEStream.Flush(true);
                }
                catch (Exception e)
                {
                    _log.Error("Error while flushing the files. Error: {0}\nStack trace: {1}", e.Message, e.StackTrace);
                }
                finally
                {
                    // Enable another timer invocation to flush the files
                    FlushingFiles = false;
                }
            }
        }


        protected override void PreStart()
        {
            try
            {
                _log.Debug("PreStart()");

                // Initialize the Snapshot Map
                InitializeSnaphotMap();

                // Start the timer for flushing the files
                FlushTimer = new Timer(FlushFiles, null, 0, 5000);
            }
            catch (Exception e)
            {
                _log.Error("Serious error in PreStart(). Message={0}\n StackTrace={1}", e.Message, e.StackTrace);
                throw e;
            }
        }

        private void InitializeSnaphotMap()
        {
            _log.Info("InitializeSnapshotMap() - STARTED reading the snapshot file to build map");
            var mapReads = 0;

            // Ensure that the position in the stream is at the start of the file
            _readSMEStream.Seek(0, SeekOrigin.Begin);

            // Loop through the snapshot store file and find all the previous objects written
            // add any objects found to the map
            // TODO must cope with corrupt files or missing items in a file. For example what happens
            // when the ID of the snapshot is writen but the snapshot object itself is missing or corrupt
            while (_readSMEStream.Position < _readSMEStream.Length)
                try
                {
                    // Get the next Snapshot Map Entry from the file
                    // TODO rather than override the existing sme, create a sorted list to save them in.
                    // This will aid in the snapshot selection criteria
                    var sme = ReadSME(_readSMEStream);
                    mapReads++;

                    // Save the SME in the map
                    if (sme != null)
                        if (!SnapshotMap.TryGetValue(sme.Metadata.PersistenceId, out var currentValue))
                        {
                            // Does not exist so add
                            if (!SnapshotMap.TryAdd(sme.Metadata.PersistenceId, sme))
                                _log.Error("Failed to add sme to map. PersistenceId={0}", sme.Metadata.PersistenceId);
                        }
                        else
                        {
                            // Exists so update
                            if (!SnapshotMap.TryUpdate(sme.Metadata.PersistenceId, sme, currentValue))
                                _log.Error("Failed to update sme in map. PersistenceId={0}",
                                    sme.Metadata.PersistenceId);
                        }
                    else
                        break;
                }
                catch (Exception e)
                {
                    _log.Error(
                        "Exception when reading SME entries from file. Only those entries recovered will be used. Potential loss of state!\nMessage={0}. \nLocation={1}",
                        e.Message, e.StackTrace);
                }

            _log.Info(
                "InitializeSnapshotMap() - FINISHED reading the snapshot file to build map. Total map entries read = {0}",
                mapReads + 1);
        }


        protected override void PostStop()
        {
            try
            {
                _log.Debug("PostStop() - flushing and closing the file");

                // Stop the scheduled flush process
                FlushTimer.Dispose();

                // Close the file and ensure that everything is flushed correctly
                _writeStream.Flush(true);
                _writeSMEStream.Flush(true);
                _writeStream.Close();
                _writeSMEStream.Close();

                foreach (var stream in _readStreams) stream.Close();
            }
            catch (Exception e)
            {
                _log.Error("Serious error in PostStop().\nMessage={0}. \nLocation={1}", e.Message, e.StackTrace);
            }
        }

        private void WriteSME(FileStream stream, SnapshotMapEntry sme)
        {
            try
            {
                var pos = stream.Position;

                // Convert the PersistenceId to bytes and store them in the buffer, leaving space at the beginning to store its length
                var temp = Encoding.ASCII.GetBytes(sme.Metadata.PersistenceId);
                var length = temp.Length;
                var buffer = new byte[length + SIZE_OF_PERSISTENCE_ID_LENGTH + SIZE_OF_SEQ_NUM + SIZE_OF_DATE_TIME +
                                      SIZE_OF_SNAPSHOT_LENGTH + SIZE_OF_SNAPSHOT_POSITION + SIZE_OF_DELETED];

                // Convert and store the length of the persistence ID
                var bits = BitConverter.GetBytes(length);
                bits.CopyTo(buffer, 0);

                // This is slower than the original code that placed the bytes from the persistence Id straight into the buffer
                // Copy the bytes into the main buffer
                temp.CopyTo(buffer, SIZE_OF_PERSISTENCE_ID_LENGTH);

                // Convert the sequence number of the snapshot
                var offset = length + SIZE_OF_PERSISTENCE_ID_LENGTH;
                var bits1 = BitConverter.GetBytes(sme.Metadata.SequenceNr);
                bits1.CopyTo(buffer, offset);

                // Convert and store the timestamp of the snapshot
                var datetime = sme.Metadata.Timestamp.ToBinary();
                offset += SIZE_OF_SEQ_NUM;
                var bits2 = BitConverter.GetBytes(datetime);
                bits2.CopyTo(buffer, offset);

                // Convert and store the position of the snapshot in the snapshot file
                var position = sme.Position;
                offset += SIZE_OF_DATE_TIME;
                var bits3 = BitConverter.GetBytes(position);
                bits3.CopyTo(buffer, offset);

                // Convert and store the length of the snapshot
                var snapLength = sme.Length;
                offset += SIZE_OF_SNAPSHOT_POSITION;
                var bits4 = BitConverter.GetBytes(snapLength);
                bits4.CopyTo(buffer, offset);

                // Convert and store the deleted marker that denotes if this snapshot is deleted
                offset += SIZE_OF_SNAPSHOT_LENGTH;
                buffer[offset] = (byte) (sme.Deleted ? 1 : 0);

                // Write to stream
                stream.Write(buffer, 0, offset + 1);
            }
            catch (Exception e)
            {
                _log.Error("Error writing SME, msg = {0}, location = {1}", e.Message, e.StackTrace);
                throw e;
            }
        }
        /*
                private void WriteSME_SAVED(FileStream stream, SnapshotMapEntry sme)
                {
                    var buffer = new byte[MAX_SME_SIZE + SIZE_OF_SEQ_NUM + SIZE_OF_DATE_TIME + SIZE_OF_SNAPSHOT_LENGTH + SIZE_OF_SNAPSHOT_POSITION + SIZE_OF_DELETED];

                    // Convert the PersistenceId to bytes and store them in the buffer, leaving space at the beginning to store its length
                    int length = Encoding.ASCII.GetBytes(
                        sme.Metadata.PersistenceId, 0, sme.Metadata.PersistenceId.Length, buffer, SIZE_OF_PERSISTENCE_ID_LENGTH);

                    // TODO throw an exception on this
                    if (length > buffer.Length)
                        Console.WriteLine("Error: PersistenceId is too large");

                    // Convert and store the length of the string 
                    buffer[0] = (byte)(length >> 24);
                    buffer[1] = (byte)(length >> 16);
                    buffer[2] = (byte)(length >> 8);
                    buffer[3] = (byte)(length);

                    // Convert the sequence number of the snapshot
                    int offset = length + SIZE_OF_PERSISTENCE_ID_LENGTH;
                    buffer[offset] = (byte)((long)(sme.Metadata.SequenceNr) >> 56);
                    buffer[offset + 1] = (byte)((long)(sme.Metadata.SequenceNr) >> 48);
                    buffer[offset + 2] = (byte)((long)(sme.Metadata.SequenceNr) >> 40);
                    buffer[offset + 3] = (byte)((long)(sme.Metadata.SequenceNr) >> 32);
                    buffer[offset + 4] = (byte)((long)(sme.Metadata.SequenceNr) >> 24);
                    buffer[offset + 5] = (byte)((long)(sme.Metadata.SequenceNr) >> 16);
                    buffer[offset + 6] = (byte)((long)(sme.Metadata.SequenceNr) >> 8);
                    buffer[offset + 7] = (byte)((long)(sme.Metadata.SequenceNr));

                    // Convert and store the timestamp of the snapshot
                    long datetime = sme.Metadata.Timestamp.ToBinary();
                    offset += SIZE_OF_SEQ_NUM;
                    buffer[offset] = (byte)((long)(datetime) >> 56);
                    buffer[offset + 1] = (byte)((long)(datetime) >> 48);
                    buffer[offset + 2] = (byte)((long)(datetime) >> 40);
                    buffer[offset + 3] = (byte)((long)(datetime) >> 32);
                    buffer[offset + 4] = (byte)((long)(datetime) >> 24);
                    buffer[offset + 5] = (byte)((long)(datetime) >> 16);
                    buffer[offset + 6] = (byte)((long)(datetime) >> 8);
                    buffer[offset + 7] = (byte)((long)(datetime));

                    // Convert and store the position of the snapshot in the snapshot file
                    long position = sme.Position;
                    offset += SIZE_OF_DATE_TIME;
                    buffer[offset] = (byte)((long)(position) >> 56);
                    buffer[offset + 1] = (byte)((long)(position) >> 48);
                    buffer[offset + 2] = (byte)((long)(position) >> 40);
                    buffer[offset + 3] = (byte)((long)(position) >> 32);
                    buffer[offset + 4] = (byte)((long)(position) >> 24);
                    buffer[offset + 5] = (byte)((long)(position) >> 16);
                    buffer[offset + 6] = (byte)((long)(position) >> 8);
                    buffer[offset + 7] = (byte)((long)(position));


                    // Convert and store the length of the snapshot
                    int snapLength = sme.Length;
                    offset += SIZE_OF_SNAPSHOT_POSITION;
                    buffer[offset] = (byte)((int)(snapLength) >> 24);
                    buffer[offset + 1] = (byte)((int)(snapLength) >> 16);
                    buffer[offset + 2] = (byte)((int)(snapLength) >> 8);
                    buffer[offset + 3] = (byte)((int)(snapLength));

                    // Convert and store the deleted marker that denotes if this snapshot is deleted
                    offset += SIZE_OF_SNAPSHOT_LENGTH;
                    buffer[offset] = (byte)((byte)(sme.Deleted ? 1 : 0));

                    // Write to stream
                    stream.Write(buffer, 0, offset + 1);
                }
        */

        private SnapshotMapEntry ReadSME(FileStream stream)
        {
            _readSME++;
            long pos = 0;

            try
            {
                pos = stream.Position;

                // Get the Snapshot Map Entry attributes from the file
                var lengthBuffer = new byte[SIZE_OF_PERSISTENCE_ID_LENGTH];

                // Determine the size of the PersistenceId
                stream.Read(lengthBuffer, 0, lengthBuffer.Length);
                var length = BitConverter.ToInt32(lengthBuffer, 0);
                var buffer = new byte[length + SIZE_OF_SEQ_NUM + SIZE_OF_DATE_TIME + SIZE_OF_SNAPSHOT_LENGTH +
                                      SIZE_OF_SNAPSHOT_POSITION + SIZE_OF_DELETED];

                // Get the PersistenceID string from the file
                var bytesRead = stream.Read(buffer, 0, buffer.Length);
                var persistenceId = Encoding.ASCII.GetString(buffer, 0, length);

                var offset = length;
                var sequenceNum = BitConverter.ToInt64(buffer, offset);

                offset += SIZE_OF_SEQ_NUM;
                var datetime = BitConverter.ToInt64(buffer, offset);

                offset += SIZE_OF_DATE_TIME;
                var position = BitConverter.ToInt64(buffer, offset);

                offset += SIZE_OF_SNAPSHOT_POSITION;
                var snapshotLength = BitConverter.ToInt32(buffer, offset);

                offset += SIZE_OF_SNAPSHOT_LENGTH;
                var deleted = BitConverter.ToBoolean(buffer, offset);

                return new SnapshotMapEntry(
                    new SnapshotMetadata(persistenceId, sequenceNum, DateTime.FromBinary(datetime)), position,
                    snapshotLength, deleted);
            }
            catch (Exception e)
            {
                _log.Info(
                    "Error when reading SME entries from file. Assuming file was partly written or corrupt so positioning to a known good location and continuing from there. \nMessage={0}. \nLocation={1}",
                    e.Message, e.StackTrace);
                _writeSMEStream.Seek(pos, SeekOrigin.Begin);
            }

            return null;
        }

/*
        private SnapshotMapEntry ReadSME(FileStream stream)
        {
            _readSME++;
            try
            {
                var pos = stream.Position;

                // Get the Snapshot Map Entry attributes from the file
                var lengthBuffer = new byte[SIZE_OF_PERSISTENCE_ID_LENGTH];

                // Determine the size of the PersistenceId
                stream.Read(lengthBuffer, 0, lengthBuffer.Length);
                int length = (lengthBuffer[0] << 24 | (lengthBuffer[1] & 0xFF) << 16 | (lengthBuffer[2] & 0xFF) << 8 | (lengthBuffer[3] & 0xFF));
                var buffer = new byte[length + SIZE_OF_SEQ_NUM + SIZE_OF_DATE_TIME + SIZE_OF_SNAPSHOT_LENGTH + SIZE_OF_SNAPSHOT_POSITION + SIZE_OF_DELETED];

                // Check to see if the length read is greate than written, only works if keep the SW running and do not STOP
                // TODO remove this after debug
                if (length > 1000)
                {
                    // Something is terribly wrong !!
                    _log.Error("Read an SME entry from the file that is longer than something written. Length read = {0}, max length written = {1}, bytes representing length = {2}",
                        length, _smeMaxLength, lengthBuffer);
                }

                // Get the PersistenceID string from the file
                var bytesRead = stream.Read(buffer, 0, buffer.Length);
                var persistenceId = Encoding.ASCII.GetString(buffer, 0, length);

                int offset = length;
                long sequenceNum = buffer[offset] << 56 |
                    (buffer[offset+1] & 0xFF) << 48 |
                    (buffer[offset+2] & 0xFF) << 40 |
                    (buffer[offset+3] & 0xFF) << 32 |
                    (buffer[offset+4] & 0xFF) << 24 |
                    (buffer[offset+5] & 0xFF) << 16 |
                    (buffer[offset+6] & 0xFF) << 8 |
                    (buffer[offset+7] & 0xFF);

                offset = length + SIZE_OF_SEQ_NUM;
                long datetime = buffer[offset] << 56 |
                    (buffer[offset++] & 0xFF) << 48 |
                    (buffer[offset++] & 0xFF) << 40 |
                    (buffer[offset++] & 0xFF) << 32 |
                    (buffer[offset++] & 0xFF) << 24 |
                    (buffer[offset++] & 0xFF) << 16 |
                    (buffer[offset++] & 0xFF) << 8 |
                    (buffer[offset++] & 0xFF);

                long position = buffer[offset++] << 56 |
                    (buffer[offset++] & 0xFF) << 48 |
                    (buffer[offset++] & 0xFF) << 40 |
                    (buffer[offset++] & 0xFF) << 32 |
                    (buffer[offset++] & 0xFF) << 24 |
                    (buffer[offset++] & 0xFF) << 16 |
                    (buffer[offset++] & 0xFF) << 8 |
                    (buffer[offset++] & 0xFF);
                if (position < 0)
                {
                    Console.WriteLine("WTF");
                }
                int snapshotLength =
                    (buffer[offset++] & 0xFF) << 24 |
                    (buffer[offset++] & 0xFF) << 16 |
                    (buffer[offset++] & 0xFF) << 8 |
                    (buffer[offset++] & 0xFF);

                bool deleted = (buffer[offset++] == 1) ? true : false;

//                _log.Debug("READ-SME\tPersistenceId={0}\tsequenceNum={1}\tdateTime={2}\tposition={3}\tsnapshotLength={4}\tdeleted={5}",
//                    persistenceId, sequenceNum, datetime, position, snapshotLength, deleted);
                _log.Debug("READ-SME ENTRY\t PersistenceId={0}\t pos={1}\t length={2}", persistenceId, pos, buffer.Length + lengthBuffer.Length);

                // Check to see if the length read is greater than written, only works if keep the SW running and do not STOP
                // TODO remove this after debug
                if (length > 1000)
                {
                    // Something is terribly wrong !!
                    _log.Error("Read an SME entry from the file that is longer than something written. Length read = {0}, PersistenceId={1}",
                        length, persistenceId);
                }

                return new SnapshotMapEntry(new SnapshotMetadata(persistenceId, sequenceNum, DateTime.FromBinary(datetime)), position, snapshotLength, deleted);

            }
            catch (Exception e)
            {
                _log.Error("Error reading SME, msg = {0}, location = {1}",
                    e.Message, e.StackTrace);
            }

            return null;
        }
*/
    }
}