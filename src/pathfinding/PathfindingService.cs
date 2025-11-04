// PathfindingService.cs - Asynchronous pathfinding service
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Cathedral.Pathfinding
{
    /// <summary>
    /// Request for pathfinding operation
    /// </summary>
    public class PathfindingRequest
    {
        public int RequestId { get; }
        public int StartNode { get; }
        public int EndNode { get; }
        public IPathGraph Graph { get; }
        public TaskCompletionSource<Path?> CompletionSource { get; }
        public CancellationToken CancellationToken { get; }
        public DateTime RequestTime { get; }

        public PathfindingRequest(int startNode, int endNode, IPathGraph graph, CancellationToken cancellationToken = default)
        {
            RequestId = Guid.NewGuid().GetHashCode();
            StartNode = startNode;
            EndNode = endNode;
            Graph = graph;
            CompletionSource = new TaskCompletionSource<Path?>();
            CancellationToken = cancellationToken;
            RequestTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Asynchronous pathfinding service that processes requests in background threads
    /// </summary>
    public class PathfindingService : IDisposable
    {
        private readonly ConcurrentQueue<PathfindingRequest> _pendingRequests;
        private readonly ConcurrentQueue<PathfindingRequest> _completedRequests;
        private readonly AStar _aStar;
        private readonly CancellationTokenSource _serviceCancellation;
        private readonly Task _backgroundTask;
        private readonly int _maxConcurrentThreads;
        private volatile int _activeThreads;
        private volatile bool _disposed;

        public PathfindingService(int maxConcurrentThreads = 4)
        {
            _pendingRequests = new ConcurrentQueue<PathfindingRequest>();
            _completedRequests = new ConcurrentQueue<PathfindingRequest>();
            _aStar = new AStar();
            _serviceCancellation = new CancellationTokenSource();
            _maxConcurrentThreads = Math.Max(1, maxConcurrentThreads);
            _activeThreads = 0;

            // Start the background processing task
            _backgroundTask = Task.Run(ProcessRequestsAsync, _serviceCancellation.Token);
        }

        /// <summary>
        /// Requests pathfinding between two nodes asynchronously
        /// </summary>
        /// <param name="graph">The graph to search</param>
        /// <param name="startNode">Starting node ID</param>
        /// <param name="endNode">Target node ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task that completes with the found path, or null if no path exists</returns>
        public Task<Path?> FindPathAsync(IPathGraph graph, int startNode, int endNode, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PathfindingService));

            var request = new PathfindingRequest(startNode, endNode, graph, cancellationToken);
            _pendingRequests.Enqueue(request);

            return request.CompletionSource.Task;
        }

        /// <summary>
        /// Gets the number of pending pathfinding requests
        /// </summary>
        public int PendingRequestCount => _pendingRequests.Count;

        /// <summary>
        /// Gets the number of active pathfinding threads
        /// </summary>
        public int ActiveThreadCount => _activeThreads;

        /// <summary>
        /// Background task that processes pathfinding requests
        /// </summary>
        private async Task ProcessRequestsAsync()
        {
            while (!_serviceCancellation.Token.IsCancellationRequested)
            {
                try
                {
                    // Process completed requests first
                    ProcessCompletedRequests();

                    // Start new worker threads if needed
                    if (_pendingRequests.Count > 0 && _activeThreads < _maxConcurrentThreads)
                    {
                        Interlocked.Increment(ref _activeThreads);
                        _ = Task.Run(ProcessSingleRequestAsync, _serviceCancellation.Token);
                    }

                    // Wait a bit before checking again
                    await Task.Delay(10, _serviceCancellation.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in pathfinding service: {ex.Message}");
                    await Task.Delay(100, _serviceCancellation.Token);
                }
            }
        }

        /// <summary>
        /// Processes a single pathfinding request
        /// </summary>
        private async Task ProcessSingleRequestAsync()
        {
            try
            {
                while (!_serviceCancellation.Token.IsCancellationRequested)
                {
                    if (!_pendingRequests.TryDequeue(out var request))
                    {
                        // No more requests, exit this thread
                        break;
                    }

                    try
                    {
                        // Check if request was cancelled
                        if (request.CancellationToken.IsCancellationRequested)
                        {
                            request.CompletionSource.SetCanceled(request.CancellationToken);
                            continue;
                        }

                        // Perform pathfinding
                        var path = await Task.Run(() => _aStar.FindPath(request.Graph, request.StartNode, request.EndNode), 
                                                  request.CancellationToken);

                        // Mark request as completed
                        _completedRequests.Enqueue(request);
                        request.CompletionSource.SetResult(path);
                    }
                    catch (OperationCanceledException)
                    {
                        request.CompletionSource.SetCanceled(request.CancellationToken);
                    }
                    catch (Exception ex)
                    {
                        request.CompletionSource.SetException(new PathfindingException($"Pathfinding failed: {ex.Message}", ex));
                    }
                }
            }
            finally
            {
                Interlocked.Decrement(ref _activeThreads);
            }
        }

        /// <summary>
        /// Processes completed requests (for cleanup/monitoring)
        /// </summary>
        private void ProcessCompletedRequests()
        {
            while (_completedRequests.TryDequeue(out var completedRequest))
            {
                // Here we could add logging, metrics, etc.
                // For now, we just remove them from the queue
            }
        }

        /// <summary>
        /// Disposes the service and cancels all pending operations
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _serviceCancellation.Cancel();

            try
            {
                _backgroundTask.Wait(TimeSpan.FromSeconds(5));
            }
            catch (AggregateException)
            {
                // Ignore cancellation exceptions during shutdown
            }

            _serviceCancellation.Dispose();

            // Cancel any remaining pending requests
            while (_pendingRequests.TryDequeue(out var request))
            {
                request.CompletionSource.SetCanceled();
            }
        }
    }
}