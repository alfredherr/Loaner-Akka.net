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
using SnapShotStore.Model;

namespace SnapShotStore
{
    /* STUFF TO DO
     * 1. Check that the sequence of event is correct. Snapshot offer does not seem to work
     * 2. Figure out how to store a snaphot with a seqnumber and time
     * 3. Change Initialize so it can read a snapshot with a seq# and time back in 
     */

    internal class FileSnapshotStore3 : SnapshotStore
    {
        // Constants for the offsets when reading and writing SFE's
        private const int MAX_SME_SIZE = 10000;
        private const int SIZE_OF_PERSISTENCE_ID_LENGTH = 4;
        private const int SIZE_OF_SEQ_NUM = 8;
        private const int SIZE_OF_DATE_TIME = 8;
        private const int SIZE_OF_SNAPSHOT_LENGTH = 4;
        private const int SIZE_OF_SNAPSHOT_POSITION = 8;
        private const int SIZE_OF_DELETED = 1;

        // Default constants in case the coniguration item is missing
        private const int NUM_ACTORS = 4;
        private const int MAX_SNAPHOT_SIZE = 40000; // Maximum size for an item to be saved as a snapshot
        private const int NUM_READ_THREADS = 4;

        // Create the map to the items held in the snapshot store
        private const int INITIAL_SIZE = 10000;
        private const int ONE_THREAD = 1;
        private readonly string _dir;
        private readonly int _maxLoadAttempts;
        private readonly Serialization _serialization;
        private readonly MessageDispatcher _streamDispatcher;

        private int _currentStreamId;
        private readonly string _defaultSerializer;

        private long _load;

        // Counters for debug
        private long _loadasync;

        private readonly ILoggingAdapter _log;

        private readonly int _maxSnapshotSize = MAX_SNAPHOT_SIZE;
        private readonly FileStream _readSMEStream;

        // Structure to hold the read streams, one per read thread
        private readonly FileStream[] _readStreams = new FileStream[NUM_READ_THREADS];
        private long _saveasync;
        private readonly FileStream _writeSMEStream;

        private readonly FileStream _writeStream;
        private bool FlushingFiles;

        private Timer FlushTimer;

        private readonly ConcurrentDictionary<string, SnapshotMapEntry> SnapshotMap =
            new ConcurrentDictionary<string, SnapshotMapEntry>(ONE_THREAD, INITIAL_SIZE);

