using Microsoft.Extensions.Logging;
using Supabase.Postgrest.Models;

namespace server.Services
{
    public class SupabaseService : ISupabaseService
    {
        private readonly Supabase.Client _supabaseClient;
        private readonly ILogger<SupabaseService> _logger;

        public SupabaseService(IConfiguration configuration, ILogger<SupabaseService> logger)
        {
            var supabaseUrl = configuration["Supabase:Url"];
            var supabaseKey = configuration["Supabase:ServiceKey"];
            _supabaseClient = new Supabase.Client(supabaseUrl, supabaseKey);
            _logger = logger;
        }

        public Supabase.Client GetClient()
        {
            return _supabaseClient;
        }

        public async Task<IEnumerable<T>> GetAllAsync<T>() where T : BaseModel, IEntityWithId, new()
        {
            _logger.LogDebug("Fetching all models of type {Type}", typeof(T).Name);
            var result = await _supabaseClient
                .From<T>()
                .Get();

            var models = result.Models ?? new List<T>();
            _logger.LogDebug("Fetched {Count} models of type {Type}", models.Count(), typeof(T).Name);
            return models;
        }

        public async Task<T?> GetByIdAsync<T>(int id) where T : BaseModel, IEntityWithId, new()
        {
            _logger.LogDebug("Fetching {Type} by id {Id}", typeof(T).Name, id);
            var result = await _supabaseClient
                .From<T>()
                .Where(x => x.Id == id)
                .Single();

            return result;
        }

        public async Task<T?> CreateAsync<T>(T entity) where T : BaseModel, IEntityWithId, new()
        {
            _logger.LogInformation("Creating {Type}", typeof(T).Name);
            var result = await _supabaseClient
                .From<T>()
                .Insert(entity);

            var created = result.Models?.FirstOrDefault();
            if (created != null)
                _logger.LogInformation("Created {Type} with id {Id}", typeof(T).Name, created.Id);
            else
                _logger.LogWarning("Create {Type} returned no model", typeof(T).Name);
            return created;
        }

        public async Task<T?> UpdateAsync<T>(T entity) where T : BaseModel, IEntityWithId, new()
        {
            _logger.LogInformation("Updating {Type} with id {Id}", typeof(T).Name, entity.Id);
            var result = await _supabaseClient
                .From<T>()
                .Update(entity);

            var updated = result.Models?.FirstOrDefault();
            if (updated != null)
                _logger.LogInformation("Updated {Type} with id {Id}", typeof(T).Name, updated.Id);
            else
                _logger.LogWarning("Update {Type} returned no model for id {Id}", typeof(T).Name, entity.Id);
            return updated;
        }

        public async Task<bool> DeleteAsync<T>(int id) where T : BaseModel, IEntityWithId, new()
        {
            try
            {
                _logger.LogInformation("Deleting {Type} with id {Id}", typeof(T).Name, id);
                await _supabaseClient
                    .From<T>()
                    .Where(x => x.Id == id)
                    .Delete();

                return true;
            }
            catch
            {
                _logger.LogError("Failed to delete {Type} with id {Id}", typeof(T).Name, id);
                return false;
            }
        }
    }
}
