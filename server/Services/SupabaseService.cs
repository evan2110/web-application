using Supabase.Postgrest.Models;

namespace server.Services
{
    public class SupabaseService : ISupabaseService
    {
        private readonly Supabase.Client _supabaseClient;

        public SupabaseService(IConfiguration configuration)
        {
            var supabaseUrl = configuration["Supabase:Url"];
            var supabaseKey = configuration["Supabase:ServiceKey"];
            _supabaseClient = new Supabase.Client(supabaseUrl, supabaseKey);
        }

        public Supabase.Client GetClient()
        {
            return _supabaseClient;
        }

        public async Task<IEnumerable<T>> GetAllAsync<T>() where T : BaseModel, IEntityWithId, new()
        {
            var result = await _supabaseClient
                .From<T>()
                .Get();

            return result.Models ?? new List<T>();
        }

        public async Task<T?> GetByIdAsync<T>(int id) where T : BaseModel, IEntityWithId, new()
        {
            var result = await _supabaseClient
                .From<T>()
                .Where(x => x.Id == id)
                .Single();

            return result;
        }

        public async Task<T?> CreateAsync<T>(T entity) where T : BaseModel, IEntityWithId, new()
        {
            var result = await _supabaseClient
                .From<T>()
                .Insert(entity);

            return result.Models?.FirstOrDefault();
        }

        public async Task<T?> UpdateAsync<T>(T entity) where T : BaseModel, IEntityWithId, new()
        {
            var result = await _supabaseClient
                .From<T>()
                .Update(entity);

            return result.Models?.FirstOrDefault();
        }

        public async Task<bool> DeleteAsync<T>(int id) where T : BaseModel, IEntityWithId, new()
        {
            try
            {
                await _supabaseClient
                    .From<T>()
                    .Where(x => x.Id == id)
                    .Delete();

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
