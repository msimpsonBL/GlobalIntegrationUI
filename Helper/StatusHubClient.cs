using GlobalIntegrationApi.Queries;
using Microsoft.AspNetCore.SignalR.Client;
using SystemAdmin.Models;

namespace SystemAdmin.Helper
{
    public class StatusHubClient
    {
        private readonly HubConnection _hubConnection;

        public StatusHubClient(string hubUrl)
        {
            if (string.IsNullOrEmpty(hubUrl))
            {
                throw new ArgumentException("Hub URL cannot be null or empty.", nameof(hubUrl));
            }
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .Build();
        }

        public async Task StartConnectionAsync()
        {
            if (_hubConnection.State != HubConnectionState.Connected)
            {
                await _hubConnection.StartAsync();
            }
        }

        public async Task StopConnectionAsync()
        {
            if (_hubConnection.State == HubConnectionState.Connected)
            {
                await _hubConnection.StopAsync();
            }
        }

        public async Task<List<string>> GetIdentifiersAsync()
        {
            await StartConnectionAsync();

            // Call the method exposed in the StatusHub
            var result = await _hubConnection.InvokeAsync<List<string>>("GetAllIdentifiers");

            await StopConnectionAsync();
            return result;
        }

        public async Task<IEnumerable<ParentEvent>> GetAuditForIdentifierAsync(string identifier)
        {
            await StartConnectionAsync();

            // Call the method exposed in the StatusHub
            var result = await _hubConnection.InvokeAsync<IEnumerable<ParentEvent>>("GetAuditForIdentifier", identifier);

            await StopConnectionAsync();
            return result;
        }

        public async Task<IEnumerable<ParentEvent>> GetAllAuditsAsync()
        {
            try
            {
                await StartConnectionAsync();

                // Call the method exposed in the StatusHub
                var contentList = await _hubConnection.InvokeAsync<IEnumerable<Content>>("GetAllAudits");
                await StopConnectionAsync();
                return contentList.Select(content => new ParentEvent
                {
                    EventId = content.EventId,
                    CreationTime = content.CreationTime,
                    CollectionCode = content.CollectionCode,
                    Author = content.Author,
                    EventName = content.EventName,
                    Title = content.Title,
                    Identifier = content.Identifier,
                });
            }
            catch (Exception ex)
            {
                // Log or handle exceptions as needed
                throw new Exception($"Error while calling GetAllAudits: {ex.Message}", ex);
            }
        }

        public async Task<IntegrationEventContent> GetIntegrationEventContentAsync()
        {
            try
            {
                // Fetch all audits using GetAllAuditsAsync
                var audits = await GetAllAuditsAsync();

                // Transform into IntegrationEventContent
                var integrationEventContent = new IntegrationEventContent
                {
                    TotalRecords = audits.Count(),
                    Data = audits.Select(group => new RsiPostItem
                    {
                        Identifier = group.Identifier,
                        ParentEvent = group
                    }).ToList()
                };

                return integrationEventContent;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error while creating IntegrationEventContent: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteEventAsync(Guid eventId)
        {
            await StartConnectionAsync(); // Ensure connection is active

            try
            {
                // Call the hub's DeleteEvent method
                var result = await _hubConnection.InvokeAsync<bool>("DeleteEvent", eventId);
                return result; // True if deletion is successful
            }
            catch (Exception ex)
            {
                // Handle or log errors as needed
                throw new Exception($"Error while deleting event: {ex.Message}", ex);
            }
            finally
            {
                await StopConnectionAsync(); // Ensure connection is stopped
            }
        }
    }
}
