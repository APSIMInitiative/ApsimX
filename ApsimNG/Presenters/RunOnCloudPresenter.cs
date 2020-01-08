using System;
using System.Text;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using UserInterface.Views;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Storage;
using System.Security.Cryptography;
using ApsimNG.Cloud;
using Microsoft.Azure.Batch.Common;
using Models.Core;
using System.Linq;
using ApsimNG.Cloud.Azure;
using System.Threading.Tasks;
using System.Threading;

namespace UserInterface.Presenters
{
    public class RunOnCloudPresenter : IPresenter
    {
        /// <summary>The new azure job view</summary>
        private RunOnCloudView view;
        
        /// <summary>The explorer presenter</summary>
        private ExplorerPresenter presenter;

        /// <summary>The model which we want to run on Azure.</summary>
        private IModel model;

        /// <summary>Cloud interface responsible for job submission.</summary>
        private ICloudInterface cloudInterface;

        /// <summary>Allows job submission to be cancelled.</summary>
        private CancellationTokenSource cancellation;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public RunOnCloudPresenter()
        {
            cloudInterface = new AzureInterface();
            cancellation = new CancellationTokenSource();
        }

        /// <summary>
        /// Attaches this presenter to a view.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="view"></param>
        /// <param name="parentPresenter"></param>
        public void Attach(object model, object view, ExplorerPresenter parentPresenter)
        {
            this.presenter = parentPresenter;
            this.view = view as RunOnCloudView;

            this.view.SubmitJob += SubmitJob;
            this.view.CancelSubmission += CancelJobSubmission;
            
            this.model = model as IModel;
            this.view.JobName = this.model.Name;
        }

        public void Detach()
        {
            view.SubmitJob -= SubmitJob;
            view.CancelSubmission -= CancelJobSubmission;
        }

        private async Task SubmitJob(object sender, EventArgs args)
        {
            JobParameters job = new JobParameters
            {
                ID = Guid.NewGuid(),
                DisplayName = view.JobName,
                Model = model,
                ApsimXPath = view.ApsimXPath,
                SendEmail = view.SendEmail,
                EmailRecipient = view.EmailRecipient,
                CpuCount = view.CpuCount,
                SaveModelFiles = view.SaveModelFiles,
                ModelPath = view.ModelPath,
                CoresPerProcess = 1,
                ApsimXVersion = Path.GetFileName(view.ApsimXPath).Substring(Path.GetFileName(view.ApsimXPath).IndexOf('-') + 1),
                JobManagerShouldSubmitTasks = true,
                AutoScale = true,
                MaxTasksPerVM = 16,
            };

            if (string.IsNullOrWhiteSpace(job.DisplayName))
                throw new Exception("A description is required");

            if (string.IsNullOrWhiteSpace(job.ApsimXPath))
                throw new Exception("Invalid path to apsim");

            if (!Directory.Exists(job.ApsimXPath) && !File.Exists(job.ApsimXPath))
                throw new Exception($"File or Directory not found: '{job.ApsimXPath}'");

            if (job.CoresPerProcess <= 0)
                job.CoresPerProcess = 1;

            if (job.SaveModelFiles && string.IsNullOrWhiteSpace(job.ModelPath))
                throw new Exception($"Invalid model output directory: '{job.ModelPath}'");

            if (!Directory.Exists(job.ModelPath))
                Directory.CreateDirectory(job.ModelPath);

            try
            {
                await cloudInterface.SubmitJobAsync(job, cancellation.Token, s => view.Status = s);
            }
            catch (Exception err)
            {
                view.Status = "Cancelled";
                presenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// Cancels submission of a job.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        public void CancelJobSubmission(object sender, EventArgs args)
        {
            cancellation.Cancel();
        }
    }
}
