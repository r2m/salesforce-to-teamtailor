using System;
using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Magello.TeamTailorTimerFunction
{
    public class TeamTailorTimerFunction
    {

        private readonly ILogger _logger;
        private readonly string StorageAccountUri = Environment.GetEnvironmentVariable("TABLE_STORAGE_URI") ?? "";
        private readonly string StorageAccountName = Environment.GetEnvironmentVariable("TABLE_STORAGE_NAME") ?? "";
        private readonly string StorageAccountKey = Environment.GetEnvironmentVariable("TABLE_STORAGE_KEY") ?? "";
        private readonly string StorageTableName = "Applications";

        public TeamTailorTimerFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<TeamTailorTimerFunction>();
        }

        [Function("TeamTailorTimerFunction")]
        public async Task Run([TimerTrigger("0 */5 * * * *", RunOnStartup = false)] MyInfo myTimer)
        {
            _logger.LogInformation($"TeamTailorTimerFunction executed at: {DateTime.Now}");
            _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus?.Next}");

            // Get the datetime of our last run
            var lastRun = DateTime.Now;
            if (myTimer.ScheduleStatus?.Last != null)
                lastRun = myTimer.ScheduleStatus.Last;

            _logger.LogInformation($"Last run was at {lastRun.ToString()}");

            // Get applications created since date of last run
            //var applications = await TeamTailorAPI.GetApplications(lastRun, _logger);
            var testingDate = new DateTime(2022, 12, 18);
            var applications = await TeamTailorAPI.GetApplications(testingDate, _logger);
            _logger.LogInformation($"Found {applications.Count} applications since last run");

            // Get team tailor custom fields
            var customFields = await TeamTailorAPI.GetCustomFields(_logger);
            if (customFields == null) {
                _logger.LogError("Custom fields was null");
                return;
            }

            // No applications - we're done
            //if (!applications.Any())
            //    return;

            // Get an Azure Table Storage client
            var tableClient = await GetTableClient(_logger);

            // Get a fresh access token for the Salesforce API
            await SalesForceApi.RefreshAccessToken(_logger);

            // Loop all found applications
            foreach (var application in applications) {
                // Sanity check
                if (application == null) {
                    _logger.LogInformation("Application was null");
                    continue;
                }

                // Get linked job for application
                var job = await TeamTailorAPI.GetJobFromApplication(application, _logger);
                if (job == null) {
                    _logger.LogInformation("Job was null");
                    continue;
                }

                // Get custom field values
                var fieldValues = await TeamTailorAPI.GetCustomFieldValues(job, _logger);
                if (fieldValues == null) {
                    _logger.LogInformation("Job has no custom field values");
                    continue;
                }

                /*
                // Load id and internal ref from teamtailor tags
                var refId = job["data"]?["attributes"]?["title"]?.GetValue<string>();
                // Check if job was created by this integration (ie it has an internal ref nr)
                if (string.IsNullOrEmpty(job.Data.Attributes.SalesForceInternalRefId)) {
                    _logger.LogInformation("Couldn't get internal ref nr for job");
                    continue;
                }
                if (string.IsNullOrEmpty(job.Data.Attributes.SalesForceOpportunityId)) {
                    _logger.LogInformation("Couldn't get opportunity id for job");
                    continue;
                }*/

                //var internalRef = job.Data.Attributes.SalesForceInternalRefId;
                var internalRef = "con-0002731";
                //var opportunityId = job.Data.Attributes.SalesForceOpportunityId;
                var opportunityId = "abc";
                // Try to get saved application
                var existingApplication = tableClient.Query<ApplicationTableEntity>(e => 
                    e.InternalRefNr == internalRef && 
                    e.ApplicationId == application["id"].GetValue<string>()
                ).FirstOrDefault();

                if (existingApplication != null) {
                    // This application has already been processed
                    _logger.LogInformation(
                        $"Application with id {application["id"].GetValue<string>()} already exists");
                    continue;
                }

                // Add the application to table storage
                var newTableEntity = new ApplicationTableEntity() {
                    InternalRefNr = internalRef,
                    ApplicationId = application["id"].GetValue<string>(),
                    RowKey = application["id"].GetValue<string>()
                };
                _logger.LogInformation($"Added new case to table storage: {newTableEntity}");
                tableClient.AddEntity<ApplicationTableEntity>(newTableEntity);

                // Create Salesforce case for application
                var teamTailorCandidateLink = 
                    application["relationships"]?["candidate"]?["links"]?["related"]?.GetValue<string>();
                await SalesForceApi.CreateCase(opportunityId, teamTailorCandidateLink, _logger);
            }

        }

        private async Task<TableClient> GetTableClient(ILogger _logger) {
            _logger.LogInformation($"Getting TableClient for {StorageAccountName}");
            var tableClient = new TableClient(    
                new Uri(StorageAccountUri),
                StorageTableName,
                new TableSharedKeyCredential(StorageAccountName, StorageAccountKey));
            await tableClient.CreateIfNotExistsAsync();
            _logger.LogInformation("TableClient OK");
            return tableClient;
        }
    }

    public class ApplicationTableEntity : ITableEntity {
        public string InternalRefNr { get; set; } = "";
        public string ApplicationId { get; set; } = "";
        public string PartitionKey { get; set; } = "";
        public string RowKey { get; set; } = "";
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public override string ToString()
        {
            return JsonSerializer.Serialize<ApplicationTableEntity>(this, Utils.GetJsonSerializer());
        }
    }

    public class MyInfo
    {
        public MyScheduleStatus? ScheduleStatus { get; set; }

        public bool IsPastDue { get; set; }
    }

    public class MyScheduleStatus
    {
        public DateTime Last { get; set; }

        public DateTime Next { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}
