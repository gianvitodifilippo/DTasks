using Npgsql;

namespace Approvals;

public interface IApproverRepository
{
    Task<string?> GetEmailByIdAsync(Guid id);
}

public class ApproverRepository(IConfiguration configuration) : IApproverRepository
{
    public async Task<string?> GetEmailByIdAsync(Guid id)
    {
        string connectionString = configuration.GetConnectionString("Default")!;

        string sql = "SELECT email FROM approvers WHERE id = @id;";

        using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();
            using (var command = new NpgsqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("id", id);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return reader.GetString(0);
                    }
                }
            }
        }

        return null;
    }
}
