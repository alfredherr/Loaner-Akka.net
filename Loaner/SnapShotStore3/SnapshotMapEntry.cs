using Akka.Persistence;

namespace Loaner.SnapShotStore3
{
    /// <summary>
    ///     This class holds the information stored in the snapshot map. It identifies the snapshot and the location
    ///     it is stored in the snapshot file
    /// </summary>
    /// <param name="metadata">The metadata of the snapshot.</param>
    /// <param name="position">
    ///     The position the snapshot resides in the file, as an offset from the
    ///     start of the file in bytes.
    /// </param>
    /// <param name="length">The length of the snapshot. Required when reading back the snapshot from the file</param>
    /// <param name="deleted">Marks the map entry as being deleted and ready for reclamation</param>
    public class SnapshotMapEntry
    {
        public SnapshotMapEntry(SnapshotMetadata metadata, long position, int length, bool deleted)
        {
            Metadata = metadata;
            Position = position;
            Length = length;
            Deleted = deleted;
        }

        public SnapshotMetadata Metadata { get; }
        public long Position { get; }
        public int Length { get; }
        public bool Deleted { get; }

        public bool Equals(SnapshotMapEntry sme)
        {
            if (!Metadata.Equals(sme.Metadata)) return false;
            if (Position != sme.Position) return false;
            if (Length != sme.Length) return false;
            if (Deleted != sme.Deleted) return false;
            return true;
        }
    }
}