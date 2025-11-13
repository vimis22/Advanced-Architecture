using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using BookScheduler_MQTT.Services;

public class DbHelper
{
    private readonly string _connString;
    public DbHelper(string connString) => _connString = connString;

    private IDbConnection Conn() => new NpgsqlConnection(_connString);

    // Machines
    public async Task<IEnumerable<MachineDto>> GetAllMachinesAsync()
    {
        using var c = Conn();
        var sql = "SELECT id, name, type, pages_per_min AS PagesPerMin, is_up AS IsUp, is_busy AS IsBusy, last_seen AS LastSeen, metadata FROM machines";
        return await c.QueryAsync<MachineDto>(sql);
    }

    public async Task<MachineDto> GetMachineAsync(Guid id)
    {
        using var c = Conn();
        return await c.QueryFirstOrDefaultAsync<MachineDto>("SELECT id, name, type, pages_per_min AS PagesPerMin, is_up AS IsUp, is_busy AS IsBusy, last_seen AS LastSeen, metadata FROM machines WHERE id=@id", new { id });
    }

    public async Task SetMachineHeartbeatAsync(Guid id, bool isUp)
    {
        using var c = Conn();
        var sql = "UPDATE machines SET is_up=@isUp, last_seen=now() WHERE id=@id";
        await c.ExecuteAsync(sql, new { isUp, id });
    }

    public async Task SetMachineBusyAsync(Guid id, bool isBusy)
    {
        using var c = Conn();
        await c.ExecuteAsync("UPDATE machines SET is_busy=@isBusy WHERE id=@id", new { isBusy, id });
    }

    public async Task<IEnumerable<MachineDto>> GetAvailableMachinesByTypeAsync(string type)
    {
        using var c = Conn();
        var sql = "SELECT id, name, type, pages_per_min AS PagesPerMin, is_up AS IsUp, is_busy AS IsBusy, last_seen AS LastSeen, metadata FROM machines WHERE type=@type AND is_up=true AND is_busy=false";
        return await c.QueryAsync<MachineDto>(sql, new { type });
    }

    // Books
    public async Task<IEnumerable<BookDto>> GetAllBooksAsync()
    {
        using var c = Conn();
        return await c.QueryAsync<BookDto>("SELECT id, title, copies, pages, created_at AS CreatedAt FROM books");
    }

    public async Task<BookDto> GetBookAsync(Guid bookId)
    {
        using var c = Conn();
        return await c.QueryFirstOrDefaultAsync<BookDto>("SELECT id, title, copies, pages, created_at AS CreatedAt FROM books WHERE id=@bookId", new { bookId });
    }

    // Stages
    public async Task EnsureStageExistsAsync(Guid bookId, string stage)
    {
        using var c = Conn();
        var sql = @"
            INSERT INTO book_stages (book_id, stage)
            SELECT @bookId, @stage
            WHERE NOT EXISTS (SELECT 1 FROM book_stages WHERE book_id=@bookId AND stage=@stage)
        ";
        await c.ExecuteAsync(sql, new { bookId, stage });
    }

    public async Task<BookStageDto> GetBookStageAsync(Guid bookId, string stage)
    {
        using var c = Conn();
        var sql = "SELECT id, book_id AS BookId, stage, status, assigned_machine AS AssignedMachine, progress, updated_at AS UpdatedAt FROM book_stages WHERE book_id=@bookId AND stage=@stage";
        return await c.QueryFirstOrDefaultAsync<BookStageDto>(sql, new { bookId, stage });
    }

    public async Task AssignStageMachineAsync(Guid bookId, string stage, Guid machineId)
    {
        using var c = Conn();
        var sql = @"UPDATE book_stages SET assigned_machine=@machineId, status='running', updated_at=now() WHERE book_id=@bookId AND stage=@stage";
        await c.ExecuteAsync(sql, new { machineId, bookId, stage });
    }

    public async Task UpdateStageProgressAsync(Guid stageId, int progress, string status = null)
    {
        using var c = Conn();
        var sql = "UPDATE book_stages SET progress=@progress, status = COALESCE(@status, status), updated_at=now() WHERE id=@stageId";
        await c.ExecuteAsync(sql, new { progress, status, stageId });
    }

    public async Task SetStageStatusAsync(Guid stageId, string status)
    {
        using var c = Conn();
        await c.ExecuteAsync("UPDATE book_stages SET status=@status, updated_at=now() WHERE id=@stageId", new { status, stageId });
    }

    public async Task<IEnumerable<BookStageDto>> GetStagesForBookAsync(Guid bookId)
    {
        using var c = Conn();
        return await c.QueryAsync<BookStageDto>("SELECT id, book_id AS BookId, stage, status, assigned_machine AS AssignedMachine, progress, updated_at AS UpdatedAt FROM book_stages WHERE book_id=@bookId", new { bookId });
    }

    public async Task InsertJobEventAsync(Guid? bookStageId, Guid? machineId, string eventType, object eventData)
    {
        using var c = Conn();
        var sql = "INSERT INTO job_events (book_stage_id, machine_id, event_type, event_data) VALUES (@bookStageId, @machineId, @eventType, @eventData::jsonb)";
        await c.ExecuteAsync(sql, new { bookStageId, machineId, eventType, eventData = Newtonsoft.Json.JsonConvert.SerializeObject(eventData) });
    }

    // Query helper for checking status string quickly:
    public async Task<string> GetStageStatusAsync(Guid bookId, string stage)
    {
        using var c = Conn();
        return await c.ExecuteScalarAsync<string>("SELECT status FROM book_stages WHERE book_id=@bookId AND stage=@stage", new { bookId, stage });
    }
}