        public FileSnapshotStore3()
        {
            try
            {
                _log = Context.GetLogger();

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

                _writeSMEStream = File.Open(filenameSME, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
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
        [MethodImpl(MethodImplOptions.Synchronized)]
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
            _loadasync++;
            if (_loadasync % 10000 == 0) _log.Info("LoadAsync() - count of calls={0}", _loadasync);

            _log.Debug("LoadAsync() -persistenceId: {0}", persistenceId);

            // Create an empty SnapshotMetadata
            var metadata = new SnapshotMetadata(persistenceId, -1);

            // Pick a read stream to use
            var streamId = getReadStream();

            return RunWithStreamDispatcher(() => Load(streamId, metadata));
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
            _saveasync++;
            if (_saveasync % 10000 == 0) _log.Info("SaveAsync() - count of calls={0}", _saveasync);

            _log.Debug("SaveAsync() - metadata: {0}, metadata.Timestamp {1:yyyy-MMM-dd-HH-mm-ss ffff}", metadata,
                metadata.Timestamp);

            return RunWithStreamDispatcher(() =>
            {
                Save(metadata, snapshot);
                return new object();
            });
        }


        /// <summary>
        ///     Saves the snapshot to the end of the file.
        /// </summary>
        /// <param name="metadata">TBD</param>
        /// <param name="snapshot">TBD</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        protected virtual void Save(SnapshotMetadata metadata, object snapshot)
        {
            _log.Debug("Save() - metadata: {0}, metadata.Timestamp {1:yyyy-MMM-dd-HH-mm-ss ffff}", metadata,
                metadata.Timestamp);

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
        }


        /// <summary>
        ///     Finds the requested snapshot in the file and returns it.
        /// </summary>
//        [MethodImpl(MethodImplOptions.Synchronized)]
        private SelectedSnapshot Load(int streamId, SnapshotMetadata metadata)
        {
            _load++;
            if (_load % 10000 == 0) _log.Info("Load() - count of calls={0}", _load);

            _log.Debug("Load() - metadata: {0}, metadata.Timestamp {1:yyyy-MMM-dd-HH-mm-ss ffff}", metadata,
                metadata.Timestamp);

            // Get the snapshot map entry to locate where in the file the snapshot is stored
            if (!SnapshotMap.TryGetValue(metadata.PersistenceId, out var sme)) return null;

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
                _log.Debug("Snapshot found for id: {0}", metadata.PersistenceId);
            }
            catch (SerializationException e)
            {
                _log.Error("Failed to deserialize. Reason: {0} at {1}", e.Message, e.StackTrace);
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
            _log.Debug("FlushFiles() - flushing the files");
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


        protected override void PreStart()
        {
            _log.Debug("PreStart()");

            // Initialize the Snapshot Map
            InitializeSnaphotMap();

            // Start the timer for flushing the files
            FlushTimer = new Timer(FlushFiles, null, 0, 5000);
        }

        private void InitializeSnaphotMap()
        {
            _log.Info("InitializeSnapshotMap() - STARTED reading the snapshot file to build map");


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

                    // Save the SME in the map
                    if (!SnapshotMap.TryGetValue(sme.Metadata.PersistenceId, out var currentValue))
                        SnapshotMap.TryAdd(sme.Metadata.PersistenceId, sme);
                    else
                        SnapshotMap.TryUpdate(sme.Metadata.PersistenceId, sme, currentValue);

                    _log.Debug(
                        "PreStart() - read metadata from file: {0}, metadata.Timestamp {1:yyyy-MMM-dd-HH-mm-ss ffff}",
                        sme.Metadata, sme.Metadata.Timestamp);
                }
                catch (SerializationException e)
                {
                    Console.WriteLine("Failed to deserialize. Reason: " + e.Message);
                    throw;
                }

            _log.Info("InitializeSnapshotMap() - FINISHED reading the snapshot file to build map");
        }


        protected override void PostStop()
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

        // TODO change back to private after the test
        private void WriteSME(FileStream stream, SnapshotMapEntry sme)
        {
            var buffer = new byte[MAX_SME_SIZE + SIZE_OF_SEQ_NUM + SIZE_OF_DATE_TIME + SIZE_OF_SNAPSHOT_LENGTH +
                                  SIZE_OF_SNAPSHOT_POSITION + SIZE_OF_DELETED];

            // Convert the PersistenceId to bytes and store them in the buffer, leaving space at the beginning to store its length
            var length = Encoding.ASCII.GetBytes(
                sme.Metadata.PersistenceId, 0, sme.Metadata.PersistenceId.Length, buffer,
                SIZE_OF_PERSISTENCE_ID_LENGTH);

            // TODO throw an exception on this
            if (length > buffer.Length)
                Console.WriteLine("Error: PersistenceId is too large");

            // Convert and store the length of the string 
            buffer[0] = (byte) (length >> 24);
            buffer[1] = (byte) (length >> 16);
            buffer[2] = (byte) (length >> 8);
            buffer[3] = (byte) length;

            // Convert the sequence number of the snapshot
            var offset = length + SIZE_OF_PERSISTENCE_ID_LENGTH;
            buffer[offset] = (byte) (sme.Metadata.SequenceNr >> 56);
            buffer[offset + 1] = (byte) (sme.Metadata.SequenceNr >> 48);
            buffer[offset + 2] = (byte) (sme.Metadata.SequenceNr >> 40);
            buffer[offset + 3] = (byte) (sme.Metadata.SequenceNr >> 32);
            buffer[offset + 4] = (byte) (sme.Metadata.SequenceNr >> 24);
            buffer[offset + 5] = (byte) (sme.Metadata.SequenceNr >> 16);
            buffer[offset + 6] = (byte) (sme.Metadata.SequenceNr >> 8);
            buffer[offset + 7] = (byte) sme.Metadata.SequenceNr;

            // Convert and store the timestamp of the snapshot
            var datetime = sme.Metadata.Timestamp.ToBinary();
            offset += SIZE_OF_SEQ_NUM;
            buffer[offset] = (byte) (datetime >> 56);
            buffer[offset + 1] = (byte) (datetime >> 48);
            buffer[offset + 2] = (byte) (datetime >> 40);
            buffer[offset + 3] = (byte) (datetime >> 32);
            buffer[offset + 4] = (byte) (datetime >> 24);
            buffer[offset + 5] = (byte) (datetime >> 16);
            buffer[offset + 6] = (byte) (datetime >> 8);
            buffer[offset + 7] = (byte) datetime;

            // Convert and store the position of the snapshot in the snapshot file
            var position = sme.Position;
            offset += SIZE_OF_DATE_TIME;
            buffer[offset] = (byte) (position >> 56);
            buffer[offset + 1] = (byte) (position >> 48);
            buffer[offset + 2] = (byte) (position >> 40);
            buffer[offset + 3] = (byte) (position >> 32);
            buffer[offset + 4] = (byte) (position >> 24);
            buffer[offset + 5] = (byte) (position >> 16);
            buffer[offset + 6] = (byte) (position >> 8);
            buffer[offset + 7] = (byte) position;


            // Convert and store the length of the snapshot
            var snapLength = sme.Length;
            offset += SIZE_OF_SNAPSHOT_POSITION;
            buffer[offset] = (byte) (snapLength >> 24);
            buffer[offset + 1] = (byte) (snapLength >> 16);
            buffer[offset + 2] = (byte) (snapLength >> 8);
            buffer[offset + 3] = (byte) snapLength;

            // Convert and store the deleted marker that denotes if this snapshot is deleted
            offset += SIZE_OF_SNAPSHOT_LENGTH;
            buffer[offset] = (byte) (sme.Deleted ? 1 : 0);

            // Write to stream
            stream.Write(buffer, 0, offset + 1);
        }


        // TODO change back to private after the test
        private SnapshotMapEntry ReadSME(FileStream stream)
        {
            // Get the Snapshot Map Entry attributes from the file
            var lengthBuffer = new byte[SIZE_OF_PERSISTENCE_ID_LENGTH];
            var buffer = new byte[MAX_SME_SIZE + SIZE_OF_SEQ_NUM + SIZE_OF_DATE_TIME + SIZE_OF_SNAPSHOT_LENGTH +
                                  SIZE_OF_SNAPSHOT_POSITION + SIZE_OF_DELETED];

            // Determine the size of the PersistenceId
            stream.Read(lengthBuffer, 0, lengthBuffer.Length);
            var length = (lengthBuffer[0] << 24) | ((lengthBuffer[1] & 0xFF) << 16) | ((lengthBuffer[2] & 0xFF) << 8) |
                         (lengthBuffer[3] & 0xFF);

            // Get the PersistenceID string from the file
            var bytesToRead = length + SIZE_OF_SEQ_NUM + SIZE_OF_DATE_TIME + SIZE_OF_SNAPSHOT_POSITION +
                              SIZE_OF_SNAPSHOT_LENGTH + SIZE_OF_DELETED;
            var bytesRead = stream.Read(buffer, 0, bytesToRead);
            var persistenceId = Encoding.ASCII.GetString(buffer, 0, length);

            var offset = length;
            long sequenceNum = (buffer[offset++] << 56) |
                               ((buffer[offset++] & 0xFF) << 48) |
                               ((buffer[offset++] & 0xFF) << 40) |
                               ((buffer[offset++] & 0xFF) << 32) |
                               ((buffer[offset++] & 0xFF) << 24) |
                               ((buffer[offset++] & 0xFF) << 16) |
                               ((buffer[offset++] & 0xFF) << 8) |
                               (buffer[offset++] & 0xFF);

            long datetime = (buffer[offset++] << 56) |
                            ((buffer[offset++] & 0xFF) << 48) |
                            ((buffer[offset++] & 0xFF) << 40) |
                            ((buffer[offset++] & 0xFF) << 32) |
                            ((buffer[offset++] & 0xFF) << 24) |
                            ((buffer[offset++] & 0xFF) << 16) |
                            ((buffer[offset++] & 0xFF) << 8) |
                            (buffer[offset++] & 0xFF);

            long position = (buffer[offset++] << 56) |
                            ((buffer[offset++] & 0xFF) << 48) |
                            ((buffer[offset++] & 0xFF) << 40) |
                            ((buffer[offset++] & 0xFF) << 32) |
                            ((buffer[offset++] & 0xFF) << 24) |
                            ((buffer[offset++] & 0xFF) << 16) |
                            ((buffer[offset++] & 0xFF) << 8) |
                            (buffer[offset++] & 0xFF);

            var snapshotLength =
                ((buffer[offset++] & 0xFF) << 24) |
                ((buffer[offset++] & 0xFF) << 16) |
                ((buffer[offset++] & 0xFF) << 8) |
                (buffer[offset++] & 0xFF);

            var deleted = buffer[offset++] == 1 ? true : false;

            return new SnapshotMapEntry(new SnapshotMetadata(persistenceId, sequenceNum, DateTime.FromBinary(datetime)),
                position, snapshotLength, deleted);
        }
    }
}