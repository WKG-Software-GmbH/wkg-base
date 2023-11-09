using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wkg.Collections.Concurrent;

// TODO: a thread-safe bit map with unlimited fixed size
// probably using a linked list of arrays ("chunks")
// make each chunk a fixed size (e.g. 1024 bits)
// each chunk has its own guard token (e.g., ulong counter) to prevent lost updates (ABA problem)
// to determine if all bits are clear or set, we use a separate bit map to track the state of each chunk
// this would allow up to 64 * 1024 bits per cluster (64 chunks).
// clusters can be organized in a tree structure
// each node in the tree has a bit map to track the state of its children
// the expected look-up time is O(log n) if multiple clusters are used and O(1) if only one cluster is used
// inserts and deletions are O(n) as clusters need to be shifted. We'd need a global lock for this. or we just don't support inserts and deletions.
internal class ConcurrentBitMap
{
}
