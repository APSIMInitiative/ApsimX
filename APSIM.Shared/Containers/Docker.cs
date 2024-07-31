using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using APSIM.Shared.Documentation.Extensions;
using APSIM.Shared.Extensions.Collections;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace APSIM.Shared.Containers
{
    /// <summary>
    /// Encapsulates a docker client.
    /// </summary>
    public class Docker
    {
        /// <summary>
        /// Docker client, used to interact with the docker daemon.
        /// </summary>
        private DockerClient client;

        /// <summary>
        /// Callback to be invoked when stdout is received from a container.
        /// </summary>
        private readonly Action<string> outputHandler;

        /// <summary>
        /// Handler for warnings from docker.
        /// </summary>
        private Action<string> warningHandler;

        /// <summary>
        /// Callback to be invoked when stderr is received from a container.
        /// </summary>
        private Action<string> errorHandler;

        /// <summary>
        /// Create a new docker client instance.
        /// </summary>
        /// <param name="outputHandler">Handler for receiving stdout from the container.</param>
        /// <param name="warningHandler">Handler for warnings from docker.</param>
        /// <param name="errorHandler">Callback for stderr from the container.</param>
        public Docker(Action<string> outputHandler = null, Action<string> warningHandler = null, Action<string> errorHandler = null)
        {
            client = new DockerClientConfiguration().CreateClient();
            this.outputHandler = outputHandler;
            this.warningHandler = warningHandler;
            this.errorHandler = errorHandler;
        }

        /// <summary>
        /// Pull an image from dockerhub.
        /// </summary>
        /// <param name="image">Name of the image (owner/organisation).</param>
        /// <param name="tag">Tag to be pulled.</param>
        /// <param name="cancelToken">Cancellation token.</param>
        public async Task PullImageAsync(string image, string tag, CancellationToken cancelToken)
        {
            ImagesCreateParameters imageParams = new ImagesCreateParameters()
            {
                FromImage = image,
                Tag = tag,
            };
            AuthConfig auth = new AuthConfig();
            IProgress<JSONMessage> progress = new Progress<JSONMessage>(m => Console.WriteLine(m.Status));
            await client.Images.CreateImageAsync(imageParams, auth, progress, cancelToken);
        }

        /// <summary>
        /// Run a container. Does NOT pull the container - the assumption is that
        /// the container already exists..
        /// </summary>
        /// <param name="image"></param>
        /// <param name="entrypoint"></param>
        /// <param name="args"></param>
        /// <param name="volumes"></param>
        /// <param name="environment"></param>
        /// <param name="workingDir"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        public async Task RunContainerAsync(string image, string entrypoint, IEnumerable<string> args, IReadOnlyList<Volume> volumes, Dictionary<string, string> environment, string workingDir, CancellationToken cancelToken)
        {
            CreateContainerParameters parameters = new CreateContainerParameters()
            {
                Image = image,
                Entrypoint = entrypoint.ToEnumerable().AppendMany(args).ToList(),
                Env = environment?.Select((x, y) => $"{x}={y}")?.ToList(),
                Tty = false,
                HostConfig = new HostConfig()
                {
                    Binds = volumes.Select(v => $"{v.SourcePath}:{v.DestinationPath}").ToList()
                },
                WorkingDir = workingDir
            };

            // Create the container.
            CreateContainerResponse container = await client.Containers.CreateContainerAsync(parameters, cancelToken);

            try
            {
                // Report any warnings from the docker daemon.
                foreach (string warning in container.Warnings)
                    warningHandler(warning);

                // Start the container, and wait for it to exit.
                await client.Containers.StartContainerAsync(container.ID, new ContainerStartParameters(), cancelToken);

                // Watch stdout and/or stderr as necessary.
                Task stdoutListener = WatchStdoutStreamAsync(container.ID, cancelToken);
                Task stderrListener = WatchStderrStreamAsync(container.ID, cancelToken);

                // Wait for the container to exit.
                ContainerWaitResponse waitResponse = await client.Containers.WaitContainerAsync(container.ID, cancelToken);

                // Wait for output listeners if cancellation has not been requested.
                if (!cancelToken.IsCancellationRequested)
                {
                    await stdoutListener.ConfigureAwait(false);
                    await stderrListener.ConfigureAwait(false);
                }

                // If cancellation isn't requested, ensure the container exited gracefully.
                if (!cancelToken.IsCancellationRequested && waitResponse.StatusCode != 0)
                {
                    (string stdout, string stderr) = await GetContainerLogsAsync(container.ID, parameters.Tty, cancelToken);
                    StringBuilder output = new StringBuilder();
                    output.AppendLine(stdout);
                    output.AppendLine(stderr);
                    throw new Exception($"Container exited with non-zero exit code. Container log:\n{output}");
                }
            }
            finally
            {
                ContainerRemoveParameters removeParameters = new ContainerRemoveParameters()
                {
                    RemoveVolumes = true,
                    Force = true
                };
                ContainerKillParameters killParameters = new ContainerKillParameters()
                {
                };
                // Only attempt to kill the container if it's still running.
                if (await IsRunning(container.ID))
                {
                    // The container may have exited between the check above and the following kill line.
                    // Wrap in try/catch just in case.
                    try
                    {
                        await client.Containers.KillContainerAsync(container.ID, killParameters);
                    }
                    catch
                    {}
                }
                await client.Containers.RemoveContainerAsync(container.ID, removeParameters);
            }
        }

        /// <summary>
        /// Check if a container is running.
        /// </summary>
        /// <param name="id">ID of the container to be checked.</param>
        private async Task<bool> IsRunning(string id)
        {
            IDictionary<string, bool> idFilter = new Dictionary<string, bool>() { { id, true } };
            Dictionary<string, IDictionary<string, bool>> filters = new Dictionary<string, IDictionary<string, bool>>()
            {
                { "id", idFilter }
            };
            ContainersListParameters parameters = new ContainersListParameters()
            {
                All = true,
                Filters = filters
            };
            IList<ContainerListResponse> containers = await client.Containers.ListContainersAsync(parameters);

            // If it doesn't exist it's not running.
            if (containers.Count < 1)
                return false;

            // todo: could we ever have >1 container matching these filters?

            ContainerListResponse response = containers[0];
            return response.State == "running";
        }

        /// <summary>
        /// Create a cancellable task to watch the stdout stream from a running
        /// container. The <see cref="outputHandler"/> callback will be invoked
        /// whenever we receive a message from the container.
        /// </summary>
        /// <param name="id">ID of the container.</param>
        /// <param name="cancelToken">Cancellation token.</param>
        private async Task WatchStdoutStreamAsync(string id, CancellationToken cancelToken)
        {
            if (outputHandler == null)
                return;
            await WatchOutputStreamsAsync(id, true, false, outputHandler, cancelToken);
        }

        /// <summary>
        /// Create a cancellable task to watch the stderr stream from a running
        /// container. The <see cref="errorHandler"/> callback will be invoked
        /// whenever we receive a message from the container.
        /// </summary>
        /// <param name="id">ID of the container.</param>
        /// <param name="cancelToken">Cancellation token.</param>
        private async Task WatchStderrStreamAsync(string id, CancellationToken cancelToken)
        {
            if (errorHandler == null)
                return;
            await WatchOutputStreamsAsync(id, false, true, errorHandler, cancelToken);
        }

        /// <summary>
        /// Create a task to monitor the stdout and/or stderr streams from a
        /// running container, and invoke the given callback whenever data is
        /// written to the stream(s).
        /// </summary>
        /// <param name="id">ID of the container.</param>
        /// <param name="stdout">Monitor the container's stdout stream?</param>
        /// <param name="stderr">Monitor the container's stderr stream?</param>
        /// <param name="handler">Message callback.</param>
        /// <param name="cancelToken">Cancellation tokne.</param>
        private async Task WatchOutputStreamsAsync(string id, bool stdout, bool stderr, Action<string> handler, CancellationToken cancelToken)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            ContainerLogsParameters logParameters = new ContainerLogsParameters()
            {
                ShowStdout = stdout,
                ShowStderr = stderr,
                Follow = true
            };
            Progress<string> callback = new Progress<string>(msg =>
            {
                try
                {
                    handler(msg);
                }
                catch (Exception error)
                {
                    Console.WriteLine($"Error in output callback:\n{error}");
                }
            });
            await client.Containers.GetContainerLogsAsync(id, logParameters, cancelToken, callback);
        }

        /// <summary>
        /// Read and return container logs as a tuple of (stdout, stderr).
        /// </summary>
        /// <param name="id">Container ID.</param>
        /// <param name="tty">Was the container started with TTY enabled?</param>
        /// <param name="cancelToken">Cancellation token.</param>
        private async Task<(string, string)> GetContainerLogsAsync(string id, bool tty, CancellationToken cancelToken)
        {
            ContainerLogsParameters logParameters = new ContainerLogsParameters()
            {
                ShowStdout = true,
                ShowStderr = true,
                Follow = false
            };
            using (MultiplexedStream logStream = await client.Containers.GetContainerLogsAsync(id, tty, logParameters, cancelToken))
                return await logStream.ReadOutputToEndAsync(cancelToken);
        }
    }
}
